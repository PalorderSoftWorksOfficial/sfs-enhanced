using System;
using ModLoader;
using ModLoader.Helpers;
using SFSEnhanced.Mod.Networking;
using SFSEnhanced.Mod.UI;
using SFSEnhanced.Mod.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SFSEnhanced.Mod
{
    public class ModMain : global::ModLoader.Mod
    {
        public const string ModId = "sfs-enhanced";

        public override string ModNameID => ModId;
        public override string DisplayName => "SFS Enhanced";
        public override string Author => "PalorderSoftWorksOfficial";
        public override string MinimumGameVersionNecessary => "1.5";
        public override string ModVersion => "0.1.1";
        public override string Description => "Multiplayer menu and client for SFS Enhanced servers.";

        public static ModMain Instance { get; private set; }
        public NetClient Client { get; private set; }
        public MultiBuildManager Builds { get; private set; }
        public MultiplayerMenu Menu { get; private set; }

        private GameObject _host;

        public override void Load()
        {
            Instance = this;
            Debug.Log("[SFSEnhanced] Loading...");

            try
            {
                Client = new NetClient();
                Menu = new MultiplayerMenu(this);

                _host = new GameObject("SFSEnhanced");
                UnityEngine.Object.DontDestroyOnLoad(_host);
                _host.AddComponent<ModLoop>().Bind(this);

                SceneHelper.OnWorldSceneLoaded += OnWorldLoaded;
                SceneHelper.OnWorldSceneUnloaded += OnWorldUnloaded;

                try
                {
                    Builds = new MultiBuildManager(Client);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SFSEnhanced] Build synchronization disabled: {e}");
                }

                Debug.Log("[SFSEnhanced] Loaded successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SFSEnhanced] Load failed: {e}");
            }
        }

        private void OnWorldLoaded(Scene scene)
        {
            Builds?.OnWorldSceneReady();
        }

        private void OnWorldUnloaded(Scene scene)
        {
            Builds?.ResetWorld();
            Menu?.Hide();
        }
    }

    internal sealed class ModLoop : MonoBehaviour
    {
        private ModMain _mod;

        public void Bind(ModMain mod)
        {
            _mod = mod;
        }

        private void Update()
        {
            if (_mod == null) return;

            _mod.Client?.PumpIncoming();
            _mod.Menu?.EnsureHomeButton();
            _mod.Builds?.TickInterpolation(Time.deltaTime);
            _mod.Builds?.TickLocalPublish(Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.F8))
                _mod.Menu?.Toggle();
        }
    }
}
