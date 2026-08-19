using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ModLoader.Helpers;
using SFS.UI.ModGUI;
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
        private GameObject _homeHolder;
        private GameObject _holder;
        private Window _window;
        private Label _status;
        private bool _visible;
        private string _host = "127.0.0.1";
        private string _port = "7777";
        private string _playerName = "Pilot";
        private string _worldName = "Shared World";
        private string _serverDirectoryUrl;

        public MultiplayerMenu(ModMain mod)
        {
            _mod = mod;
            _serverDirectoryUrl = ModSettings.DirectoryUrl;
            _playerName = ModSettings.PlayerName;
            _host = ModSettings.Host;
            _port = ModSettings.Port.ToString();
            _mod.Client.OnPacket += OnPacket;
            SceneHelper.OnHomeSceneUnloaded += new Action<Scene>(_ =>
            {
                Hide();
                HideHomeButton();
            });
            SceneHelper.OnWorldSceneUnloaded += new Action<Scene>(_ => Hide());
        }

        public void AttachHomeButton()
        {
            HideHomeButton();
            _homeHolder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "SFSEnhanced_Home");
            Builder.CreateButton(_homeHolder.transform, 260, 54, 0, -250, Show, "MULTIPLAYER");
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
            ModSettings.PlayerName = _playerName;
            ModSettings.Host = _host;
            if (int.TryParse(_port, out var savedPort)) ModSettings.Port = savedPort;

            _holder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "SFSEnhanced_MP");
            _window = Builder.CreateWindow(_holder.transform, Builder.GetRandomID(), 520, 560, 0, 0, true, true, 0.98f, "SFS Enhanced Multiplayer");
            var root = _window.ChildrenHolder;

            Builder.CreateLabel(root, 460, 32, 0, -38, "MULTIPLAYER");
            Builder.CreateLabel(root, 460, 26, 0, -70, "Play with other pilots, host your own server, or browse the community.");
            Builder.CreateButton(root, 210, 42, -115, -120, () => _ = BrowseServersAsync(root), "BROWSE SERVERS");
            Builder.CreateButton(root, 210, 42, 115, -120, ShowHostControls, "HOST SERVER");
            Builder.CreateButton(root, 210, 42, -115, -175, ShowDirectConnect, "DIRECT CONNECT");
            Builder.CreateButton(root, 210, 42, 115, -175, () =>
            {
                if (!_mod.Client.IsConnected)
                {
                    SetStatus("Connect to a server first.");
                    return;
                }
                _mod.Client.SendAsync(PacketType.WorldListRequest, new { });
                SetStatus("Loading worlds...");
            }, "MY WORLDS");

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
            Builder.CreateInputWithLabel(root, 420, 40, 0, -390, "Server port", _port, s => _port = s);
            Builder.CreateInputWithLabel(root, 420, 40, 0, -440, "World name", _worldName, s => _worldName = s);

            Builder.CreateButton(root, 130, 38, -155, -490, () =>
            {
                if (!int.TryParse(_port, out var port)) port = 7777;
                _mod.ConnectToServer(_host, port, _playerName);
                SetStatus($"Connecting to {_host}:{port}...");
            }, "CONNECT");
            Builder.CreateButton(root, 130, 38, 0, -490, () =>
            {
                if (!_mod.Client.IsConnected)
                {
                    SetStatus("Connect to a server first.");
                    return;
                }
                _mod.Client.SendAsync(PacketType.WorldCreate, new WorldCreatePacket { Name = _worldName, IsPublic = true });
                SetStatus("Creating world...");
            }, "CREATE WORLD");
            Builder.CreateButton(root, 130, 38, 155, -490, () =>
            {
                _mod.Client.Disconnect();
                SetStatus("Disconnected");
            }, "DISCONNECT");
            _status = Builder.CreateLabel(root, 460, 34, 0, -532, "Not connected");
        }

        private void ShowDirectConnect()
        {
            SetStatus("Enter the host and port below, then press CONNECT.");
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

        private async Task BrowseServersAsync(Transform root)
        {
            if (string.IsNullOrWhiteSpace(_serverDirectoryUrl))
            {
                SetStatus("Set a server directory URL to browse public servers.");
                return;
            }
            SetStatus("Loading public servers...");
            var servers = await _directory.ListAsync(_serverDirectoryUrl);
            if (!_visible || _window == null) return;
            for (int i = 0; i < servers.Count && i < 6; i++)
            {
                var server = servers[i];
                int y = -80 - (i * 54);
                string text = $"{server.Name}  {server.OnlinePlayers}/{server.MaxPlayers}  {server.Region}";
                Builder.CreateButton(root, 420, 44, 0, y, () => JoinServer(server), text);
            }
            SetStatus(servers.Count == 0 ? "No public servers found." : $"Found {servers.Count} public server(s).");
        }

        private void JoinServer(ServerListing server)
        {
            if (server == null || string.IsNullOrWhiteSpace(server.Host)) return;
            _host = server.Host;
            _port = server.Port.ToString();
            _mod.ConnectToServer(server.Host, server.Port, _playerName);
            SetStatus($"Connecting to {server.Name}...");
        }

        public void Hide()
        {
            _visible = false;
            if (_holder != null)
            {
                UnityEngine.Object.Destroy(_holder);
                _holder = null;
                _window = null;
                _status = null;
            }
        }

        private void HideHomeButton()
        {
            if (_homeHolder == null) return;
            UnityEngine.Object.Destroy(_homeHolder);
            _homeHolder = null;
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
                    SetStatus(join.Accepted ? $"In world {join.WorldId} ({join.Builds.Count} builds)" : $"Join failed: {join.RejectReason}");
                    break;
                case PacketType.WorldListResponse:
                    var list = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldListResponsePacket>(json);
                    SetStatus(list?.Worlds == null ? "World list unavailable." : $"Found {list.Worlds.Count} world(s).");
                    break;
                case PacketType.Error:
                    var error = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorPacket>(json);
                    SetStatus(error?.Message ?? "Server error");
                    break;
            }
        }
    }
}
