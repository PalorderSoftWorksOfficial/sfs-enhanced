using System.Collections.Concurrent;
using System.Linq;
using SFSEnhanced.Server.Persistence;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Server.Social
{
    /// <summary>
    /// Server-side friends list, persisted on the PlayerAccount so it survives
    /// restarts and follows the player across whichever world they're in —
    /// matches "friends are server-side, not a code you paste in" from
    /// docs/FEATURE_RESEARCH.md.
    /// </summary>
    public class FriendsService
    {
        private readonly AccountService _accounts;

        // playerId -> connected? (NetServer updates this on connect/disconnect)
        private readonly ConcurrentDictionary<string, bool> _online = new();
        private readonly ConcurrentDictionary<string, string> _currentWorld = new();

        public FriendsService(AccountService accounts) => _accounts = accounts;

        public void SetOnline(string playerId, bool online)
        {
            _online[playerId] = online;
            if (!online) _currentWorld.TryRemove(playerId, out _);
        }

        public void SetCurrentWorld(string playerId, string worldId) => _currentWorld[playerId] = worldId;

        public bool RequestFriend(PlayerAccount requester, string targetName, out string error)
        {
            error = null;
            var target = _accounts.FindByName(targetName);
            if (target == null) { error = "No player with that name has connected to this server yet."; return false; }
            if (target.PlayerId == requester.PlayerId) { error = "You can't friend yourself."; return false; }
            if (requester.FriendPlayerIds.Contains(target.PlayerId)) { error = "Already friends."; return false; }

            if (!requester.OutgoingFriendRequests.Contains(target.PlayerId))
                requester.OutgoingFriendRequests.Add(target.PlayerId);
            if (!target.IncomingFriendRequests.Contains(requester.PlayerId))
                target.IncomingFriendRequests.Add(requester.PlayerId);

            _accounts.Save(requester);
            _accounts.Save(target);
            return true;
        }

        public bool RespondToRequest(PlayerAccount responder, string fromPlayerId, bool accept)
        {
            var requester = _accounts.FindById(fromPlayerId);
            if (requester == null) return false;

            responder.IncomingFriendRequests.Remove(fromPlayerId);
            requester.OutgoingFriendRequests.Remove(responder.PlayerId);

            if (accept)
            {
                if (!responder.FriendPlayerIds.Contains(fromPlayerId)) responder.FriendPlayerIds.Add(fromPlayerId);
                if (!requester.FriendPlayerIds.Contains(responder.PlayerId)) requester.FriendPlayerIds.Add(responder.PlayerId);
            }

            _accounts.Save(responder);
            _accounts.Save(requester);
            return true;
        }

        public FriendListResponsePacket BuildFriendList(PlayerAccount account)
        {
            FriendInfo ToInfo(string id)
            {
                var acc = _accounts.FindById(id);
                return new FriendInfo
                {
                    PlayerId = id,
                    PlayerName = acc?.PlayerName ?? "(unknown)",
                    Online = _online.TryGetValue(id, out var on) && on,
                    CurrentWorldId = _currentWorld.TryGetValue(id, out var w) ? w : null,
                };
            }

            return new FriendListResponsePacket
            {
                Friends = account.FriendPlayerIds.Select(ToInfo).ToList(),
                PendingIncoming = account.IncomingFriendRequests.Select(ToInfo).ToList(),
                PendingOutgoing = account.OutgoingFriendRequests.Select(ToInfo).ToList(),
            };
        }

        public bool AreFriends(string playerIdA, string playerIdB)
        {
            var a = _accounts.FindById(playerIdA);
            return a != null && a.FriendPlayerIds.Contains(playerIdB);
        }
    }
}
