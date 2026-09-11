using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ModLoader.Helpers;
using SFS.UI;
using SFS.UI.ModGUI;
using SFS.Input;
using SFSEnhanced.Mod.Networking;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SFSEnhanced.Mod.UI
{
    public partial class MultiplayerMenu
    {
        private readonly ModMain _mod;
        private readonly ServerDirectoryClient _directory = new ServerDirectoryClient();
        private GameObject _holder;
        private Window _window;
        private Label _status;
        private bool _visible;
        private MultiplayerScreen _screen;
        private GameObject _screenHolder;
        private SFS.UI.Button _playButton;
        private GameObject _playMenu;
        private string _host = "127.0.0.1";
        private string _port = "";
        private string _playerName = "Pilot";
        private string _worldName = "Shared World";
        private bool _worldPublic = true;
        private string _serverDirectoryUrl;
        private string _localClaimId;
        private string _pendingInviteWorldId;
        private string _pendingInviteFrom;
        private GameObject _serverResultsHolder;
        private GameObject _browserHolder;
        private GameObject _worldToolsHolder;
        private GameObject _friendsHolder;
        private GameObject _friendsResultsHolder;
        private GameObject _chatHolder;
        private GameObject _chatResultsHolder;
        private Transform _chatRoot;
        private string _friendTarget = "";
        private string _chatMessage = "";
        private readonly List<string> _chatLog = new List<string>();
        private GameObject _timeWarpVoteHolder;

        public MultiplayerMenu(ModMain mod)
        {
            _mod = mod;
            _serverDirectoryUrl = ModSettings.DirectoryUrl;
            _playerName = ModSettings.PlayerName;
            _host = ModSettings.Host;
            _port = "";
            _mod.Client.OnPacket += OnPacket;
            SceneHelper.OnHomeSceneUnloaded += new Action<Scene>(_ =>
            {
                Hide();
                CleanupOnHomeUnload();
            });
            SceneHelper.OnWorldSceneUnloaded += new Action<Scene>(_ => Hide());
        }

        public void EnsurePlayButton()
        {
            var menus = UnityEngine.Object.FindObjectsOfType<WorldsMenu>();
            if (menus == null || menus.Length == 0) return;
            var menu = menus[0];
            if (_playButton != null && _playMenu == menu.gameObject) return;
            if (menu.worldButtons == null || menu.worldButtons.Length == 0) return;
            var source = menu.worldButtons[0];
            if (source == null || source.transform.parent == null) return;
            if (_playButton != null) UnityEngine.Object.Destroy(_playButton.gameObject);
            _playButton = UnityEngine.Object.Instantiate(source, source.transform.parent);
            _playButton.name = "SFSEnhanced Multiplayer Button";
            _playButton.onClick = new OptionalDelegate<OnInputEndData>();
            _playButton.onClick += (Action<OnInputEndData>)(_ => Show());
            var text = _playButton.GetComponentInChildren<TextAdapter>();
            if (text != null) text.Text = "MULTIPLAYER";
            _playButton.transform.SetAsLastSibling();
            _playMenu = menu.gameObject;
        }

        public void Toggle()
        {
            if (_visible) Hide();
            else Show();
        }

        public void Show()
        {
            if (_visible) return;
            _visible = true;
            _screenHolder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "SFSEnhanced_MultiplayerScreen");
            _screen = _screenHolder.AddComponent<MultiplayerScreen>();
            _screen.Bind(this);
            ScreenManager.main.OpenScreen(() => _screen);
        }

        internal void BuildWindow(Transform parent)
        {
            ModSettings.PlayerName = _playerName;
            ModSettings.Host = _host;
            if (int.TryParse(_port, out var savedPort)) ModSettings.Port = savedPort;
            ClearServerResults();
            _holder = parent.gameObject;
            _window = Builder.CreateWindow(parent, Builder.GetRandomID(), 900, 620, 0, 310, false, false, 0.98f, "SFS Enhanced Multiplayer");
            var root = _window.ChildrenHolder;

            Builder.CreateLabel(root, 460, 32, 0, -38, "MULTIPLAYER");
            Builder.CreateLabel(root, 460, 26, 0, -70, "Connect, browse worlds, manage friends, and chat.");
            Builder.CreateButton(root, 112, 42, -180, -118, BrowseServers, "BROWSE");
            Builder.CreateButton(root, 112, 42, -60, -118, ShowHostControls, "HOST");
            Builder.CreateButton(root, 112, 42, 60, -118, ShowFriendsPanel, "FRIENDS");
            Builder.CreateButton(root, 112, 42, 180, -118, ShowChatPanel, "CHAT");
            Builder.CreateButton(root, 112, 42, -180, -172, RequestServerInfo, "SERVER INFO");
            Builder.CreateButton(root, 112, 42, -60, -172, () =>
            {
                if (!_mod.Client.IsConnected)
                {
                    SetStatus("Connect to a server first.");
                    return;
                }
                ShowWorldBrowser();
            }, "WORLD LIST");
            Builder.CreateButton(root, 112, 42, 60, -172, () =>
            {
                if (!_mod.Client.IsConnected || string.IsNullOrEmpty(_mod.Client.CurrentWorldId))
                {
                    SetStatus("You are not in a world.");
                    return;
                }
                _ = LeaveWorldAsync();
            }, "LEAVE WORLD");
            Builder.CreateButton(root, 112, 42, 180, -172, ShowWorldToolsPanel, "WORLD TOOLS");

            Builder.CreateInputWithLabel(root, 420, 40, 0, -240, "Player name", _playerName, s =>
            {
                _playerName = s;
                ModSettings.PlayerName = s;
            });
            Builder.CreateInputWithLabel(root, 420, 40, 0, -290, "Directory", _serverDirectoryUrl, s =>
            {
                _serverDirectoryUrl = s;
                ModSettings.DirectoryUrl = s;
            });
            Builder.CreateInputWithLabel(root, 420, 40, 0, -340, "Server host", _host, s => _host = s);
            Builder.CreateInputWithLabel(root, 420, 40, 0, -390, "Port override (optional)", _port, s => _port = s);
            Builder.CreateInputWithLabel(root, 420, 40, 0, -440, "World name", _worldName, s => _worldName = s);
            Builder.CreateToggleWithLabel(root, 420, 40, () => _worldPublic, () => _worldPublic = !_worldPublic, 0, -480, "Public world");

            Builder.CreateButton(root, 130, 38, -155, -530, () =>
            {
                int? port = int.TryParse(_port, out var parsedPort) ? parsedPort : (int?)null;
                _mod.ConnectToServer(_host, port, _playerName);
                SetStatus(port.HasValue ? $"Resolving {_host}:{port.Value}..." : $"Resolving {_host} using SRV...");
            }, "CONNECT");
            Builder.CreateButton(root, 130, 38, 0, -530, () =>
            {
                if (!_mod.Client.IsConnected)
                {
                    SetStatus("Connect to a server first.");
                    return;
                }
                _ = _mod.Client.SendAsync(PacketType.WorldCreate, new WorldCreatePacket { Name = _worldName, IsPublic = _worldPublic });
                SetStatus(_worldPublic ? "Creating public world..." : "Creating private world...");
            }, "CREATE WORLD");
            Builder.CreateButton(root, 130, 38, 155, -530, () =>
            {
                _mod.Client.Disconnect();
                SetStatus("Disconnected");
            }, "DISCONNECT");
            _status = Builder.CreateLabel(root, 460, 34, 0, -572, "Not connected");
        }

        internal void DestroyWindow()
        {
            if (_window != null) UnityEngine.Object.Destroy(_window.gameObject);
            _window = null;
            _holder = null;
            _status = null;
            _serverResultsHolder = null;
        }

        internal void OnScreenClosed()
        {
            _visible = false;
            _screen = null;
            if (_screenHolder != null) UnityEngine.Object.Destroy(_screenHolder);
            _screenHolder = null;
        }

        public void Hide()
        {
            if (_screen != null && ScreenManager.main.CurrentScreen == _screen)
            {
                _screen.Close();
                return;
            }
            OnScreenClosed();
            DestroyWindow();
            HideFriendsPanel();
            HideChatPanel();
            HideBrowserPanel();
            HideWorldToolsPanel();
        }

        public void CleanupOnHomeUnload()
        {
            if (_playButton != null) UnityEngine.Object.Destroy(_playButton.gameObject);
            _playButton = null;
            _playMenu = null;
        }

        private void SetStatus(string text)
        {
            if (_status != null) _status.Text = text;
            UnityEngine.Debug.Log("[SFSEnhanced] " + text);
        }

    }
}
