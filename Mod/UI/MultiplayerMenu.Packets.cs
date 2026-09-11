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
        private void OnPacket(PacketType type, string json)
        {
            switch (type)
            {
                case PacketType.AuthResult:
                    var hello = Newtonsoft.Json.JsonConvert.DeserializeObject<AuthResultPacket>(json);
                    if (hello != null && hello.Accepted) SetStatus($"Online as {_mod.Client.PlayerId}");
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
                case PacketType.TimeWarpVote:
                    var vote = Newtonsoft.Json.JsonConvert.DeserializeObject<TimeWarpVotePacket>(json);
                    if (vote != null) ShowTimeWarpVote(vote);
                    break;
                case PacketType.TimeWarpResult:
                    var result = Newtonsoft.Json.JsonConvert.DeserializeObject<TimeWarpResultPacket>(json);
                    if (result != null) SetStatus(result.Approved ? $"Timewarp: {result.ActualMultiplier:0.##}x" : result.Reason);
                    break;
                case PacketType.PlayerConnected:
                    var connected = Newtonsoft.Json.JsonConvert.DeserializeObject<PlayerPresencePacket>(json);
                    if (connected != null) SetStatus($"{connected.PlayerName} connected.");
                    break;
                case PacketType.PlayerDisconnected:
                    var disconnected = Newtonsoft.Json.JsonConvert.DeserializeObject<PlayerPresencePacket>(json);
                    if (disconnected != null) SetStatus($"{disconnected.PlayerName} disconnected.");
                    break;
                case PacketType.Error:
                    var error = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorPacket>(json);
                    SetStatus(error?.Message ?? "Server error");
                    break;
            }
        }
    }
}
