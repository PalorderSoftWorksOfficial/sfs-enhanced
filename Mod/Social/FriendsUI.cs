using System.Collections.Generic;
using UnityEngine;
using SFSEnhanced.Mod.Networking;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Mod.Social
{
    /// <summary>
    /// Thin controller between the network layer and an in-game friends panel.
    /// The actual UI (buttons, list rendering) needs to be built with SFS's UI
    /// toolkit — TODO(game-hook): SFS uses Unity UGUI (UnityEngine.UI) per its
    /// dependency list, so this would be a prefab-based panel like the game's
    /// existing menus; wire the calls below to your panel's button OnClick events.
    /// </summary>
    public class FriendsUI
    {
        private readonly NetClient _client;
        public List<FriendInfo> Friends { get; private set; } = new();
        public List<FriendInfo> IncomingRequests { get; private set; } = new();
        public List<FriendInfo> OutgoingRequests { get; private set; } = new();

        public FriendsUI(NetClient client)
        {
            _client = client;
            _client.OnPacket += HandlePacket;
        }

        private void HandlePacket(PacketType type, string json)
        {
            if (type != PacketType.FriendListResponse) return;
            var resp = Newtonsoft.Json.JsonConvert.DeserializeObject<FriendListResponsePacket>(json);
            Friends = resp.Friends;
            IncomingRequests = resp.PendingIncoming;
            OutgoingRequests = resp.PendingOutgoing;
            Debug.Log($"[SFSEnhanced] Friends updated: {Friends.Count} friends, {IncomingRequests.Count} pending.");
            // TODO(game-hook): refresh your friends panel's list view here.
        }

        public void SendFriendRequest(string playerName) =>
            _ = _client.SendAsync(PacketType.FriendRequest, new FriendRequestPacket { TargetPlayerName = playerName });

        public void RespondToRequest(string fromPlayerId, bool accept) =>
            _ = _client.SendAsync(PacketType.FriendRequestResponse, new FriendRequestResponsePacket
            {
                FromPlayerId = fromPlayerId,
                Accepted = accept,
            });

        public void RefreshFriendsList() =>
            _ = _client.SendAsync(PacketType.FriendListRequest, null);

        public void InviteFriendToCurrentWorld(string playerName) =>
            _ = _client.SendAsync(PacketType.FriendInviteToWorld, new FriendInviteToWorldPacket
            {
                TargetPlayerName = playerName,
                WorldId = _client.CurrentWorldId,
            });
    }
}
