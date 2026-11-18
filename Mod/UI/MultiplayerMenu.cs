using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using ModLoader.Helpers;
using SFS.Input;
using SFS.UI;
using SFSEnhanced.Mod.Networking;
using SFSEnhanced.Mod.UI.App;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SFSEnhanced.Mod.UI
{
    internal sealed class MultiplayerMenu : IAppActions
    {
        private readonly ModMain _mod;
        private readonly ServerDirectoryClient _directory = new ServerDirectoryClient();
        private readonly AppState _state = new AppState();
        private readonly ConcurrentQueue<Action> _deferred = new ConcurrentQueue<Action>();

        private AppShell _shell;
        private MultiplayerScreen _screen;
        private GameObject _screenHolder;
        private Screen_Base _previousScreen;
        private SFS.UI.Button _playButton;
        private GameObject _playMenu;
        private string _host;
        private int? _port;
        private long _chatSequence;
        private string _forgetArmedEndpointKey;
        private string _forgetArmedFingerprint;
        private float _forgetArmedAt;

        public AppState State => _state;

        public ModMain Mod => _mod;

        public MultiplayerMenu(ModMain mod)
        {
            _mod = mod;
            _host = ModSettings.Host;
            _mod.Client.OnPacket += OnPacket;
            _mod.Client.ProfileChanged += () => _state.NotifyContent();
            _mod.Client.Disconnected += () => RunOnMainThread(HandleTransportDrop);
            _mod.Client.IdentityPromptChanged += () => RunOnMainThread(() =>
            {
                _state.PendingIdentity = _mod.Client.PendingIdentity;
                _state.NotifyContent();
            });
            _state.MigrationNotice = _mod.Client.MigrationNotice;
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
            if (_screen != null)
            {
                if (ScreenManager.main != null && ScreenManager.main.CurrentScreen != _screen)
                {
                    Debug.LogWarning("[SFSEnhanced] Another screen is open above the multiplayer shell; close it first.");
                    return;
                }
                Hide();
                return;
            }
            Show();
        }

        public bool CanOpenHere() => _screen != null || SceneManager.GetActiveScene().name != "World_PC";

        public void Show()
        {
            if (_screen != null) return;
            if (ScreenManager.main == null)
            {
                Debug.LogWarning("[SFSEnhanced] ScreenManager is unavailable; the multiplayer screen cannot open.");
                return;
            }

            if (SceneManager.GetActiveScene().name == "World_PC")
            {
                Debug.LogWarning("[SFSEnhanced] The full shell is disabled in flight; a non-pausing overlay is not implemented yet.");
                return;
            }

            _screenHolder = CreateScreenHolder();
            if (_screenHolder == null)
            {
                Debug.LogError("[SFSEnhanced] No canvas accepted the multiplayer screen; refusing to open an invisible window.");
                return;
            }

            _previousScreen = ScreenManager.main.CurrentScreen;
            _screen = _screenHolder.AddComponent<MultiplayerScreen>();
            _screen.Bind(this);
            ScreenManager.main.OpenScreen(() => _screen);
            Debug.Log($"[SFSEnhanced] screen stack {ScreenManager.main.GetStackCount()} after open.");
        }

        private static GameObject CreateScreenHolder()
        {
            GameObject holder = TryCreateHolder(SFS.UI.ModGUI.Builder.SceneToAttach.BaseScene);
            if (holder != null) return holder;
            return TryCreateHolder(SFS.UI.ModGUI.Builder.SceneToAttach.CurrentScene);
        }

        private static GameObject TryCreateHolder(SFS.UI.ModGUI.Builder.SceneToAttach mode)
        {
            GameObject holder;
            try
            {
                holder = SFS.UI.ModGUI.Builder.CreateHolder(mode, "SFSEnhanced_MultiplayerScreen");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SFSEnhanced] Mount attempt {mode} failed: {e.Message}");
                return null;
            }

            if (holder != null && holder.transform.parent != null) return holder;
            if (holder != null) UnityEngine.Object.Destroy(holder);
            return null;
        }

        internal void BuildWindow(Transform parent)
        {
            ModSettings.PlayerName = string.IsNullOrWhiteSpace(ModSettings.PlayerName) ? "Player" : ModSettings.PlayerName;
            ModSettings.Host = _host;
            if (_port.HasValue) ModSettings.Port = _port.Value;

            _state.ServerHost = _port.HasValue ? _host + ":" + _port.Value : _host;
            _shell = new AppShell(this);
            _shell.Build(parent);

            if (_mod.Client.IsConnected)
            {
                RefreshFriends();
                RefreshRooms();
            }
        }

        internal void DestroyWindow()
        {
            if (_shell != null) _shell.Dispose();
            _shell = null;
        }

        internal void OnScreenClosed()
        {
            _screen = null;
            if (_screenHolder != null) UnityEngine.Object.Destroy(_screenHolder);
            _screenHolder = null;
        }

        public void Hide()
        {
            MultiplayerScreen screen = _screen;
            if (screen == null || ScreenManager.main == null)
            {
                OnScreenClosed();
                DestroyWindow();
                return;
            }

            int before = ScreenManager.main.GetStackCount();
            bool buried = ScreenManager.main.CurrentScreen != screen;

            if (!RestorePreviousScreen())
            {
                Debug.LogWarning("[SFSEnhanced] No screen to restore; keeping the multiplayer shell open instead of destroying a live screen.");
                return;
            }

            if (ScreenManager.main.CurrentScreen == (Screen_Base)screen)
            {
                Debug.LogWarning("[SFSEnhanced] The multiplayer screen is still current after restore; leaving it open.");
                return;
            }

            Debug.Log($"[SFSEnhanced] screen stack {before} -> {ScreenManager.main.GetStackCount()} after hide (buried: {buried}).");

            OnScreenClosed();
            DestroyWindow();
        }

        private bool RestorePreviousScreen()
        {
            if (ScreenManager.main == null) return false;
            try
            {
                Screen_Base previous = _previousScreen != null ? _previousScreen : ScreenManager.main.initialScreen;
                if (previous == null) return false;
                ScreenManager.main.SetStack(() => previous);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SFSEnhanced] Could not restore the previous screen: " + e.Message);
                return false;
            }
            finally
            {
                _previousScreen = null;
            }
        }

        public void CleanupOnHomeUnload()
        {
            if (_playButton != null) UnityEngine.Object.Destroy(_playButton.gameObject);
            _playButton = null;
            _playMenu = null;
        }

        internal void Tick()
        {
            Action action;
            while (_deferred.TryDequeue(out action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Debug.LogError("[SFSEnhanced] Deferred UI action failed: " + e);
                }
            }
        }

        private void RunOnMainThread(Action action) => _deferred.Enqueue(action);

        public void Navigate(string pageId)
        {
            if (_shell != null) _shell.Navigate(pageId);
        }

        public bool Back() => _shell != null && _shell.Back();

        private void HandleTransportDrop()
        {
            if (!_state.Connected) return;
            _state.ResetConnection();
            _state.Chat.Clear();
            _state.SetStatus("Disconnected from the server.", true);
            _state.NotifyContent();
        }

        public void SetPlayerName(string playerName)
        {
            string trimmed = (playerName ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                SetStatus("Enter a player name.", true);
                return;
            }
            if (trimmed.Length > 32)
            {
                SetStatus("Player names are limited to 32 characters.", true);
                return;
            }
            ModSettings.PlayerName = trimmed;
            SetStatus("Player name saved. It applies the next time you connect.");
        }

        private string _pendingRegistrationPassword;
        private bool _pendingCreateAccount;

        public void Connect(string host, string portText) => StartConnect(host, portText, null, false);

        public void CreateAccount(string host, string portText, string registrationPassword)
            => StartConnect(host, portText, registrationPassword, true);

        public void RetryWithPassword(string registrationPassword)
        {
            if (string.IsNullOrEmpty(_host))
            {
                SetStatus("Connect to a server before entering a password.", true);
                return;
            }
            _pendingRegistrationPassword = registrationPassword;
            SetStatus(_pendingCreateAccount ? $"Creating an account on {_state.ServerHost}..." : $"Connecting to {_state.ServerHost}...");
            _ = ConnectAndReportAsync(_host, _port, ModSettings.PlayerName, registrationPassword, _pendingCreateAccount);
        }

        private void StartConnect(string host, string portText, string registrationPassword, bool createAccount)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                SetStatus("Enter a server host.", true);
                return;
            }
            if (string.IsNullOrWhiteSpace(ModSettings.PlayerName))
            {
                SetStatus("Set a player name in Settings first.", true);
                return;
            }

            int parsed;
            bool hasPort = int.TryParse(portText, out parsed);
            if (!string.IsNullOrWhiteSpace(portText) && (!hasPort || parsed < 1 || parsed > 65535))
            {
                SetStatus("Port must be between 1 and 65535.", true);
                return;
            }

            _host = host.Trim();
            _port = string.IsNullOrWhiteSpace(portText) ? (int?)null : parsed;
            _pendingRegistrationPassword = registrationPassword;
            _pendingCreateAccount = createAccount;
            _state.ServerHost = _port.HasValue ? _host + ":" + _port.Value : _host;
            if (createAccount) SetStatus($"Creating an account on {_state.ServerHost}...");
            else SetStatus(_port.HasValue ? $"Connecting to {_host}:{_port.Value}..." : $"Resolving {_host} over SRV...");
            _ = ConnectAndReportAsync(_host, _port, ModSettings.PlayerName, registrationPassword, createAccount);
        }

        private async Task ConnectAndReportAsync(string host, int? port, string playerName, string registrationPassword, bool createAccount)
        {
            bool ok = await _mod.ConnectToServer(host, port, playerName, registrationPassword, createAccount);
            RunOnMainThread(() =>
            {
                _state.PendingIdentity = _mod.Client.PendingIdentity;
                _state.LastConnectError = _mod.Client.LastConnectError;
                _state.LastConnectErrorCode = _mod.Client.LastConnectErrorCode;
                if (ok) _state.SetStatus($"Connected to {_state.ServerHost}.");
                else if (_state.PendingIdentity != null) _state.SetStatus(_state.PendingIdentity.Reason ?? "The server identity was not trusted.", true);
                else if (!string.IsNullOrEmpty(_state.LastConnectError)) _state.SetStatus(_state.LastConnectError, true);
                else _state.SetStatus("Could not connect to the server.", true);
                _state.NotifyContent();
            });
        }

        public void ReplaceServerIdentity()
        {
            ServerIdentityPrompt prompt = _state.PendingIdentity;
            if (prompt == null || !prompt.IsChanged)
            {
                SetStatus("There is no changed server identity waiting for confirmation.", true);
                return;
            }

            bool armed = string.Equals(_forgetArmedEndpointKey, prompt.EndpointKey, StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(_forgetArmedFingerprint, prompt.PresentedFingerprint, StringComparison.OrdinalIgnoreCase) &&
                         Time.unscaledTime - _forgetArmedAt <= 30f;
            if (!armed)
            {
                _forgetArmedEndpointKey = prompt.EndpointKey;
                _forgetArmedFingerprint = prompt.PresentedFingerprint;
                _forgetArmedAt = Time.unscaledTime;
                SetStatus("Press replace again within 30 seconds to trust the new fingerprint. The previous fingerprint stays on record.", true);
                return;
            }

            if (!_mod.Client.TrustPendingIdentity())
            {
                SetStatus("The new server identity could not be accepted.", true);
                return;
            }
            _forgetArmedEndpointKey = null;
            _forgetArmedFingerprint = null;
            SetStatus($"Trusting the new fingerprint for {prompt.DisplayAuthority}. Connecting...");
            _state.NotifyContent();
            _ = ConnectAndReportAsync(_host, _port, ModSettings.PlayerName, _pendingRegistrationPassword, _pendingCreateAccount);
        }

        public void TrustServer()
        {
            ServerIdentityPrompt prompt = _state.PendingIdentity;
            if (prompt == null || !prompt.IsTrustPrompt)
            {
                SetStatus("There is no server identity waiting for your decision.", true);
                return;
            }
            if (!_mod.Client.TrustPendingIdentity())
            {
                SetStatus("The server identity could not be saved.", true);
                return;
            }
            _state.PendingIdentity = null;
            _forgetArmedEndpointKey = null;
            SetStatus($"Trusted {prompt.DisplayAuthority} on this device. Connecting...");
            _state.NotifyContent();
            _ = ConnectAndReportAsync(_host, _port, ModSettings.PlayerName, _pendingRegistrationPassword, _pendingCreateAccount);
        }

        public void CancelIdentityPrompt()
        {
            _mod.Client.CancelPendingIdentity();
            _state.PendingIdentity = null;
            _forgetArmedEndpointKey = null;
            _forgetArmedFingerprint = null;
            SetStatus("Connection cancelled. The server identity was not trusted.");
            _state.NotifyContent();
        }

        public void ForgetTrustedServer(string endpointKey)
        {
            if (string.IsNullOrWhiteSpace(endpointKey)) return;
            bool armed = string.Equals(_forgetArmedEndpointKey, endpointKey, StringComparison.OrdinalIgnoreCase) &&
                         Time.unscaledTime - _forgetArmedAt <= 30f;
            if (!armed)
            {
                _forgetArmedEndpointKey = endpointKey;
                _forgetArmedAt = Time.unscaledTime;
                SetStatus("Press the same remove button again within 30 seconds to forget this trusted fingerprint.", true);
                return;
            }
            _forgetArmedEndpointKey = null;
            _mod.Client.ForgetPin(endpointKey);
            _state.PendingIdentity = null;
            SetStatus("Trusted fingerprint removed. Connect again to compare and trust an identity.");
            _state.NotifyContent();
        }

        public void Disconnect()
        {
            _mod.Client.Disconnect();
            _state.ResetConnection();
            _state.SetStatus("Disconnected.");
            _state.NotifyContent();
        }

        public void RefreshServers()
        {
            string url = ModSettings.DirectoryUrl;
            if (string.IsNullOrWhiteSpace(url))
            {
                _state.Servers.Clear();
                SetStatus("No server directory configured.", true);
                return;
            }

            _state.LoadingServers = true;
            _state.DirectoryError = null;
            _state.NotifyContent();
            _ = LoadServersAsync(url);
        }

        private async Task LoadServersAsync(string url)
        {
            ServerDirectoryResult result = await _directory.ListAsync(url).ConfigureAwait(false);
            RunOnMainThread(() =>
            {
                _state.Servers.Clear();
                _state.Servers.AddRange(result.Servers);
                _state.DirectoryError = result.Error;
                _state.LoadingServers = false;
                if (result.Error != null) _state.SetStatus("The server directory could not be loaded.", true);
                else _state.SetStatus(result.Servers.Count == 0 ? "The directory returned no servers." : $"Directory returned {result.Servers.Count} server(s).");
                _state.NotifyContent();
            });
        }

        public void RefreshRooms()
        {
            if (!_state.Connected)
            {
                SetStatus("Connect to a server first.", true);
                return;
            }
            _state.LoadingRooms = true;
            _state.NotifyContent();
            _ = _mod.Client.SendAsync(PacketType.RoomListRequest, new { });
        }

        public void CreateRoom(string name, string description, bool isPublic, string capacityText)
        {
            if (!_state.Connected)
            {
                SetStatus("Connect to a server first.", true);
                return;
            }
            string trimmed = (name ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                SetStatus("Enter a room name.", true);
                return;
            }
            if (trimmed.Length > 48)
            {
                SetStatus("Room names are limited to 48 characters.", true);
                return;
            }
            string capacityValue = (capacityText ?? string.Empty).Trim();
            if (capacityValue.Length == 0) capacityValue = "8";
            if (!int.TryParse(capacityValue, out int capacity) || capacity < 2 || capacity > 64)
            {
                SetStatus("Room capacity must be a whole number between 2 and 64.", true);
                return;
            }
            if ((description ?? string.Empty).Length > 160)
            {
                SetStatus("Room descriptions are limited to 160 characters.", true);
                return;
            }

            _ = _mod.Client.SendAsync(PacketType.RoomCreate, new RoomCreatePacket
            {
                Name = trimmed,
                Description = (description ?? string.Empty).Trim(),
                Visibility = isPublic ? RoomVisibility.Public : RoomVisibility.Friends,
                Capacity = capacity
            });
            SetStatus(isPublic ? "Opening a public room..." : "Opening a friends-only room...");
        }

        public void JoinRoom(string roomId, string roomName)
        {
            if (!_state.Connected)
            {
                SetStatus("Connect to a server first.", true);
                return;
            }
            if (string.IsNullOrWhiteSpace(roomId)) return;
            _ = _mod.Client.SendAsync(PacketType.RoomJoin, new RoomJoinPacket { RoomId = roomId });
            SetStatus($"Joining {roomName ?? "room"}...");
        }

        public void LeaveRoom()
        {
            _state.CurrentRoomId = null;
            _state.CurrentRoomName = null;
            _state.CurrentWorldId = null;
            _state.CurrentWorldName = null;
            _state.CurrentWorldBuilds = 0;
            _state.CurrentWorldPlayers = 0;
            _state.LocalClaimId = null;
            _state.PendingVote = null;
            _mod.Builds.ResetWorld();
            _ = LeaveWorldOnServerAsync();
            SetStatus("Left the room.");
            _state.NotifyContent();
            RefreshRooms();
        }

        public void CloseRoom()
        {
            if (string.IsNullOrEmpty(_state.CurrentRoomId))
            {
                SetStatus("You are not hosting a room.", true);
                return;
            }
            _ = _mod.Client.SendAsync(PacketType.RoomClose, new RoomClosePacket { RoomId = _state.CurrentRoomId });
            SetStatus("Closing the room...");
        }

        public void JoinWorld(string worldId, string worldName)
        {
            if (!_state.Connected)
            {
                SetStatus("Connect to a server first.", true);
                return;
            }
            if (string.IsNullOrWhiteSpace(worldId)) return;
            _state.CurrentWorldName = worldName;
            _ = _mod.Client.SendAsync(PacketType.WorldJoin, new WorldJoinPacket { WorldId = worldId });
            SetStatus($"Joining {worldName ?? worldId}...");
        }

        public void LeaveWorld()
        {
            _state.CurrentRoomId = null;
            _state.CurrentRoomName = null;
            _state.CurrentWorldId = null;
            _state.CurrentWorldName = null;
            _state.CurrentWorldBuilds = 0;
            _state.CurrentWorldPlayers = 0;
            _state.LocalClaimId = null;
            _state.PendingVote = null;
            _mod.Builds.ResetWorld();
            SetStatus("Left the world.");
            _state.NotifyContent();
            _ = LeaveWorldOnServerAsync();
        }

        private async Task LeaveWorldOnServerAsync() => await _mod.Client.LeaveWorldAsync();

        public void SendChat(string message)
        {
            if (!_state.Connected)
            {
                SetStatus("Connect to a server first.", true);
                return;
            }
            string trimmed = (message ?? string.Empty).Trim();
            if (trimmed.Length == 0) return;
            if (trimmed.Length > 512) trimmed = trimmed.Substring(0, 512);
            _ = _mod.Client.SendAsync(PacketType.ChatMessage, new ChatMessagePacket { WorldId = _state.CurrentWorldId, Message = trimmed });
        }

        public void ClearChat()
        {
            _state.Chat.Clear();
            _state.NotifyContent();
        }

        public void ClaimToggle()
        {
            if (!_state.Connected || string.IsNullOrEmpty(_state.CurrentWorldId))
            {
                SetStatus("Join a world first.", true);
                return;
            }
            string buildId = _mod.Builds.LocalBuildId;
            if (string.IsNullOrEmpty(buildId))
            {
                SetStatus("Build something in this world first.", true);
                return;
            }
            if (!string.IsNullOrEmpty(_state.LocalClaimId))
            {
                _ = _mod.Client.SendAsync(PacketType.ClaimRemove, new ClaimRemovePacket { WorldId = _state.CurrentWorldId, ClaimId = _state.LocalClaimId });
                SetStatus("Releasing claim...");
                return;
            }
            _ = _mod.Client.SendAsync(PacketType.ClaimCreate, new ClaimCreatePacket { WorldId = _state.CurrentWorldId, Shape = ClaimShape.Build, BuildId = buildId });
            SetStatus("Claiming your build...");
        }

        public void RequestTimewarp(double multiplier)
        {
            if (!_state.Connected || string.IsNullOrEmpty(_state.CurrentWorldId))
            {
                SetStatus("Join a world to control time warp.", true);
                return;
            }
            _ = _mod.Client.SendAsync(PacketType.TimeWarpRequest, new TimeWarpRequestPacket { WorldId = _state.CurrentWorldId, RequestedMultiplier = multiplier });
            SetStatus($"Requested {multiplier:0.##}x time warp.");
        }

        public void AnswerVote(bool approved)
        {
            TimeWarpVotePacket vote = _state.PendingVote;
            if (vote == null) return;
            _state.PendingVote = null;
            _ = _mod.Client.SendAsync(PacketType.TimeWarpVoteResponse, new TimeWarpVoteResponsePacket
            {
                WorldId = vote.WorldId,
                VoteId = vote.VoteId,
                Approved = approved
            });
            SetStatus(approved ? "Time warp vote approved." : "Time warp vote rejected.");
        }

        public void RefreshFriends()
        {
            if (!_state.Connected)
            {
                SetStatus("Connect to a server first.", true);
                return;
            }
            _mod.Friends.RefreshFriendsList();
            _state.NotifyContent();
        }

        public void AddFriend(string playerName)
        {
            if (!_state.Connected)
            {
                SetStatus("Connect to a server first.", true);
                return;
            }
            string trimmed = (playerName ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                SetStatus("Enter a player name.", true);
                return;
            }
            _mod.Friends.SendFriendRequest(trimmed);
            SetStatus($"Friend request sent to {trimmed}.");
            _state.NotifyContent();
        }

        public void RespondFriend(string playerId, bool accept)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            _mod.Friends.RespondToRequest(playerId, accept);
            SetStatus(accept ? "Friend request accepted." : "Friend request declined.");
            _state.NotifyContent();
        }

        public void InviteFriend(string playerName)
        {
            if (string.IsNullOrEmpty(_state.CurrentWorldId))
            {
                SetStatus("Join a world before inviting friends.", true);
                return;
            }
            _mod.Friends.InviteFriendToCurrentWorld(playerName);
            SetStatus($"Invited {playerName} to this world.");
        }

        public void SaveProfile(ProfileUpdatePacket update)
        {
            if (!_state.Connected)
            {
                SetStatus("Connect to a server first.", true);
                return;
            }
            _ = _mod.Client.UpdateProfileAsync(update);
            SetStatus("Saving profile...");
        }

        public void AcceptPendingInvite()
        {
            if (string.IsNullOrEmpty(_state.PendingInviteWorldId)) return;
            JoinWorld(_state.PendingInviteWorldId, "invited world");
            _state.PendingInviteWorldId = null;
            _state.PendingInviteFrom = null;
            _state.NotifyContent();
        }

        public void HostLocalServer()
        {
            string executable = ModSettings.ServerExecutablePath;
            if (string.IsNullOrWhiteSpace(executable))
            {
                SetStatus("Set the dedicated server path in Settings first.", true);
                return;
            }
            if (!System.IO.File.Exists(executable))
            {
                SetStatus("The configured server executable was not found.", true);
                return;
            }
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = executable,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(executable),
                    UseShellExecute = true
                });
                SetStatus("Dedicated server launched.");
            }
            catch (Exception e)
            {
                SetStatus("Could not launch the server: " + e.Message, true);
            }
        }

        public void SetStatus(string text, bool isError = false) => _state.SetStatus(text, isError);

        private void OnPacket(PacketType type, string json)
        {
            switch (type)
            {
                case PacketType.AuthResult:
                {
                    AuthResultPacket auth = Newtonsoft.Json.JsonConvert.DeserializeObject<AuthResultPacket>(json);
                    if (auth == null || !auth.Accepted)
                    {
                        _state.Connected = false;
                        SetStatus(auth == null ? "Authentication failed." : (auth.RejectReason ?? "Authentication failed."), true);
                        _state.NotifyContent();
                        break;
                    }
                    _state.Connected = true;
                    _state.PlayerId = auth.PlayerId;
                    SetStatus("Connected.");
                    _state.NotifyContent();
                    _ = _mod.Client.SendAsync(PacketType.FriendListRequest, null);
                    _ = _mod.Client.SendAsync(PacketType.ServerInfoRequest, new { });
                    break;
                }
                case PacketType.ServerInfoResponse:
                {
                    ServerInfoResponsePacket info = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerInfoResponsePacket>(json);
                    if (info == null) break;
                    _state.ServerName = info.ServerName;
                    _state.ServerPlayers = info.OnlinePlayers;
                    _state.ServerMaxPlayers = info.MaxPlayers;
                    _state.Notify();
                    break;
                }
                case PacketType.RoomListResponse:
                {
                    RoomListResponsePacket list = Newtonsoft.Json.JsonConvert.DeserializeObject<RoomListResponsePacket>(json);
                    if (list == null)
                    {
                        _state.LoadingRooms = false;
                        SetStatus("The room list was unavailable.", true);
                        _state.NotifyContent();
                        break;
                    }
                    _state.Rooms.Clear();
                    if (list.Rooms != null) _state.Rooms.AddRange(list.Rooms);
                    _state.LoadingRooms = false;

                    if (!string.IsNullOrEmpty(_state.CurrentRoomId))
                    {
                        RoomSummary current = _state.Rooms.FirstOrDefault(r => r.RoomId == _state.CurrentRoomId);
                        if (current != null)
                        {
                            _state.CurrentRoomName = current.Name;
                            _state.CurrentWorldPlayers = current.PlayersOnline;
                            _state.CurrentWorldBuilds = 0;
                        }
                    }
                    SetStatus($"Found {_state.Rooms.Count} open room(s).");
                    _state.NotifyContent();
                    break;
                }
                case PacketType.RoomState:
                {
                    RoomStatePacket room = Newtonsoft.Json.JsonConvert.DeserializeObject<RoomStatePacket>(json);
                    if (room == null)
                    {
                        SetStatus("The room response was invalid.", true);
                        break;
                    }
                    if (!room.Accepted)
                    {
                        _state.LoadingRooms = false;
                        SetStatus("Room action refused: " + room.Reason, true);
                        _state.NotifyContent();
                        break;
                    }
                    ApplyRoomState(room);
                    _state.NotifyContent();
                    RefreshRooms();
                    break;
                }
                case PacketType.RoomClosed:
                {
                    RoomClosedPacket closed = Newtonsoft.Json.JsonConvert.DeserializeObject<RoomClosedPacket>(json);
                    if (closed == null || (!string.IsNullOrEmpty(_state.CurrentRoomId) && closed.RoomId != _state.CurrentRoomId)) break;
                    _state.CurrentRoomId = null;
                    _state.CurrentRoomName = null;
                    _state.CurrentWorldId = null;
                    _state.CurrentWorldName = null;
                    _state.CurrentWorldBuilds = 0;
                    _state.CurrentWorldPlayers = 0;
                    _state.PendingVote = null;
                    _mod.Builds.ResetWorld();
                    SetStatus(closed.Reason ?? "The room closed.");
                    _state.NotifyContent();
                    RefreshRooms();
                    break;
                }
                case PacketType.WorldJoinAck:
                {
                    WorldJoinAckPacket join = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldJoinAckPacket>(json);
                    if (join == null)
                    {
                        SetStatus("The world response was invalid.", true);
                        break;
                    }
                    if (!join.Accepted)
                    {
                        SetStatus("Join failed: " + join.RejectReason, true);
                        break;
                    }
                    _state.CurrentWorldId = join.WorldId;
                    _state.CurrentRoomId = null;
                    _state.CurrentRoomName = null;
                    _state.CurrentWorldBuilds = join.Builds == null ? 0 : join.Builds.Count;
                    _state.CurrentWorldPlayers = join.PlayersOnline == null ? 0 : join.PlayersOnline.Count;
                    _state.LocalClaimId = FindOwnClaim(join);
                    _state.PendingInviteWorldId = null;
                    _state.PendingInviteFrom = null;
                    SetStatus($"In {_state.RoomLabel()} ({_state.CurrentWorldBuilds} builds).");
                    _state.NotifyContent();
                    break;
                }
                case PacketType.ChatMessage:
                {
                    ChatMessagePacket chat = Newtonsoft.Json.JsonConvert.DeserializeObject<ChatMessagePacket>(json);
                    if (chat == null) break;
                    _chatSequence++;
                    _state.Chat.Add(new ChatLine
                    {
                        From = chat.FromPlayerName,
                        Text = chat.Message,
                        Scope = string.IsNullOrEmpty(chat.WorldId) ? "lobby" : "room",
                        Sequence = _chatSequence
                    });
                    while (_state.Chat.Count > 60) _state.Chat.RemoveAt(0);
                    _state.NotifyContent();
                    break;
                }
                case PacketType.FriendListResponse:
                    _state.NotifyContent();
                    break;
                case PacketType.ClaimCreate:
                {
                    ClaimInfo claim = Newtonsoft.Json.JsonConvert.DeserializeObject<ClaimInfo>(json);
                    if (claim != null && claim.OwnerPlayerId == _state.PlayerId && claim.BuildId == _mod.Builds.LocalBuildId)
                    {
                        _state.LocalClaimId = claim.ClaimId;
                        SetStatus("Your build is claimed.");
                        _state.NotifyContent();
                    }
                    break;
                }
                case PacketType.ClaimRemove:
                {
                    ClaimRemovePacket removed = Newtonsoft.Json.JsonConvert.DeserializeObject<ClaimRemovePacket>(json);
                    if (removed != null && removed.ClaimId == _state.LocalClaimId)
                    {
                        _state.LocalClaimId = null;
                        SetStatus("Claim released.");
                        _state.NotifyContent();
                    }
                    break;
                }
                case PacketType.FriendInviteToWorld:
                {
                    FriendInviteToWorldPacket invite = Newtonsoft.Json.JsonConvert.DeserializeObject<FriendInviteToWorldPacket>(json);
                    if (invite != null && !string.IsNullOrEmpty(invite.WorldId))
                    {
                        _state.PendingInviteWorldId = invite.WorldId;
                        _state.PendingInviteFrom = invite.TargetPlayerName;
                        SetStatus($"World invite from {invite.TargetPlayerName}.");
                        _state.NotifyContent();
                    }
                    break;
                }
                case PacketType.TimeWarpVote:
                {
                    TimeWarpVotePacket vote = Newtonsoft.Json.JsonConvert.DeserializeObject<TimeWarpVotePacket>(json);
                    if (vote == null) break;
                    _state.PendingVote = vote;
                    _state.NotifyContent();
                    Navigate(AppShell.RoomsPageId);
                    break;
                }
                case PacketType.TimeWarpResult:
                {
                    TimeWarpResultPacket result = Newtonsoft.Json.JsonConvert.DeserializeObject<TimeWarpResultPacket>(json);
                    if (result == null) break;
                    _state.PendingVote = null;
                    SetStatus(result.Approved ? $"Time warp is now {result.ActualMultiplier:0.##}x." : result.Reason);
                    _state.NotifyContent();
                    break;
                }
                case PacketType.PlayerConnected:
                {
                    PlayerPresencePacket presence = Newtonsoft.Json.JsonConvert.DeserializeObject<PlayerPresencePacket>(json);
                    if (presence != null) SetStatus($"{presence.PlayerName} connected.");
                    _state.NotifyContent();
                    _ = _mod.Client.SendAsync(PacketType.FriendListRequest, null);
                    break;
                }
                case PacketType.PlayerDisconnected:
                {
                    PlayerPresencePacket presence = Newtonsoft.Json.JsonConvert.DeserializeObject<PlayerPresencePacket>(json);
                    if (presence != null) SetStatus($"{presence.PlayerName} disconnected.");
                    _state.NotifyContent();
                    _ = _mod.Client.SendAsync(PacketType.FriendListRequest, null);
                    break;
                }
                case PacketType.AuthError:
                {
                    AuthErrorPacket error = Newtonsoft.Json.JsonConvert.DeserializeObject<AuthErrorPacket>(json);
                    _state.Connected = false;
                    SetStatus(error == null ? "Authentication failed." : error.Message, true);
                    _state.NotifyContent();
                    break;
                }
                case PacketType.Error:
                {
                    ErrorPacket error = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorPacket>(json);
                    SetStatus(error == null ? "The server reported an error." : error.Message, true);
                    break;
                }
            }
        }

        private void ApplyRoomState(RoomStatePacket room)
        {
            if (room.Room == null)
            {
                _state.CurrentRoomId = null;
                _state.CurrentRoomName = null;
                _state.CurrentWorldId = null;
                _state.CurrentWorldName = null;
                _state.CurrentWorldBuilds = 0;
                _state.CurrentWorldPlayers = 0;
                _state.LocalClaimId = null;
                _state.PendingVote = null;
                _mod.Builds.ResetWorld();
                SetStatus("Left the room.");
                return;
            }

            _state.CurrentRoomId = room.Room.RoomId;
            _state.CurrentRoomName = room.Room.Name;
            _state.CurrentWorldId = room.World == null ? room.Room.WorldId : room.World.WorldId;
            _state.CurrentWorldName = room.Room.Name;
            _state.CurrentWorldBuilds = room.World?.Builds == null ? 0 : room.World.Builds.Count;
            _state.CurrentWorldPlayers = room.Room.PlayersOnline;
            _state.LocalClaimId = room.World == null ? null : FindOwnClaim(room.World);
            _state.PendingInviteWorldId = null;
            _state.PendingInviteFrom = null;
            SetStatus($"In {room.Room.Name} ({_state.CurrentWorldBuilds} builds, {room.Room.PlayersOnline}/{room.Room.Capacity} players).");
        }

        private string FindOwnClaim(WorldJoinAckPacket join)
        {
            if (join.Claims == null || string.IsNullOrEmpty(_state.PlayerId)) return null;
            string localBuild = _mod.Builds.LocalBuildId;
            foreach (ClaimInfo claim in join.Claims)
            {
                if (claim == null) continue;
                if (claim.Shape != ClaimShape.Build) continue;
                if (claim.OwnerPlayerId != _state.PlayerId) continue;
                if (string.IsNullOrEmpty(localBuild) || claim.BuildId == localBuild) return claim.ClaimId;
            }
            return null;
        }
    }
}
