using System;
using ModLoader;
using ModLoader.Helpers;
using SFSEnhanced.Mod.Networking;
using SFSEnhanced.Mod.Social;
using SFSEnhanced.Mod.UI;
using SFSEnhanced.Mod.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SFSEnhanced.Mod
{
    /// <summary>
    /// Entry point for the game's built-in ModLoader (compiled into Assembly-CSharp).
    /// No Harmony / third-party loader required.
    /// </summary>
    public class ModMain : global::ModLoader.Mod
    {
        public const string ModId = "sfs-enhanced";

        public override string ModNameID => ModId;
        public override string DisplayName => "SFS Enhanced";
        public override string Author => "SFS Enhanced";
        public override string MinimumGameVersionNecessary => "1.5";
        public override string ModVersion => "0.1.0";
        public override string Description =>
            "Dedicated-server multiplayer, shared worlds, multi-build sync, friends & claims.";

        public static ModMain Instance { get; private set; }

        public NetClient Client { get; private set; }
        public MultiBuildManager Builds { get; private set; }
        public FriendsUI Friends { get; private set; }
        public MultiplayerMenu Menu { get; private set; }

        private GameObject _host;

        public override void Load()
        {
            Instance = this;

            Client = new NetClient();
            Builds = new MultiBuildManager(Client);
            Friends = new FriendsUI(Client);
            Menu = new MultiplayerMenu(this);

            _host = new GameObject("SFSEnhanced");
            UnityEngine.Object.DontDestroyOnLoad(_host);
            _host.AddComponent<ModLoop>().Bind(this);

            SceneHelper.OnWorldSceneLoaded += new Action<Scene>(_ => OnWorldLoaded());
            SceneHelper.OnHomeSceneLoaded += new Action<Scene>(_ => Menu.Show());

            Debug.Log("[SFSEnhanced] Loaded (native ModLoader). Open the multiplayer window from Home, or press F8 in-world.");
        }

        private void OnWorldLoaded()
        {
            Menu.Show();
            Builds.OnWorldSceneReady();
        }

        public async void ConnectToServer(string host, int port, string playerName)
        {
            bool ok = await Client.ConnectAsync(host, port, playerName);
            Debug.Log(ok
                ? $"[SFSEnhanced] Connected to {host}:{port} as {playerName}"
                : "[SFSEnhanced] Connection failed.");
        }
    }

    /// <summary>Persistent MonoBehaviour that pumps net I/O every frame.</summary>
    internal sealed class ModLoop : MonoBehaviour
    {
        private ModMain _mod;

        public void Bind(ModMain mod) => _mod = mod;

        private void Update()
        {
            if (_mod == null) return;
            _mod.Client?.PumpIncoming();
            _mod.Builds?.TickInterpolation(Time.deltaTime);
            _mod.Builds?.TickLocalPublish(Time.deltaTime);
            if (Input.GetKeyDown(KeyCode.F8))
                _mod.Menu?.Toggle();
        }
    }
}
