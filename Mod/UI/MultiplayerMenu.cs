using System;
using ModLoader.Helpers;
using SFS.UI.ModGUI;
using SFSEnhanced.Shared.Protocol;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SFSEnhanced.Mod.UI
{
    /// <summary>Native ModGUI window: connect, create/join world, basic status.</summary>
    public class MultiplayerMenu
    {
        private readonly ModMain _mod;
        private GameObject _holder;
        private Window _window;
        private Label _status;
        private string _host = "127.0.0.1";
        private string _port = "7777";
        private string _playerName = "Pilot";
        private string _worldName = "Shared World";
        private bool _visible;

        public MultiplayerMenu(ModMain mod)
        {
            _mod = mod;
            _mod.Client.OnPacket += OnPacket;
            SceneHelper.OnHomeSceneUnloaded += new Action<Scene>(_ => Hide());
            SceneHelper.OnWorldSceneUnloaded += new Action<Scene>(_ => Hide());
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

            _holder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "SFSEnhanced_MP");
            _window = Builder.CreateWindow(
                _holder.transform,
                Builder.GetRandomID(),
                360, 420,
                0, 0,
                draggable: true,
                savePosition: true,
                opacity: 0.95f,
                titleText: "SFS Enhanced");

            Transform root = _window.ChildrenHolder;

            Builder.CreateLabel(root, 320, 28, 0, -40, "Multiplayer");

            Builder.CreateInputWithLabel(root, 320, 40, 0, -90, "Host", _host, s => _host = s);
            Builder.CreateInputWithLabel(root, 320, 40, 0, -140, "Port", _port, s => _port = s);
            Builder.CreateInputWithLabel(root, 320, 40, 0, -190, "Name", _playerName, s => _playerName = s);
            Builder.CreateInputWithLabel(root, 320, 40, 0, -240, "World", _worldName, s => _worldName = s);

            Builder.CreateButton(root, 150, 36, -80, -300, () =>
            {
                if (!int.TryParse(_port, out int port)) port = 7777;
                _mod.ConnectToServer(_host, port, _playerName);
                SetStatus("Connecting...");
            }, "Connect");

            Builder.CreateButton(root, 150, 36, 80, -300, () =>
            {
                _ = _mod.Client.SendAsync(PacketType.WorldCreate, new WorldCreatePacket
                {
                    Name = _worldName,
                    IsPublic = true,
                });
                SetStatus("Creating world...");
            }, "Create world");

            Builder.CreateButton(root, 150, 36, -80, -350, async () =>
            {
                await _mod.Client.SendAsync(PacketType.WorldListRequest, new { });
                SetStatus("Refreshing worlds...");
            }, "List worlds");

            Builder.CreateButton(root, 150, 36, 80, -350, () =>
            {
                _mod.Client.Disconnect();
                SetStatus("Disconnected");
            }, "Disconnect");

            _status = Builder.CreateLabel(root, 320, 28, 0, -390, "Not connected");
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

        private void SetStatus(string text)
        {
            if (_status != null) _status.Text = text;
            Debug.Log("[SFSEnhanced] " + text);
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
                    SetStatus(join.Accepted
                        ? $"In world {join.WorldId} ({join.Builds.Count} builds)"
                        : $"Join failed: {join.RejectReason}");
                    break;
                case PacketType.WorldListResponse:
                    var list = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldListResponsePacket>(json);
                    if (list?.Worlds != null && list.Worlds.Count > 0)
                    {
                        var first = list.Worlds[0];
                        SetStatus($"Joining {first.Name}...");
                        _ = _mod.Client.SendAsync(PacketType.WorldJoin, new WorldJoinPacket { WorldId = first.WorldId });
                    }
                    else SetStatus("No worlds on server");
                    break;
                case PacketType.Error:
                    SetStatus("Server error");
                    break;
            }
        }
    }
}
