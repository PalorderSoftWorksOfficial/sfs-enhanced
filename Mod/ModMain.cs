using System;
using System.Threading.Tasks;
using ModLoader;
using ModLoader.Helpers;
using SFSEnhanced.Mod.Controls;
using SFSEnhanced.Mod.Networking;
using SFSEnhanced.Mod.Packaging;
using SFSEnhanced.Mod.Runtime;
using SFSEnhanced.Mod.Social;
using SFSEnhanced.Mod.UI;
using SFSEnhanced.Mod.World;
using SFSEnhanced.Mod.Integrated;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SFSEnhanced.Mod
{
    public class ModMain : global::ModLoader.Mod, IModRuntimeLifecycle
    {
        public const string ModId = "sfs-enhanced";

        static ModMain()
        {
            SfsModLoaderRuntime.Install();
        }

        public string[] HarmonyOwners => new[] { "sfs-enhanced.vanillaupgrades" };

        public void DisableFromControls()
        {
            SfsModLoaderRuntime.DisableFromControls(this);
        }

        public void OnRuntimeEnable(bool startupComplete)
        {
            IsDisabled = false;
            VanillaUpgradesIntegration.Enabled = true;
            if (startupComplete) Load();
        }

        public void OnRuntimeDisable()
        {
            Disable();
        }

        public override string ModNameID => ModId;
        public override string DisplayName => "SFS Enhanced";
        public override string Author => "SFS Enhanced";
        public override string MinimumGameVersionNecessary => "1.5";
        public override string ModVersion => "0.1.2";
        public override string Description => "Multiplayer platform, dedicated servers, shared worlds, multi-build sync, friends, claims, and expanded SFS systems.";

        public static ModMain Instance { get; private set; }
        public NetClient Client { get; private set; }
        public MultiBuildManager Builds { get; private set; }
        public LocalRocketFleetSync RocketFleet { get; private set; }
        public FriendsUI Friends { get; private set; }
        internal MultiplayerMenu Menu { get; private set; }
        public bool IsDisabled { get; private set; }

        private GameObject _host;
        private ModLoop _loop;
        private Action<Scene> _worldSceneLoadedHandler;
        private Action<Scene> _worldSceneUnloadedHandler;

        public override void Load()
        {
            if (IsDisabled) return;
            Instance = this;
            VanillaUpgradesIntegration.Initialize();
            Client = new NetClient();
            Builds = new MultiBuildManager(Client);
            RocketFleet = new LocalRocketFleetSync(Client);
            Friends = new FriendsUI(Client);
            Menu = new MultiplayerMenu(this);
            PackageManager.Initialize(ModFolder.ToString());

            _host = new GameObject("SFSEnhanced");
            UnityEngine.Object.DontDestroyOnLoad(_host);
            _loop = _host.AddComponent<ModLoop>();
            _loop.Bind(this);

            _worldSceneLoadedHandler = _ => OnWorldLoaded();
            _worldSceneUnloadedHandler = _ => OnWorldUnloaded();
            SceneHelper.OnWorldSceneLoaded += _worldSceneLoadedHandler;
            SceneHelper.OnWorldSceneUnloaded += _worldSceneUnloadedHandler;
            Debug.Log("[SFSEnhanced] Loaded. SFS Enhanced replaces the home menu and keeps the native SFS actions available.");
        }

        public void Disable()
        {
            if (IsDisabled) return;
            IsDisabled = true;
            ModControlBindings.CancelCapture();

            Cleanup("multiplayer menu", () => Menu?.Dispose());
            Cleanup("rocket fleet synchronization", () => RocketFleet?.Dispose());
            Cleanup("friends", () => Friends?.Dispose());
            Cleanup("vanilla Harmony patches", VanillaUpgradesIntegration.Shutdown);
            Cleanup("build synchronization", () => Builds?.Dispose());
            Cleanup("package assets", PackageManager.Shutdown);
            Cleanup("network client", () => Client?.Dispose());

            if (_worldSceneLoadedHandler != null) SceneHelper.OnWorldSceneLoaded -= _worldSceneLoadedHandler;
            if (_worldSceneUnloadedHandler != null) SceneHelper.OnWorldSceneUnloaded -= _worldSceneUnloadedHandler;
            _loop?.Shutdown();
            if (_host != null) UnityEngine.Object.Destroy(_host);

            _loop = null;
            _host = null;
            Menu = null;
            Builds = null;
            RocketFleet = null;
            Friends = null;
            Client = null;
            if (Instance == this) Instance = null;
            Debug.Log("[SFSEnhanced] Runtime disabled. Owned patches and game state were restored. The native loader keeps managed assemblies in the default game context.");
        }

        private static void Cleanup(string label, Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogError("[SFSEnhanced] Cleanup failed for " + label + ": " + exception);
            }
        }

        private void OnWorldLoaded()
        {
            if (IsDisabled) return;
            Builds?.OnWorldSceneReady();
            PackageManager.OnWorldSceneLoaded();
        }

        private void OnWorldUnloaded()
        {
            Builds?.ResetWorld();
            PackageManager.OnWorldSceneUnloaded();
        }

        public async Task<bool> ConnectToServer(string host, int? port, string playerName, string registrationPassword = null, bool createAccount = false)
        {
            if (IsDisabled || Client == null) return false;
            ModSettings.Host = host;
            if (port.HasValue) ModSettings.Port = port.Value;
            ModSettings.PlayerName = playerName;
            try
            {
                bool ok = await Client.ConnectAsync(host, port, playerName, registrationPassword, createAccount);
                if (IsDisabled)
                {
                    Client?.Disconnect();
                    return false;
                }
                Debug.Log(ok ? $"[SFSEnhanced] Connected to {host} as {playerName}" : "[SFSEnhanced] Connection failed.");
                if (ok) ServerHistory.Record(host, port, host);
                return ok;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SFSEnhanced] Connection to {host} failed: {e.Message}");
                return false;
            }
        }
    }

    internal sealed class ModLoop : MonoBehaviour
    {
        private ModMain _mod;
        private float _homeMenuRetry;
        private Action<Scene> _homeSceneLoadedHandler;

        public void Bind(ModMain mod)
        {
            _mod = mod;
            _homeMenuRetry = 1.25f;
            _homeSceneLoadedHandler = _ => _homeMenuRetry = 1.25f;
            SceneHelper.OnHomeSceneLoaded += _homeSceneLoadedHandler;
        }

        public void Shutdown()
        {
            if (_homeSceneLoadedHandler != null) SceneHelper.OnHomeSceneLoaded -= _homeSceneLoadedHandler;
            _homeSceneLoadedHandler = null;
            _mod = null;
        }

        private void Update()
        {
            if (_mod == null || _mod.IsDisabled) return;
            _mod.Client?.PumpIncoming();
            _mod.Menu?.Tick();
            PackageManager.Tick();
            ModControlBindings.Tick(_mod);
            if (_homeMenuRetry > 0f)
            {
                _homeMenuRetry -= Time.deltaTime;
                if (_homeMenuRetry <= 0f) _mod.Menu?.EnsureMainMenu();
            }
            if (SceneManager.GetActiveScene().name == "Home_PC")
            {
                _mod.Menu?.EnsureHomeButton();
                _mod.Menu?.EnsurePlayButton();
            }
            _mod.Builds?.TickInterpolation(Time.deltaTime);
            _mod.Builds?.TickLocalPublish(Time.deltaTime);
            _mod.RocketFleet?.Tick(Time.deltaTime);
        }
    }
}
