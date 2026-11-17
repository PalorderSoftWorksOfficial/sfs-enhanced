using System;
using System.Threading.Tasks;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Server.Networking
{
    public partial class NetServer
    {
        private async Task HandleFriendRequest(ClientConnection conn, FriendRequestPacket req)
        {
            bool ok = _friends.RequestFriend(conn.Account, req?.TargetPlayerName, out string error);
            if (!ok)
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = error });
                return;
            }
            var target = _accounts.FindByName(req.TargetPlayerName);
            if (target != null && _connections.TryGetValue(target.PlayerId, out var targetConn)) await targetConn.SendAsync(PacketType.FriendListResponse, _friends.BuildFriendList(target));
            await conn.SendAsync(PacketType.FriendListResponse, _friends.BuildFriendList(conn.Account));
        }

        private async Task HandleFriendResponse(ClientConnection conn, FriendRequestResponsePacket resp)
        {
            _friends.RespondToRequest(conn.Account, resp?.FromPlayerId, resp != null && resp.Accepted);
            await conn.SendAsync(PacketType.FriendListResponse, _friends.BuildFriendList(conn.Account));
            if (resp != null && _connections.TryGetValue(resp.FromPlayerId, out var otherConn)) await otherConn.SendAsync(PacketType.FriendListResponse, _friends.BuildFriendList(otherConn.Account));
        }

        private async Task HandleClaimCreate(ClientConnection conn, ClaimCreatePacket req)
        {
            if (req == null || req.WorldId != conn.CurrentWorldId) return;
            if (!TryConsume(conn.Account.PlayerId, "claim", 6, 1.0))
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "You are creating claims too quickly. Wait a moment and retry." });
                return;
            }
            if (req.Shape != ClaimShape.Build && req.Shape != ClaimShape.Region)
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "Unknown claim type." });
                return;
            }
            if (req.Shape == ClaimShape.Region && !IsValidRegionClaim(req))
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "A region claim needs a finite centre and a radius between 1 and 50000 metres." });
                return;
            }
            if (req.Shape == ClaimShape.Build)
            {
                var build = _worlds.FindBuild(req.WorldId, req.BuildId);
                if (build == null || build.OwnerPlayerId != conn.Account.PlayerId)
                {
                    await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "You can only claim your own build." });
                    return;
                }
            }
            var claim = _claims.Create(req.WorldId, conn.Account.PlayerId, conn.Account.PlayerName, req);
            if (claim == null)
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "That claim is outside the server's limits." });
                return;
            }
            await BroadcastToWorld(req.WorldId, PacketType.ClaimCreate, claim, null);
        }

        private async Task HandleClaimRemove(ClientConnection conn, ClaimRemovePacket req)
        {
            if (req == null || req.WorldId != conn.CurrentWorldId || string.IsNullOrEmpty(req.ClaimId)) return;
            if (_claims.Remove(req.WorldId, req.ClaimId, conn.Account.PlayerId)) await BroadcastToWorld(req.WorldId, PacketType.ClaimRemove, req, null);
        }

        private async Task HandleChat(ClientConnection conn, ChatMessagePacket msg)
        {
            if (msg == null || string.IsNullOrWhiteSpace(msg.Message)) return;
            if (!TryConsume(conn.Account.PlayerId, "chat", 8, 2.0))
                return;
            msg.Message = msg.Message.Trim();
            if (msg.Message.Length > 512) msg.Message = msg.Message.Substring(0, 512);
            msg.FromPlayerName = conn.Account.PlayerName;
            msg.SentUtc = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(msg.WorldId))
            {
                if (msg.WorldId != conn.CurrentWorldId) return;
                await BroadcastToWorld(msg.WorldId, PacketType.ChatMessage, msg, null);
            }
            else await BroadcastToAll(PacketType.ChatMessage, msg, null);
        }

        private async Task HandleFriendInvite(ClientConnection conn, FriendInviteToWorldPacket invite)
        {
            if (invite == null) return;
            if (!TryConsume(conn.Account.PlayerId, "invite", 5, 0.5))
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "You are sending invites too quickly. Wait a moment and retry." });
                return;
            }
            var target = _accounts.FindByName(invite.TargetPlayerName);
            if (target == null)
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "Player not found." });
                return;
            }
            if (!_connections.TryGetValue(target.PlayerId, out var targetConn))
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "That player is offline." });
                return;
            }
            string worldId = invite.WorldId ?? conn.CurrentWorldId;
            await targetConn.SendAsync(PacketType.FriendInviteToWorld, new FriendInviteToWorldPacket { TargetPlayerName = conn.Account.PlayerName, WorldId = worldId });
        }
    }
}
