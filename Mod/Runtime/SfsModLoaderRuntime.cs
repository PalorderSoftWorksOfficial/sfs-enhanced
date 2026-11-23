using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using ModLoader;
using ModLoader.UI;
using ModBase = global::ModLoader.Mod;
using UnityEngine;

namespace SFSEnhanced.Mod.Runtime
{
    internal static class SfsModLoaderRuntime
    {
        private const string HarmonyId = "sfs-enhanced.native-loader-runtime";
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private static readonly Dictionary<string, List<ModsListElement>> Rows = new Dictionary<string, List<ModsListElement>>(StringComparer.Ordinal);
        private static readonly FieldInfo LoadedModsField = AccessTools.Field(typeof(Loader), "loadedMods");
        private static readonly FieldInfo ActiveGetterField = AccessTools.Field(typeof(ModsListElement), "active");
        private static readonly FieldInfo ActiveOldField = AccessTools.Field(typeof(ModsListElement), "activeOld");
        private static readonly MethodInfo CheckStateMethod = AccessTools.Method(typeof(ModsListElement), "CheckState");
        private static Harmony _harmony;
        private static bool _installed;
        private static bool _startupComplete;

        private enum ToggleResult
        {
            Live,
            RestartRequired,
            Failed
        }

        private sealed class Entry
        {
            public ModBase Instance;
            public bool EarlyLoaded;
            public bool Enabled;
        }

        public static void Install()
        {
            lock (Sync)
            {
                if (_installed)
                {
                    return;
                }

                try
                {
                    _harmony = new Harmony(HarmonyId);
                    _harmony.Patch(
                        AccessTools.Method(typeof(Loader), "LoadMod", new[] { typeof(ModBase) }),
                        postfix: new HarmonyMethod(typeof(SfsModLoaderRuntime), nameof(LoadModPostfix)));
                    _harmony.Patch(
                        AccessTools.Method(typeof(Loader), "Initialize_Load"),
                        postfix: new HarmonyMethod(typeof(SfsModLoaderRuntime), nameof(InitializeLoadPostfix)));
                    _harmony.Patch(
                        AccessTools.Method(typeof(ModsListElement), "Init"),
                        postfix: new HarmonyMethod(typeof(SfsModLoaderRuntime), nameof(ModsListElementInitPostfix)));
                    _installed = true;
                    Type loadContext = Type.GetType("System.Runtime.Loader.AssemblyLoadContext, System.Runtime.Loader", false);
                    Debug.Log("[SFSEnhanced] Native mod-loader lifecycle hooks installed. Runtime=" + Environment.Version
                        + ", AssemblyLoadContext=" + (loadContext == null ? "unavailable" : "available")
                        + ", native assembly loading remains in the default game context.");
                }
                catch (Exception exception)
                {
                    try
                    {
                        _harmony?.UnpatchAll(HarmonyId);
                    }
                    catch
                    {
                    }

                    _harmony = null;
                    Debug.LogError("[SFSEnhanced] Native mod-loader hooks could not be installed: " + exception);
                }
            }
        }

        private static void LoadModPostfix(Loader __instance, ModBase mod)
        {
            if (mod == null)
            {
                return;
            }

            List<ModBase> loaded = GetLoadedMods(__instance);
            Register(mod, loaded != null && loaded.Contains(mod));
        }

        private static void InitializeLoadPostfix(Loader __instance)
        {
            _startupComplete = true;
            ModBase[] mods;

            try
            {
                mods = __instance.GetAllMods();
            }
            catch
            {
                return;
            }

            List<ModBase> loaded = GetLoadedMods(__instance);
            foreach (ModBase mod in mods)
            {
                if (mod != null)
                {
                    Register(mod, loaded != null && loaded.Contains(mod));
                }
            }
        }

        private static void ModsListElementInitPostfix(ModsListElement __instance, object[] __args)
        {
            if (__instance == null || __args == null || __args.Length == 0)
            {
                return;
            }

            ModsListElement.ModData data = (ModsListElement.ModData)__args[0];
            if (data.type != ModsListElement.ModType.Mod || string.IsNullOrWhiteSpace(data.saveName) || __instance.activeToggle == null)
            {
                return;
            }

            if (!Rows.TryGetValue(data.saveName, out List<ModsListElement> rows))
            {
                rows = new List<ModsListElement>();
                Rows.Add(data.saveName, rows);
            }

            if (!rows.Contains(__instance))
            {
                rows.Add(__instance);
            }

            Func<bool> active = ActiveGetterField?.GetValue(__instance) as Func<bool>;
            if (active == null)
            {
                return;
            }

            __instance.activeToggle.Bind(
                () => ToggleMod(data.saveName, __instance, active),
                active);
        }

        private static void ToggleMod(string id, ModsListElement element, Func<bool> active)
        {
            bool previous = active();
            bool desired = !previous;
            ModsSettings settingsObject = ModsSettings.main;
            if (settingsObject == null)
            {
                return;
            }

            Dictionary<string, bool> settings = settingsObject.settings.modsActive;
            settings[id] = desired;
            settingsObject.SaveAll();

            Loader loader = Loader.main;
            ToggleResult result = loader == null
                ? ToggleResult.Failed
                : desired
                    ? EnableMod(loader, id, new HashSet<string>(StringComparer.Ordinal))
                    : DisableMod(loader, id);

            if (result == ToggleResult.Failed)
            {
                settings[id] = previous;
                settingsObject.SaveAll();
                SetUiState(element, previous);
                Debug.LogWarning("[SFSEnhanced] Mod " + id + " could not change runtime state. Its enabled setting was restored.");
                return;
            }

            if (result == ToggleResult.Live)
            {
                SetUiState(element, desired);
                Debug.Log("[SFSEnhanced] Mod " + id + (desired ? " enabled" : " disabled") + " live.");
                return;
            }

            Debug.LogWarning("[SFSEnhanced] Mod " + id + " requires an SFS restart to apply this change because it does not implement IModRuntimeLifecycle.");
        }

        public static void DisableFromControls(ModBase mod)
        {
            if (mod == null || Loader.main == null || ModsSettings.main == null)
            {
                return;
            }

            Dictionary<string, bool> settings = ModsSettings.main.settings.modsActive;
            settings[mod.ModNameID] = false;
            ModsSettings.main.SaveAll();
            ToggleResult result = DisableMod(Loader.main, mod.ModNameID);
            if (result == ToggleResult.Failed)
            {
                settings[mod.ModNameID] = true;
                ModsSettings.main.SaveAll();
                Debug.LogWarning("[SFSEnhanced] SFS Enhanced could not be disabled. Its enabled setting was restored.");
                return;
            }

            if (result == ToggleResult.RestartRequired)
            {
                Debug.LogWarning("[SFSEnhanced] SFS Enhanced requires an SFS restart to disable.");
                return;
            }

            SetRowsState(mod.ModNameID, false);
        }

        private static void SetRowsState(string id, bool active)
        {
            if (!Rows.TryGetValue(id, out List<ModsListElement> rows))
            {
                return;
            }

            foreach (ModsListElement row in rows.ToArray())
            {
                if (row != null)
                {
                    SetUiState(row, active);
                }
            }
        }

        private static void SetUiState(ModsListElement element, bool active)
        {
            ActiveOldField?.SetValue(element, active);
            CheckStateMethod?.Invoke(element, null);
            element.activeToggle?.UpdateUI(false);
        }

        private static ToggleResult EnableMod(Loader loader, string id, HashSet<string> dependencyPath)
        {
            Entry entry = FindEntry(loader, id);
            if (entry == null || entry.Instance == null)
            {
                return ToggleResult.Failed;
            }

            List<ModBase> loaded = GetLoadedMods(loader);
            if (loaded == null)
            {
                return ToggleResult.Failed;
            }

            if (loaded.Contains(entry.Instance))
            {
                entry.Enabled = true;
                return ToggleResult.Live;
            }

            if (!dependencyPath.Add(id))
            {
                Debug.LogWarning("[SFSEnhanced] Dependency cycle prevents enabling " + id + ".");
                return ToggleResult.Failed;
            }

            try
            {
                ToggleResult dependencyResult = EnableDependencies(loader, entry.Instance, dependencyPath);
                if (dependencyResult != ToggleResult.Live)
                {
                    return dependencyResult;
                }

                if (_startupComplete)
                {
                    IModRuntimeLifecycle lifecycle = entry.Instance as IModRuntimeLifecycle;
                    if (lifecycle == null)
                    {
                        return ToggleResult.RestartRequired;
                    }

                    entry.EarlyLoaded = true;
                    entry.Instance.LoadKeybindings?.Invoke();
                    lifecycle.OnRuntimeEnable(true);
                }
                else
                {
                    if (!entry.EarlyLoaded)
                    {
                        entry.Instance.Early_Load();
                        entry.EarlyLoaded = true;
                    }

                    IModRuntimeLifecycle lifecycle = entry.Instance as IModRuntimeLifecycle;
                    lifecycle?.OnRuntimeEnable(false);
                }

                if (!loaded.Contains(entry.Instance))
                {
                    loaded.Add(entry.Instance);
                }

                entry.Enabled = true;
                return ToggleResult.Live;
            }
            catch (Exception exception)
            {
                IModRuntimeLifecycle lifecycle = entry.Instance as IModRuntimeLifecycle;
                if (lifecycle != null)
                {
                    try
                    {
                        lifecycle.OnRuntimeDisable();
                    }
                    catch
                    {
                    }
                }

                Debug.LogError("[SFSEnhanced] Enabling mod " + id + " failed: " + exception);
                return ToggleResult.Failed;
            }
            finally
            {
                dependencyPath.Remove(id);
            }
        }

        private static ToggleResult EnableDependencies(Loader loader, ModBase mod, HashSet<string> dependencyPath)
        {
            if (mod.Dependencies == null)
            {
                return ToggleResult.Live;
            }

            Dictionary<string, bool> activeSettings = ModsSettings.main.settings.modsActive;
            foreach (KeyValuePair<string, string> dependency in mod.Dependencies)
            {
                Entry dependencyEntry = FindEntry(loader, dependency.Key);
                if (dependencyEntry == null || dependencyEntry.Instance == null)
                {
                    Debug.LogWarning("[SFSEnhanced] Missing dependency " + dependency.Key + " required by " + mod.ModNameID + ".");
                    return ToggleResult.Failed;
                }

                bool enabled = !activeSettings.TryGetValue(dependency.Key, out bool active) || active;
                if (!enabled || !MeetsMinimumVersion(dependency.Value, dependencyEntry.Instance.ModVersion))
                {
                    Debug.LogWarning("[SFSEnhanced] Dependency " + dependency.Key + " is disabled or too old for " + mod.ModNameID + ".");
                    return ToggleResult.Failed;
                }

                List<ModBase> loaded = GetLoadedMods(loader);
                if (loaded == null)
                {
                    return ToggleResult.Failed;
                }

                if (!loaded.Contains(dependencyEntry.Instance))
                {
                    ToggleResult result = EnableMod(loader, dependency.Key, dependencyPath);
                    if (result != ToggleResult.Live)
                    {
                        return result;
                    }
                }
            }

            return ToggleResult.Live;
        }

        private static ToggleResult DisableMod(Loader loader, string id)
        {
            Entry entry = FindEntry(loader, id);
            if (entry == null || entry.Instance == null)
            {
                return ToggleResult.Failed;
            }

            List<ModBase> loaded = GetLoadedMods(loader);
            if (loaded == null)
            {
                return ToggleResult.Failed;
            }

            if (loaded.Any(candidate => candidate != entry.Instance
                && candidate.Dependencies != null
                && candidate.Dependencies.ContainsKey(id)))
            {
                Debug.LogWarning("[SFSEnhanced] Cannot disable " + id + " while an enabled mod depends on it.");
                return ToggleResult.Failed;
            }

            if (!loaded.Contains(entry.Instance))
            {
                entry.Enabled = false;
                return ToggleResult.Live;
            }

            IModRuntimeLifecycle lifecycle = entry.Instance as IModRuntimeLifecycle;
            if (lifecycle == null)
            {
                return ToggleResult.RestartRequired;
            }

            try
            {
                lifecycle.OnRuntimeDisable();
                string[] owners = lifecycle.HarmonyOwners ?? Array.Empty<string>();
                foreach (string owner in owners)
                {
                    if (string.IsNullOrWhiteSpace(owner))
                    {
                        continue;
                    }

                    _harmony?.UnpatchAll(owner);
                }

                loaded.Remove(entry.Instance);
                entry.Enabled = false;
                return ToggleResult.Live;
            }
            catch (Exception exception)
            {
                try
                {
                    lifecycle.OnRuntimeEnable(_startupComplete);
                }
                catch (Exception rollbackException)
                {
                    Debug.LogError("[SFSEnhanced] Rollback failed for " + id + ": " + rollbackException);
                }

                Debug.LogError("[SFSEnhanced] Disabling mod " + id + " failed: " + exception);
                return ToggleResult.Failed;
            }
        }

        private static bool MeetsMinimumVersion(string minimum, string current)
        {
            if (!Version.TryParse(minimum, out Version minimumVersion) || !Version.TryParse(current, out Version currentVersion))
            {
                return false;
            }

            return currentVersion >= minimumVersion;
        }

        private static Entry FindEntry(Loader loader, string id)
        {
            if (Entries.TryGetValue(id, out Entry existing))
            {
                return existing;
            }

            try
            {
                ModBase mod = loader.GetAllMods().FirstOrDefault(candidate => candidate != null && candidate.ModNameID == id);
                return mod == null ? null : Register(mod, GetLoadedMods(loader)?.Contains(mod) == true);
            }
            catch
            {
                return null;
            }
        }

        private static Entry Register(ModBase mod, bool loaded)
        {
            if (mod == null || string.IsNullOrWhiteSpace(mod.ModNameID))
            {
                return null;
            }

            if (!Entries.TryGetValue(mod.ModNameID, out Entry entry))
            {
                entry = new Entry { Instance = mod };
                Entries.Add(mod.ModNameID, entry);
            }
            else
            {
                entry.Instance = mod;
            }

            if (loaded)
            {
                entry.EarlyLoaded = true;
                entry.Enabled = true;
            }

            return entry;
        }

        private static List<ModBase> GetLoadedMods(Loader loader)
        {
            return LoadedModsField?.GetValue(loader) as List<ModBase>;
        }
    }
}
