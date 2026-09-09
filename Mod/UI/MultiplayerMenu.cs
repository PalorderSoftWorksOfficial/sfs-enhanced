using System;
using System.Collections.Generic;
using SFS.UI.ModGUI;
using SFSEnhanced.Mod.Networking;
using SFSEnhanced.Shared.Protocol;
using UnityEngine;

namespace SFSEnhanced.Mod.UI
{
    public sealed class MultiplayerMenu
    {
        private readonly ModMain _mod;
        private GameObject _homeHolder;
        private Button _homeButton;
        private GameObject _holder;
        private Window _window;
        private Transform _root;
        private Label _status;
        private GameObject _worldListHolder;
        private bool _visible;
        private string _host;
        private string _port;
        private string _playerName;
        private string _worldName = "Shared World";

        public MultiplayerMenu(ModMain mod)
        {
            _mod = mod;
            _host = ModSettings.Host;
            _port = ModSettings.Port.ToString();
            _playerName = ModSettings.PlayerName;
            _mod.Client.OnPacket += OnPacket;
        }

        public void EnsureHomeButton()
        {
            if (_homeButton != null && _homeButton.gameObject != null)
                return;

            var buttons = GameObject.Find("Buttons");
            if (buttons == null)
                return;

            var existing = buttons.transform.Find("SFSEnhanced_MultiplayerButton");
            if (existing != null)
            {
                _homeButton = existing.GetComponent<Button>();
                if (_homeButton != null)
                    return;
                UnityEngine.Object.Destroy(existing.gameObject);
            }

            _homeHolder = new GameObject("SFSEnhanced_Home");
            _homeHolder.transform.SetParent(buttons.transform, false);
            _homeButton = Builder.CreateButton(_homeHolder.transform, 280, 64, 0, -350, Toggle, "MULTIPLAYER");
            _homeButton.gameObject.name = "SFSEnhanced_MultiplayerButton";
            _homeHolder.transform.SetAsLastSibling();
            Debug.Log("[SFSEnhanced] Multiplayer button attached.");
        }

        public void Toggle()
        {
            if (_visible && _holder != null)
                Hide();
            else
                Show();
        }

        public void Show()
        {
            if (_holder != null)
                Hide();

            _visible = true;
            _host = string.IsNullOrWhiteSpace(_host) ? "127.0.0.1" : _host;
            _port = string.IsNullOrWhiteSpace(_port) ? "7777" : _port;
            _playerName = string.IsNullOrWhiteSpace(_playerName) ? "Pilot" : _playerName;

            _holder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "SFSEnhanced_MP");
            _window = Builder.CreateWindow(_holder.transform, Builder.GetRandomID(), 520, 600, 0, 0, true, true, 0.98f, "SFS Enhanced Multiplayer");
            _root = _window.ChildrenHolder;

            Builder.CreateLabel(_root, 460, 36, 0, -35, "SFS ENHANCED");
            Builder.CreateLabel(_root, 460, 28, 0, -70, "Multiplayer");
            Builder.CreateInputWithLabel(_root, 420, 40, 0, -125, "Player name", _playerName, value => _playerName = value);
            Builder.CreateInputWithLabel(_root, 420, 40, 0, -180, "Server host", _host, value => _host = value);
            Builder.CreateInputWithLabel(_root, 420, 40, 0, -235, "Server port", _port, value => _port = value);
            Builder.CreateInputWithLabel(_root, 420, 40, 0, -290, "World name", _worldName, value => _worldName = value);
            Builder.CreateButton(_root, 190, 42, -105, -350, Connect, "CONNECT");
            Builder.CreateButton(_root, 190, 42, 105, -350, Disconnect, "DISCONNECT");
            Builder.CreateButton(_root, 190, 42, -105, -405, RequestWorlds, "REFRESH WORLDS");
            Builder.CreateButton(_root, 190, 42, 105, -405, CreateWorld, "CREATE WORLD");
            _status = Builder.CreateLabel(_root, 460, 54, 0, -460, _mod.Client.IsConnected ? "Connected" : "Not connected");
            Builder.CreateLabel(_root, 460, 28, 0, -505, "Available worlds");
            _worldListHolder = new GameObject("SFSEnhanced_WorldList");
            _worldListHolder.transform.SetParent(_root, false);

            if (_mod.Client.IsConnected)
                RequestWorlds();
            else
                RenderWorlds(new List<WorldSummary>());
        }

        public void Hide()
        {
            _visible = false;
            if (_holder != null)
                UnityEngine.Object.Destroy(_holder);
            _holder = null;
            _window = null;
            _root = null;
            _status = null;
            _worldListHolder = null;
        }

        private void Connect()
        {
            if (!int.TryParse(_port, out var port) || port < 1 || port > 65535)
            {
                SetStatus("Invalid server port.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_host))
            {
                SetStatus("Enter a server host.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_playerName))
            {
                SetStatus("Enter a player name.");
                return;
            }

            ModSettings.Host = _host.Trim();
            ModSettings.Port = port;
            ModSettings.PlayerName = _playerName.Trim();
            SetStatus($"Connecting to {ModSettings.Host}:{port}...");
            _mod.Client.ConnectAsync(ModSettings.Host, port, ModSettings.PlayerName);
        }

        private void Disconnect()
        {
            _mod.Client.Disconnect();
            SetStatus("Disconnected.");
            RenderWorlds(new List<WorldSummary>());
        }

        private void RequestWorlds()
        {
            if (!_mod.Client.IsConnected)
            {
                SetStatus("Connect to a server first.");
                return;
            }

            SetStatus("Loading worlds...");
            _mod.Client.SendAsync(PacketType.WorldListRequest, new { });
        }

        private void CreateWorld()
        {
            if (!_mod.Client.IsConnected)
            {
                SetStatus("Connect to a server first.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_worldName))
            {
                SetStatus("Enter a world name.");
                return;
            }

            SetStatus("Creating world...");
            _mod.Client.SendAsync(PacketType.WorldCreate, new WorldCreatePacket
            {
                Name = _worldName.Trim(),
                IsPublic = true
            });
        }

        private void JoinWorld(string worldId)
        {
            if (!_mod.Client.IsConnected)
            {
                SetStatus("Connect to a server first.");
                return;
            }

            SetStatus("Joining world...");
            _mod.Client.SendAsync(PacketType.WorldJoin, new WorldJoinPacket
            {
                WorldId = worldId
            });
        }

        private void OnPacket(PacketType type, string json)
        {
            if (!_visible)
                return;

            try
            {
                switch (type)
                {
                    case PacketType.HelloAck:
                    {
                        var ack = Newtonsoft.Json.JsonConvert.DeserializeObject<HelloAckPacket>(json);
                        if (ack != null && ack.Accepted)
                        {
                            SetStatus("Connected.");
                            RequestWorlds();
                        }
                        break;
                    }
                    case PacketType.ServerInfoResponse:
                    {
                        var info = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerInfoResponsePacket>(json);
                        if (info != null)
                            SetStatus($"{info.ServerName} | {info.OnlinePlayers}/{info.MaxPlayers}");
                        break;
                    }
                    case PacketType.WorldListResponse:
                    {
                        var response = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldListResponsePacket>(json);
                        RenderWorlds(response?.Worlds ?? new List<WorldSummary>());
                        SetStatus("World list updated.");
                        break;
                    }
                    case PacketType.WorldJoinAck:
                    {
                        var response = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldJoinAckPacket>(json);
                        if (response != null)
                            SetStatus(response.Accepted ? $"Joined {response.WorldId}." : $"Join failed: {response.RejectReason}");
                        break;
                    }
                    case PacketType.Error:
                    {
                        var error = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorPacket>(json);
                        SetStatus(error?.Message ?? "Server error.");
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SFSEnhanced] UI packet handling failed: {e}");
            }
        }

        private void RenderWorlds(List<WorldSummary> worlds)
        {
            if (_worldListHolder == null)
                return;

            for (var i = _worldListHolder.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_worldListHolder.transform.GetChild(i).gameObject);

            if (worlds == null || worlds.Count == 0)
            {
                Builder.CreateLabel(_worldListHolder.transform, 440, 30, 0, 0, "No worlds available.");
                return;
            }

            var index = 0;
            foreach (var world in worlds)
            {
                var capturedWorldId = world.WorldId;
                Builder.CreateButton(_worldListHolder.transform, 440, 40, 0, -index * 44, () => JoinWorld(capturedWorldId), world.Name);
                index++;
                if (index >= 10)
                    break;
            }
        }

        private void SetStatus(string message)
        {
            if (_root == null)
                return;

            if (_status != null)
                UnityEngine.Object.Destroy(_status.gameObject);

            _status = Builder.CreateLabel(_root, 460, 54, 0, -460, message);
        }
    }
}
