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
            Builder.CreateButton(root, 150, 38, -165, -390, () => RequestTimewarp(1), "STOP WARP");
            Builder.CreateButton(root, 150, 38, 0, -390, () => RequestTimewarp(4), "4X WARP");
            Builder.CreateButton(root, 150, 38, 165, -390, () => RequestTimewarp(16), "16X WARP");
            Builder.CreateButton(root, 180, 38, 0, -445, HideWorldToolsPanel, "BACK");
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

        private void RequestServerInfo()
        {
            if (!_mod.Client.IsConnected)
            {
                SetStatus("Connect to a server first.");
                return;
            }
            SetStatus("Loading server information...");
            _ = _mod.Client.SendAsync(PacketType.ServerInfoRequest, new { });
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

        private void ShowTimeWarpVote(TimeWarpVotePacket vote)
        {
            if (_timeWarpVoteHolder != null) UnityEngine.Object.Destroy(_timeWarpVoteHolder);
            _timeWarpVoteHolder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "SFSEnhanced_TimeWarpVote");
            var window = Builder.CreateWindow(_timeWarpVoteHolder.transform, Builder.GetRandomID(), 420, 260, 0, 0, true, true, 0.98f, "Timewarp Vote");
            var root = window.ChildrenHolder;
            Builder.CreateLabel(root, 380, 34, 0, -38, $"{vote.RequesterPlayerName} requests {vote.RequestedMultiplier:0.##}x timewarp");
            Builder.CreateButton(root, 160, 42, -90, -100, () => SubmitTimeWarpVote(vote, true), "APPROVE");
            Builder.CreateButton(root, 160, 42, 90, -100, () => SubmitTimeWarpVote(vote, false), "REJECT");
            Builder.CreateLabel(root, 380, 30, 0, -165, "All players must approve the request.");
        }

        private void SubmitTimeWarpVote(TimeWarpVotePacket vote, bool approved)
        {
            if (_timeWarpVoteHolder != null) UnityEngine.Object.Destroy(_timeWarpVoteHolder);
            _timeWarpVoteHolder = null;
            _ = _mod.Client.SendAsync(PacketType.TimeWarpVoteResponse, new TimeWarpVoteResponsePacket { WorldId = vote.WorldId, VoteId = vote.VoteId, Approved = approved });
            SetStatus(approved ? "Timewarp vote approved." : "Timewarp vote rejected.");
        }
        private void RequestTimewarp(double multiplier)
        {
            if (!_mod.Client.IsConnected || string.IsNullOrEmpty(_mod.Client.CurrentWorldId))
            {
                SetStatus("Join a world to control timewarp.");
                return;
            }
            _ = _mod.Client.SendAsync(PacketType.TimeWarpRequest, new TimeWarpRequestPacket { WorldId = _mod.Client.CurrentWorldId, RequestedMultiplier = multiplier });
            SetStatus($"Requested {multiplier:0.##}x timewarp.");
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
            List<ServerListing> servers;
            try { servers = await _directory.ListAsync(_serverDirectoryUrl); }
            catch (Exception e)
            {
                SetStatus("Server directory unreachable: " + e.Message);
                return;
            }
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
            _mod.ConnectToServer(server.Host, server.Port, _playerName);
            SetStatus($"Connecting to {server.Name}...");
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

    }
}
