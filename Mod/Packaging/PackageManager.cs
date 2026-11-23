using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SFSEnhanced.Shared.Packaging;
using SFS;
using SFS.WorldBase;
using UnityEngine;

namespace SFSEnhanced.Mod.Packaging
{
    public sealed class PackageEntry
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public bool Enabled { get; set; }
        public bool RequiresRestart { get; set; }
    }

    public static class PackageManager
    {
        private sealed class LoaderTextureState
        {
            public string Name;
            public bool HadOriginal;
            public Texture2D Original;
        }

        private sealed class PlanetTextureState
        {
            public Planet Planet;
            public Texture2D Original;
        }

        private sealed class MaterialTextureState
        {
            public Material Material;
            public string Property;
            public Texture Original;
        }

        private const string PackageDirectoryName = "Packages";
        private const string EnabledKeyPrefix = "sfs_enhanced.package.enabled.";
        public const string CurrentSfsEnhancedVersion = "0.1.2";

        private static string _modFolder;
        private static readonly Dictionary<string, Dictionary<string, Texture2D>> TextureAssets = new Dictionary<string, Dictionary<string, Texture2D>>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<LoaderTextureState> LoaderTextureStates = new List<LoaderTextureState>();
        private static readonly List<PlanetTextureState> PlanetTextureStates = new List<PlanetTextureState>();
        private static readonly List<MaterialTextureState> MaterialTextureStates = new List<MaterialTextureState>();
        private static bool _textureAssetsLoaded;
        private static bool _pendingTextureApply;

        public static string PackagesDirectory { get; private set; }
        public static int Revision { get; private set; }
        public static string LastOperation { get; private set; } = "Drop a .pack file into the Packages folder to install it.";
        public static event Action Changed;

        public static void Initialize(string modFolder)
        {
            _modFolder = Path.GetFullPath(modFolder);
            PackagesDirectory = Path.Combine(_modFolder, PackageDirectoryName);
            Directory.CreateDirectory(PackagesDirectory);
            Directory.CreateDirectory(Path.Combine(PackagesDirectory, PackageInstaller.InstalledDirectoryName));
        }

        public static void Shutdown()
        {
            _pendingTextureApply = false;
            RestoreAppliedTextureOverrides();
            DestroyTextureAssets();
            _textureAssetsLoaded = false;
            PackagesDirectory = null;
            _modFolder = null;
            Changed = null;
        }

        public static IReadOnlyList<string> GetPendingArchives()
        {
            if (string.IsNullOrEmpty(PackagesDirectory) || !Directory.Exists(PackagesDirectory)) return new string[0];
            return Directory.GetFiles(PackagesDirectory, "*", SearchOption.TopDirectoryOnly)
                .Where(path => PackageInstaller.PackageExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .Select(Path.GetFileName)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static IReadOnlyList<PackageEntry> GetInstalledPackages()
        {
            string installedDirectory = Path.Combine(PackagesDirectory ?? string.Empty, PackageInstaller.InstalledDirectoryName);
            if (!Directory.Exists(installedDirectory)) return new PackageEntry[0];
            var entries = new List<PackageEntry>();
            foreach (string directory in Directory.GetDirectories(installedDirectory))
            {
                string manifestPath = Path.Combine(directory, PackageInstaller.ManifestName);
                if (!File.Exists(manifestPath)) continue;
                try
                {
                    PackageManifest manifest = JsonConvert.DeserializeObject<PackageManifest>(File.ReadAllText(manifestPath));
                    if (manifest == null || string.IsNullOrWhiteSpace(manifest.Id)) continue;
                    string type = NormalizeType(manifest.Type);
                    entries.Add(new PackageEntry
                    {
                        Id = manifest.Id,
                        Name = string.IsNullOrWhiteSpace(manifest.Name) ? manifest.Id : manifest.Name,
                        Version = manifest.Version ?? "unknown",
                        Author = manifest.Author ?? "Unknown",
                        Description = manifest.Description ?? string.Empty,
                        Type = type,
                        Enabled = type != "texture" || PlayerPrefs.GetInt(EnabledKey(manifest.Id), 1) == 1,
                        RequiresRestart = type == "mod" || type == "experimental-mod"
                    });
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[SFSEnhanced] Could not read installed package " + Path.GetFileName(directory) + ": " + exception.Message);
                }
            }
            return entries.OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public static PackageInstallResult InstallArchive(string fileName)
        {
            var failed = new PackageInstallResult();
            if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName))
            {
                failed.Error = "Choose a package file from the Packages folder.";
                LastOperation = failed.Error;
                return failed;
            }

            string archivePath = Path.Combine(PackagesDirectory, fileName);
            if (!File.Exists(archivePath) || !PackageInstaller.PackageExtensions.Contains(Path.GetExtension(archivePath), StringComparer.OrdinalIgnoreCase))
            {
                failed.Error = "The selected package file does not exist or has an unsupported extension.";
                LastOperation = failed.Error;
                return failed;
            }

            PackageInstallResult result = PackageInstaller.InstallArchive(archivePath, PackagesDirectory, _modFolder, message => Debug.Log("[SFSEnhanced] " + message), CurrentSfsEnhancedVersion);
            if (!result.Success)
            {
                LastOperation = result.Error ?? "Package installation failed.";
                return result;
            }

            PackageManifest manifest = ReadInstalledManifest(result.PackageId);
            if (manifest != null && NormalizeType(manifest.Type) == "texture")
            {
                RefreshTextureOverrides();
                LastOperation = "Texture pack installed and enabled. Planet textures are replaced in the current session when a world is loaded.";
            }
            else if (manifest != null && NormalizeType(manifest.Type) == "solar-system")
            {
                LastOperation = "Solar system installed. It is available from the native Create World solar-system selector without restarting SFS.";
            }
            else if (manifest != null && NormalizeType(manifest.Type) == "experimental-mod")
            {
                LastOperation = "Experimental mod installed. Restart SFS to load its assembly.";
            }
            else
            {
                LastOperation = "Package installed. Restart SFS if it contains a mod assembly.";
            }

            BumpRevision();
            return result;
        }

        public static PackageUninstallResult Uninstall(string id)
        {
            PackageManifest manifest = ReadInstalledManifest(id);
            if (manifest == null)
            {
                return new PackageUninstallResult { Success = false, Error = "Installed package manifest was not found." };
            }

            string type = NormalizeType(manifest.Type);
            if (type == "solar-system" && Base.worldBase != null && Base.worldBase.insideWorld.Value)
            {
                return new PackageUninstallResult { Success = false, Error = "Exit the current world before removing a solar-system package." };
            }

            if (type == "texture") RestoreAppliedTextureOverrides();
            PackageUninstallResult result = PackageInstaller.Uninstall(id, PackagesDirectory, _modFolder);
            if (!result.Success)
            {
                LastOperation = result.Error ?? "Package removal failed.";
                return result;
            }

            PlayerPrefs.DeleteKey(EnabledKey(id));
            if (type == "solar-system" && !string.IsNullOrWhiteSpace(manifest.SolarSystemName))
            {
                string solarSystemPath = Path.Combine(FileLocations.SolarSystemsFolder.ToString(), manifest.SolarSystemName);
                if (Directory.Exists(solarSystemPath) && Directory.GetFileSystemEntries(solarSystemPath).Length == 0)
                    Directory.Delete(solarSystemPath);
            }
            if (type == "texture") RefreshTextureOverrides();
            LastOperation = "Removed " + (manifest.Name ?? id) + ".";
            BumpRevision();
            return result;
        }

        public static bool SetEnabled(string id, bool enabled)
        {
            PackageManifest manifest = ReadInstalledManifest(id);
            if (manifest == null || NormalizeType(manifest.Type) != "texture") return false;
            PlayerPrefs.SetInt(EnabledKey(id), enabled ? 1 : 0);
            PlayerPrefs.Save();
            RefreshTextureOverrides();
            LastOperation = enabled ? "Texture pack enabled for this session." : "Texture pack disabled and its previous textures restored.";
            BumpRevision();
            return true;
        }

        public static void Refresh()
        {
            RefreshTextureOverrides();
            LastOperation = GetPendingArchives().Count == 0 ? "Package library refreshed. No incoming packages." : "Package library refreshed.";
            BumpRevision();
        }

        public static void OpenPackagesFolder()
        {
            try
            {
                string command = Application.platform == RuntimePlatform.WindowsPlayer ? "explorer.exe" :
                    (Application.platform == RuntimePlatform.OSXPlayer ? "open" : "xdg-open");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = command,
                    Arguments = "\"" + PackagesDirectory + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                LastOperation = "Opened the package folder.";
            }
            catch (Exception exception)
            {
                LastOperation = "Could not open the package folder: " + exception.Message;
                Debug.LogWarning("[SFSEnhanced] " + LastOperation);
            }
            BumpRevision();
        }

        public static void OnWorldSceneLoaded()
        {
            _pendingTextureApply = true;
            LoadTextureAssets();
            Tick();
        }

        public static void OnWorldSceneUnloaded()
        {
            _pendingTextureApply = false;
            RestoreAppliedTextureOverrides();
        }

        public static void RefreshTextureOverrides()
        {
            RestoreAppliedTextureOverrides();
            DestroyTextureAssets();
            _textureAssetsLoaded = false;
            LoadTextureAssets();
            _pendingTextureApply = true;
            Tick();
        }

        public static void Tick()
        {
            if (!_pendingTextureApply) return;
            if (TryApplyTextureOverrides()) _pendingTextureApply = false;
        }

        private static void LoadTextureAssets()
        {
            if (_textureAssetsLoaded) return;
            _textureAssetsLoaded = true;
            foreach (PackageEntry entry in GetInstalledPackages())
            {
                if (entry.Type != "texture" || !entry.Enabled) continue;
                PackageManifest manifest = ReadInstalledManifest(entry.Id);
                if (manifest == null) continue;
                var assets = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
                foreach (PackageFile file in manifest.Files ?? new List<PackageFile>())
                {
                    string extension = Path.GetExtension(file.Target ?? string.Empty).ToLowerInvariant();
                    if (extension != ".png" && extension != ".jpg" && extension != ".jpeg") continue;
                    try
                    {
                        string target = PackageInstaller.ResolveTarget(file.Target, PackageInstaller.ResolveGameRoot(_modFolder));
                        if (!File.Exists(target)) continue;
                        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (!texture.LoadImage(File.ReadAllBytes(target)))
                        {
                            UnityEngine.Object.Destroy(texture);
                            continue;
                        }
                        texture.name = Path.GetFileNameWithoutExtension(file.Target);
                        assets[texture.name] = texture;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning("[SFSEnhanced] Texture in " + entry.Name + " could not be loaded: " + exception.Message);
                    }
                }
                TextureAssets[entry.Id] = assets;
            }
        }

        private static bool TryApplyTextureOverrides()
        {
            if (Base.planetLoader == null || Base.worldBase == null || !Base.worldBase.insideWorld.Value || Base.planetLoader.planets.Count == 0) return false;
            if (TextureAssets.Count == 0) return true;
            var replacementsByName = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, Dictionary<string, Texture2D>> pack in TextureAssets.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                foreach (KeyValuePair<string, Texture2D> texture in pack.Value)
                    replacementsByName[texture.Key] = texture.Value;
            }

            var replacements = new Dictionary<Texture, Texture2D>();
            foreach (KeyValuePair<string, Texture2D> replacement in replacementsByName)
            {
                Texture2D original;
                bool hadOriginal = Base.planetLoader.textures.TryGetValue(replacement.Key, out original);
                LoaderTextureStates.Add(new LoaderTextureState
                {
                    Name = replacement.Key,
                    HadOriginal = hadOriginal,
                    Original = original
                });
                if (hadOriginal && original != null) replacements[original] = replacement.Value;
                Base.planetLoader.textures[replacement.Key] = replacement.Value;
            }

            foreach (Planet planet in Base.planetLoader.planets.Values)
            {
                if (planet == null) continue;
                if (planet.planetTexture != null && replacements.TryGetValue(planet.planetTexture, out Texture2D planetReplacement))
                {
                    PlanetTextureStates.Add(new PlanetTextureState { Planet = planet, Original = planet.planetTexture });
                    planet.planetTexture = planetReplacement;
                }

                ApplyMaterialOverrides(planet.terrainMaterial, replacements);
                ApplyMaterialOverrides(planet.atmosphereMaterial, replacements);
                foreach (Renderer renderer in planet.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material material in renderer.sharedMaterials)
                        ApplyMaterialOverrides(material, replacements);
                }
            }
            return true;
        }

        private static void ApplyMaterialOverrides(Material material, Dictionary<Texture, Texture2D> replacements)
        {
            if (material == null) return;
            foreach (string property in material.GetTexturePropertyNames())
            {
                Texture current = material.GetTexture(property);
                if (current == null || !replacements.TryGetValue(current, out Texture2D replacement)) continue;
                if (MaterialTextureStates.Any(state => state.Material == material && state.Property == property)) continue;
                MaterialTextureStates.Add(new MaterialTextureState { Material = material, Property = property, Original = current });
                material.SetTexture(property, replacement);
            }
        }

        private static void RestoreAppliedTextureOverrides()
        {
            foreach (MaterialTextureState state in MaterialTextureStates)
            {
                if (state.Material != null) state.Material.SetTexture(state.Property, state.Original);
            }
            MaterialTextureStates.Clear();

            foreach (PlanetTextureState state in PlanetTextureStates)
            {
                if (state.Planet != null) state.Planet.planetTexture = state.Original;
            }
            PlanetTextureStates.Clear();

            if (Base.planetLoader != null)
            {
                foreach (LoaderTextureState state in LoaderTextureStates)
                {
                    if (state.HadOriginal) Base.planetLoader.textures[state.Name] = state.Original;
                    else Base.planetLoader.textures.Remove(state.Name);
                }
            }
            LoaderTextureStates.Clear();
        }

        private static void DestroyTextureAssets()
        {
            foreach (Dictionary<string, Texture2D> pack in TextureAssets.Values)
            {
                foreach (Texture2D texture in pack.Values)
                {
                    if (texture != null) UnityEngine.Object.Destroy(texture);
                }
            }
            TextureAssets.Clear();
        }

        private static PackageManifest ReadInstalledManifest(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return null;
            string path = Path.Combine(PackagesDirectory ?? string.Empty, PackageInstaller.InstalledDirectoryName, id, PackageInstaller.ManifestName);
            if (!File.Exists(path)) return null;
            try
            {
                return JsonConvert.DeserializeObject<PackageManifest>(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeType(string type)
        {
            return string.IsNullOrWhiteSpace(type) ? "mod" : type.Trim().ToLowerInvariant();
        }

        private static string EnabledKey(string id) => EnabledKeyPrefix + id;

        private static void BumpRevision()
        {
            Revision++;
            Changed?.Invoke();
        }
    }
}
