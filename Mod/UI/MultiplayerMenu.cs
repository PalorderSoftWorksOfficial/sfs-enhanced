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
    public class MultiplayerMenu
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

        private void ShowWorldToolsPanel()
        {
            HideFriendsPanel();
            HideChatPanel();
            HideBrowserPanel();
            HideWorldToolsPanel();
            _screenHolder.SetActive(false);
            _worldToolsHolder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "SFSEnhanced_WorldTools");
            var window = Builder.CreateWindow(_worldToolsHolder.transform, Builder.GetRandomID(), 520, 500, 0, 0, true, true, 0.98f, "World Tools");
            var root = window.ChildrenHolder;
            Builder.CreateLabel(root, 460, 32, 0, -38, "WORLD TOOLS");
            if (!_mod.Client.IsConnected || string.IsNullOrEmpty(_mod.Client.CurrentWorldId))
            {
                Builder.CreateLabel(root, 460, 34, 0, -100, "Join a world to use world tools.");
                return;
            }
            Builder.CreateLabel(root, 460, 34, 0, -100, $"World: {_mod.Client.CurrentWorldId}");
            Builder.CreateLabel(root, 460, 34, 0, -135, string.IsNullOrEmpty(_mod.Builds.LocalBuildId) ? "No local build detected." : $"Build: {_mod.Builds.LocalBuildId}");
            Builder.CreateButton(root, 210, 42, -115, -195, ClaimLocalBuild, string.IsNullOrEmpty(_localClaimId) ? "CLAIM BUILD" : "UNCLAIM BUILD");
            Builder.CreateButton(root, 210, 42, 115, -195, () => { _ = LeaveWorldAsync(); }, "LEAVE WORLD");
            if (!string.IsNullOrEmpty(_pendingInviteWorldId))
            {
                Builder.CreateLabel(root, 460, 34, 0, -260, $"Invite from {_pendingInviteFrom}: {_pendingInviteWorldId}");
                Builder.CreateButton(root, 210, 42, 0, -315, AcceptPendingInvite, "ACCEPT INVITE");
            }
            Builder.CreateButton(root, 180, 38, 0, -390, HideWorldToolsPanel, "BACK");
        }

        private void ClaimLocalBuild()
        {
            if (!_mod.Client.IsConnected || string.IsNullOrEmpty(_mod.Client.CurrentWorldId) || string.IsNullOrEmpty(_mod.Builds.LocalBuildId))
            {
                SetStatus("A local build in a world is required.");
                return;
            }
            if (!string.IsNullOrEmpty(_localClaimId))
            {
                _ = _mod.Client.SendAsync(PacketType.ClaimRemove, new ClaimRemovePacket { WorldId = _mod.Client.CurrentWorldId, ClaimId = _localClaimId });
                return;
            }
            _ = _mod.Client.SendAsync(PacketType.ClaimCreate, new ClaimCreatePacket { WorldId = _mod.Client.CurrentWorldId, Shape = ClaimShape.Build, BuildId = _mod.Builds.LocalBuildId });
            SetStatus("Claiming local build...");
        }

        private void AcceptPendingInvite()
        {
            if (string.IsNullOrEmpty(_pendingInviteWorldId)) return;
            JoinWorld(_pendingInviteWorldId, "invited world");
            _pendingInviteWorldId = null;
            _pendingInviteFrom = null;
        }

        private void ShowDirectConnect()
        {
            SetStatus("Set the host and port below, then press CONNECT.");
        }

        private async void RequestServerInfo()
        {
            if (!_mod.Client.IsConnected)
            {
                SetStatus("Connect to a server first.");
                return;
            }
            SetStatus("Loading server information...");
            await _mod.Client.SendAsync(PacketType.ServerInfoRequest, new { });
        }

        private void ShowFriendsPanel()
        {
            HideFriendsPanel();
            HideChatPanel();
            HideBrowserPanel();
            HideWorldToolsPanel();

            HideFriendsPanel();
            _screenHolder.SetActive(false);
            _friendsHolder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "SFSEnhanced_Friends");
            var window = Builder.CreateWindow(_friendsHolder.transform, Builder.GetRandomID(), 520, 500, 0, 0, true, true, 0.98f, "SFS Enhanced Friends");
            var root = window.ChildrenHolder;
            Builder.CreateLabel(root, 460, 32, 0, -38, "FRIENDS");
            Builder.CreateInputWithLabel(root, 420, 40, 0, -90, "Player name", _friendTarget, value => _friendTarget = value);
            Builder.CreateButton(root, 190, 42, -105, -145, () =>
            {
                if (!_mod.Client.IsConnected)
                {
                    SetStatus("Connect to a server first.");
                    return;
                }
                _mod.Friends.SendFriendRequest(_friendTarget);
                SetStatus("Friend request sent.");
            }, "ADD FRIEND");
            Builder.CreateButton(root, 190, 42, 105, -145, () => _mod.Friends.RefreshFriendsList(), "REFRESH");
            _friendsResultsHolder = new GameObject("SFSEnhanced_FriendsResults");
            _friendsResultsHolder.transform.SetParent(root, false);
            RenderFriendsList(_friendsResultsHolder.transform);
            _mod.Friends.RefreshFriendsList();
            Builder.CreateButton(root, 180, 38, 0, -455, HideFriendsPanel, "BACK");
        }

        private void RenderFriendsList(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(root.GetChild(i).gameObject);
            int index = 0;
            foreach (var friend in _mod.Friends.Friends)
            {
                string state = friend.Online ? "ONLINE" : "OFFLINE";
                string world = string.IsNullOrEmpty(friend.CurrentWorldId) ? "" : "WORLD";
                int y = -205 - index * 38;
                Builder.CreateLabel(root, 260, 28, -100, y, $"{friend.PlayerName}  {state}  {world}");
                if (friend.Online && !string.IsNullOrEmpty(friend.CurrentWorldId))
                {
                    Builder.CreateButton(root, 150, 30, 150, y, () =>
                    {
                        _mod.Friends.InviteFriendToCurrentWorld(friend.PlayerName);
                        SetStatus($"Invited {friend.PlayerName} to your world.");
                    }, "INVITE");
                }
                index++;
            }
            foreach (var request in _mod.Friends.IncomingRequests)
            {
                int y = -205 - index * 34;
                Builder.CreateButton(root, 200, 32, -105, y, () =>
                {
                    _mod.Friends.RespondToRequest(request.PlayerId, true);
                    SetStatus($"Accepted {request.PlayerName}.");
                }, $"ACCEPT {request.PlayerName}");
                Builder.CreateButton(root, 200, 32, 105, y, () =>
                {
                    _mod.Friends.RespondToRequest(request.PlayerId, false);
                    SetStatus($"Declined {request.PlayerName}.");
                }, "DECLINE");
                index++;
            }
            foreach (var request in _mod.Friends.OutgoingRequests)
            {
                Builder.CreateLabel(root, 430, 28, 0, -205 - index * 34, $"Pending: {request.PlayerName}");
                index++;
            }
            if (index == 0) Builder.CreateLabel(root, 430, 30, 0, -205, "No friends or pending requests.");
        }

        private void ShowChatPanel()
        {
            HideFriendsPanel();
            HideChatPanel();
            HideBrowserPanel();
            HideWorldToolsPanel();

            HideChatPanel();
            _screenHolder.SetActive(false);
            _chatHolder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "SFSEnhanced_Chat");
            var window = Builder.CreateWindow(_chatHolder.transform, Builder.GetRandomID(), 520, 560, 0, 0, true, true, 0.98f, "SFS Enhanced Chat");
            _chatRoot = window.ChildrenHolder;
            Builder.CreateLabel(_chatRoot, 460, 32, 0, -38, "CHAT");
            Builder.CreateInputWithLabel(_chatRoot, 420, 40, 0, -90, "Message", _chatMessage, value => _chatMessage = value);
            Builder.CreateButton(_chatRoot, 190, 42, -105, -145, () =>
            {
                if (!_mod.Client.IsConnected)
                {
                    SetStatus("Connect to a server first.");
                    return;
                }
                if (string.IsNullOrWhiteSpace(_chatMessage)) return;
                _ = _mod.Client.SendAsync(PacketType.ChatMessage, new ChatMessagePacket
                {
                    WorldId = _mod.Client.CurrentWorldId,
                    Message = _chatMessage.Trim(),
                });
                _chatMessage = "";
            }, "SEND");
            Builder.CreateButton(_chatRoot, 190, 42, 105, -145, () => { _chatLog.Clear(); RenderChatLog(); }, "CLEAR");
            _chatResultsHolder = new GameObject("SFSEnhanced_ChatResults");
            _chatResultsHolder.transform.SetParent(_chatRoot, false);
            RenderChatLog();
            Builder.CreateButton(_chatRoot, 180, 38, 0, -500, HideChatPanel, "BACK");
        }

        private void RenderChatLog()
        {
            if (_chatResultsHolder == null) return;
            for (int i = _chatResultsHolder.transform.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_chatResultsHolder.transform.GetChild(i).gameObject);
            int start = Math.Max(0, _chatLog.Count - 12);
            for (int i = start; i < _chatLog.Count; i++)
                Builder.CreateLabel(_chatResultsHolder.transform, 450, 28, 0, -205 - (i - start) * 28, _chatLog[i]);
            if (_chatLog.Count == 0) Builder.CreateLabel(_chatResultsHolder.transform, 450, 28, 0, -205, "No messages yet.");
        }

        private void ShowHostControls()
        {
            string executable = ModSettings.ServerExecutablePath;
            if (string.IsNullOrWhiteSpace(executable))
            {
                SetStatus("Set ServerExecutablePath in the mod settings first.");
                return;
            }
            if (!System.IO.File.Exists(executable))
            {
                SetStatus("Configured server executable was not found.");
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = executable,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(executable),
                    UseShellExecute = true
                });
                SetStatus("Dedicated server launched.");
            }
            catch (Exception e)
            {
                SetStatus("Could not launch server: " + e.Message);
            }
        }

        private void BrowseServers()
        {
            ShowServerBrowser();
            _ = LoadPublicServersAsync();
        }

        private void ShowServerBrowser()
        {
            HideFriendsPanel();
            HideChatPanel();
            HideBrowserPanel();
            HideWorldToolsPanel();

            HideBrowserPanel();
            _screenHolder.SetActive(false);
            _browserHolder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "SFSEnhanced_ServerBrowser");
            var window = Builder.CreateWindow(_browserHolder.transform, Builder.GetRandomID(), 560, 560, 0, 0, true, true, 0.98f, "Public Servers");
            var root = window.ChildrenHolder;
            Builder.CreateLabel(root, 500, 32, 0, -38, "PUBLIC SERVERS");
            Builder.CreateButton(root, 210, 40, -115, -90, () => _ = LoadPublicServersAsync(), "REFRESH");
            Builder.CreateButton(root, 210, 40, 115, -90, HideBrowserPanel, "CLOSE");
            _serverResultsHolder = new GameObject("SFSEnhanced_ServerResults");
            _serverResultsHolder.transform.SetParent(root, false);
            Builder.CreateLabel(_serverResultsHolder.transform, 470, 28, 0, -145, "Loading servers...");
        }

        private async Task LoadPublicServersAsync()
        {
            if (string.IsNullOrWhiteSpace(_serverDirectoryUrl))
            {
                SetStatus("Set a server directory URL to browse public servers.");
                return;
            }
            SetStatus("Loading public servers...");
            var servers = await _directory.ListAsync(_serverDirectoryUrl);
            if (_browserHolder == null || _serverResultsHolder == null) return;
            for (int i = _serverResultsHolder.transform.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_serverResultsHolder.transform.GetChild(i).gameObject);
            if (servers.Count == 0)
            {
                Builder.CreateLabel(_serverResultsHolder.transform, 470, 30, 0, -145, "No public servers found.");
            }
            else
            {
                for (int i = 0; i < servers.Count && i < 7; i++)
                {
                    var server = servers[i];
                    int y = -145 - i * 50;
                    string text = $"{server.Name}  {server.OnlinePlayers}/{server.MaxPlayers}  {server.Region}";
                    Builder.CreateButton(_serverResultsHolder.transform, 470, 42, 0, y, () => JoinServer(server), text);
                }
            }
            SetStatus($"Found {servers.Count} public server(s).");
        }

        private void ShowWorldBrowser()
        {
            HideFriendsPanel();
            HideChatPanel();
            HideBrowserPanel();
            HideWorldToolsPanel();

            HideBrowserPanel();
            if (!_mod.Client.IsConnected)
            {
                SetStatus("Connect to a server first.");
                return;
            }
            _screenHolder.SetActive(false);
            _browserHolder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "SFSEnhanced_WorldBrowser");
            var window = Builder.CreateWindow(_browserHolder.transform, Builder.GetRandomID(), 560, 560, 0, 0, true, true, 0.98f, "World Browser");
            var root = window.ChildrenHolder;
            Builder.CreateLabel(root, 500, 32, 0, -38, "WORLD BROWSER");
            Builder.CreateButton(root, 210, 40, -115, -90, () => _ = RequestWorldListAsync(), "REFRESH");
            Builder.CreateButton(root, 210, 40, 115, -90, HideBrowserPanel, "CLOSE");
            _serverResultsHolder = new GameObject("SFSEnhanced_WorldResults");
            _serverResultsHolder.transform.SetParent(root, false);
            Builder.CreateLabel(_serverResultsHolder.transform, 470, 30, 0, -145, "Loading worlds...");
            _ = RequestWorldListAsync();
        }

        private async Task RequestWorldListAsync()
        {
            if (!_mod.Client.IsConnected)
            {
                SetStatus("Connect to a server first.");
                return;
            }
            SetStatus("Loading worlds...");
            await _mod.Client.SendAsync(PacketType.WorldListRequest, new { });
        }


        private async Task LeaveWorldAsync()
        {
            SetStatus("Leaving world...");
            await _mod.Client.LeaveWorldAsync();
            _mod.Builds.ResetWorld();
            _localClaimId = null;
            SetStatus("World left.");
        }

        private void JoinWorld(string worldId, string worldName)
        {
            if (!_mod.Client.IsConnected)
            {
                SetStatus("Connect to a server first.");
                return;
            }
            _ = _mod.Client.SendAsync(PacketType.WorldJoin, new WorldJoinPacket { WorldId = worldId });
            SetStatus($"Joining {worldName}...");
        }

        private void JoinServer(ServerListing server)
        {
            if (server == null || string.IsNullOrWhiteSpace(server.Host)) return;
            _host = server.Host;
            _port = server.Port.ToString();
            if (!string.IsNullOrWhiteSpace(server.CertificateFingerprint)) ModSettings.SetServerCertificateFingerprint(server.Host, server.CertificateFingerprint);
            _mod.ConnectToServer(server.Host, server.Port, _playerName);
            SetStatus($"Connecting to {server.Name}...");
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

        private void HideBrowserPanel()
        {
            if (_browserHolder != null) UnityEngine.Object.Destroy(_browserHolder);
            _browserHolder = null;
            _serverResultsHolder = null;
            if (_visible && _screenHolder != null) _screenHolder.SetActive(true);
        }

        private void ClearServerResults()
        {
            if (_serverResultsHolder == null) return;
            UnityEngine.Object.Destroy(_serverResultsHolder);
            _serverResultsHolder = null;
        }

        private void HideFriendsPanel()
        {
            if (_friendsHolder != null) UnityEngine.Object.Destroy(_friendsHolder);
            _friendsHolder = null;
            _friendsResultsHolder = null;
            if (_visible && _screenHolder != null) _screenHolder.SetActive(true);
        }

        private void HideChatPanel()
        {
            if (_chatHolder != null) UnityEngine.Object.Destroy(_chatHolder);
            _chatHolder = null;
            _chatResultsHolder = null;
            _chatRoot = null;
            if (_visible && _screenHolder != null) _screenHolder.SetActive(true);
        }

        private void HideWorldToolsPanel()
        {
            if (_worldToolsHolder != null) UnityEngine.Object.Destroy(_worldToolsHolder);
            _worldToolsHolder = null;
            if (_visible && _screenHolder != null) _screenHolder.SetActive(true);
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

        private void OnPacket(PacketType type, string json)
        {
            switch (type)
            {
                case PacketType.HelloAck:
                    var hello = Newtonsoft.Json.JsonConvert.DeserializeObject<HelloAckPacket>(json);
                    SetStatus(hello.Accepted ? $"Online as {hello.PlayerId}" : $"Rejected: {hello.RejectReason}");
                    break;
                case PacketType.WorldJoinAck:
                    var join = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldJoinAckPacket>(json);
                    if (join == null)
                    {
                        SetStatus("Invalid world response.");
                        break;
                    }
                    SetStatus(join.Accepted ? $"In world {join.WorldId} ({join.Builds.Count} builds)" : $"Join failed: {join.RejectReason}");
                    break;
                case PacketType.ServerInfoResponse:
                    var info = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerInfoResponsePacket>(json);
                    SetStatus(info == null ? "Server information unavailable." : $"{info.ServerName}: {info.OnlinePlayers}/{info.MaxPlayers} online");
                    break;
                case PacketType.WorldListResponse:
                    var list = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldListResponsePacket>(json);
                    if (list?.Worlds == null)
                    {
                        SetStatus("World list unavailable.");
                        break;
                    }
                    if (_browserHolder == null || _serverResultsHolder == null) break;
                    for (int i = _serverResultsHolder.transform.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_serverResultsHolder.transform.GetChild(i).gameObject);
                    for (int i = 0; i < list.Worlds.Count && i < 7; i++)
                    {
                        var world = list.Worlds[i];
                        int y = -145 - i * 50;
                        Builder.CreateButton(_serverResultsHolder.transform, 470, 42, 0, y, () => JoinWorld(world.WorldId, world.Name), $"{world.Name}  {world.PlayersOnline} online  {world.BuildCount} builds");
                    }
                    if (list.Worlds.Count == 0) Builder.CreateLabel(_serverResultsHolder.transform, 470, 30, 0, -145, "No public worlds found.");
                    SetStatus($"Found {list.Worlds.Count} world(s).");
                    break;
                case PacketType.FriendListResponse:
                    var friends = Newtonsoft.Json.JsonConvert.DeserializeObject<FriendListResponsePacket>(json);
                    if (friends != null && _friendsResultsHolder != null) RenderFriendsList(_friendsResultsHolder.transform);
                    break;
                case PacketType.ChatMessage:
                    var chat = Newtonsoft.Json.JsonConvert.DeserializeObject<ChatMessagePacket>(json);
                    if (chat != null)
                    {
                        _chatLog.Add($"{chat.FromPlayerName}: {chat.Message}");
                        while (_chatLog.Count > 50) _chatLog.RemoveAt(0);
                        RenderChatLog();
                    }
                    break;
                case PacketType.ClaimCreate:
                    var claim = Newtonsoft.Json.JsonConvert.DeserializeObject<ClaimInfo>(json);
                    if (claim != null && claim.OwnerPlayerId == _mod.Client.PlayerId && claim.BuildId == _mod.Builds.LocalBuildId)
                    {
                        _localClaimId = claim.ClaimId;
                        SetStatus("Local build claimed.");
                    }
                    break;
                case PacketType.ClaimRemove:
                    var removedClaim = Newtonsoft.Json.JsonConvert.DeserializeObject<ClaimRemovePacket>(json);
                    if (removedClaim != null && removedClaim.ClaimId == _localClaimId)
                    {
                        _localClaimId = null;
                        SetStatus("Local build unclaimed.");
                    }
                    break;
                case PacketType.FriendInviteToWorld:
                    var invite = Newtonsoft.Json.JsonConvert.DeserializeObject<FriendInviteToWorldPacket>(json);
                    if (invite != null && !string.IsNullOrEmpty(invite.WorldId))
                    {
                        _pendingInviteWorldId = invite.WorldId;
                        _pendingInviteFrom = invite.TargetPlayerName;
                        SetStatus($"World invite received from {invite.TargetPlayerName}.");
                    }
                    break;
                case PacketType.Error:
                    var error = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorPacket>(json);
                    SetStatus(error?.Message ?? "Server error");
                    break;
            }
        }
    }
}
