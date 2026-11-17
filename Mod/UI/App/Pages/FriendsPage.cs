using System.Collections.Generic;
using System.Text;
using SFSEnhanced.Mod.UI.App;
using SFSEnhanced.Mod.UI.Kit;
using SFSEnhanced.Shared.Protocol;
using SFS.UI.ModGUI;
using TMPro;
using UnityEngine;

namespace SFSEnhanced.Mod.UI.App.Pages
{
    internal sealed class FriendsPage : AppPage
    {
        private string _friendName = string.Empty;
        private Transform _friendsRegion;
        private Transform _requestsRegion;
        private Label _requestsSummary;
        private Label _friendsSummary;
        private string _friendsKey;
        private string _requestsKey;

        public override string Id => AppShell.FriendsPageId;
        public override string Title => "Friends";
        public override string Subtitle => "Requests, presence and world invites, all in one place.";

        public override void Build(IAppActions app, Transform host)
        {
            int y = Theme.ContentTop;
            UiKit.Section(host, "ADD A FRIEND", y);
            y -= 46;
            Builder.CreateInputWithLabel(host, 500, 40, -100, y, "Player name", _friendName, v => _friendName = v);
            UiKit.Primary(host, 130, 38, 280, y, "ADD", () => app.AddFriend(_friendName));

            UiKit.Section(host, "FRIENDS", y - 56);
            _friendsSummary = UiKit.Label(host, 320, 18, 80, y - 56, string.Empty, Theme.SmallSize, Theme.TextMuted, TextAlignmentOptions.Right);
            UiKit.Secondary(host, 110, 28, 315, y - 56, "REFRESH", app.RefreshFriends);
            _friendsRegion = UiKit.Region(host, 0, y - 144, 140);
            UiKit.Section(host, "PENDING REQUESTS", y - 256);
            _requestsSummary = UiKit.Label(host, 340, 18, 205, y - 256, string.Empty, Theme.SmallSize, Theme.TextMuted, TextAlignmentOptions.Right);
            _requestsRegion = UiKit.Region(host, 0, y - 308, 96);

            _friendsKey = null;
            _requestsKey = null;
            Update(app);
        }

        public override void Update(IAppActions app)
        {
            AppState state = app.State;
            List<FriendInfo> friends = app.Mod.Friends.Friends;

            string friendsKey = BuildFriendsKey(state, friends);
            if (_friendsRegion != null && friendsKey != _friendsKey)
            {
                _friendsKey = friendsKey;
                UiKit.Clear(_friendsRegion);

                int shown = 0;
                if (!state.Connected)
                {
                    UiKit.EmptyState(_friendsRegion, 30, "Not connected", "Friends are server-side accounts. Connect to load your list.");
                }
                else if (friends.Count == 0)
                {
                    UiKit.EmptyState(_friendsRegion, 30, "No friends yet", "Send a request above. Presence appears as soon as they are online.");
                }
                else
                {
                    int rowY = 24;
                    foreach (FriendInfo friend in friends)
                    {
                        if (shown >= 3) break;
                        FriendInfo captured = friend;
                        string subtitle = captured.Online
                            ? (string.IsNullOrEmpty(captured.CurrentWorldId) ? "Online · in the lobby" : "Online · in a world")
                            : "Offline";
                        string action = captured.Online && !string.IsNullOrEmpty(state.CurrentWorldId) ? "INVITE" : null;
                        System.Action invite = action == null ? (System.Action)null : () => app.InviteFriend(captured.PlayerName);
                        UiKit.ListRow(_friendsRegion, rowY, captured.PlayerName, subtitle, action, invite);
                        rowY -= 44;
                        shown++;
                    }
                }

                if (_friendsSummary != null)
                {
                    if (!state.Connected) _friendsSummary.Text = string.Empty;
                    else if (friends.Count == 0) _friendsSummary.Text = "No friends";
                    else if (friends.Count > shown) _friendsSummary.Text = $"{shown} of {friends.Count} friends";
                    else _friendsSummary.Text = friends.Count == 1 ? "1 friend" : $"{friends.Count} friends";
                }
            }

            if (_friendsRegion == null && _friendsSummary != null) _friendsSummary.Text = string.Empty;

            string requestsKey = BuildRequestsKey(app.Mod.Friends.IncomingRequests, app.Mod.Friends.OutgoingRequests);
            if (_requestsRegion != null && requestsKey != _requestsKey)
            {
                _requestsKey = requestsKey;
                UiKit.Clear(_requestsRegion);

                int incomingShown = 0;
                foreach (FriendInfo request in app.Mod.Friends.IncomingRequests)
                {
                    if (incomingShown >= 2) break;
                    FriendInfo captured = request;
                    int cardY = 20 - incomingShown * 46;
                    UiKit.Card(_requestsRegion, Theme.ContentWidth, Theme.RowHeight, 0, cardY, Theme.Row, 0.5f);
                    UiKit.Label(_requestsRegion, 380, 20, -165, cardY, $"{captured.PlayerName} wants to be friends", 14f, Theme.TextPrimary);
                    UiKit.Primary(_requestsRegion, 90, 28, 210, cardY, "ACCEPT", () => app.RespondFriend(captured.PlayerId, true));
                    UiKit.Danger(_requestsRegion, 90, 28, 310, cardY, "DECLINE", () => app.RespondFriend(captured.PlayerId, false));
                    incomingShown++;
                }

                int incomingTotal = app.Mod.Friends.IncomingRequests.Count;
                int outgoingTotal = app.Mod.Friends.OutgoingRequests.Count;
                if (incomingTotal == 0 && outgoingTotal == 0)
                {
                    UiKit.Small(_requestsRegion, Theme.ContentWidth, 0, 20, "No pending requests.", Theme.TextMuted);
                }

                if (_requestsSummary != null)
                {
                    if (incomingTotal == 0 && outgoingTotal == 0)
                    {
                        _requestsSummary.Text = string.Empty;
                    }
                    else
                    {
                        string incomingText = incomingTotal > incomingShown
                            ? $"{incomingShown} of {incomingTotal} incoming"
                            : $"{incomingTotal} incoming";
                        _requestsSummary.Text = $"{incomingText} · {outgoingTotal} outgoing";
                    }
                }
            }
        }

        private static string BuildFriendsKey(AppState state, List<FriendInfo> friends)
        {
            var builder = new StringBuilder();
            builder.Append(state.Connected).Append('|').Append(state.CurrentWorldId).Append('|');
            foreach (FriendInfo friend in friends)
                builder.Append(friend.PlayerId).Append(',').Append(friend.Online).Append(',').Append(friend.CurrentWorldId).Append(';');
            return builder.ToString();
        }

        private static string BuildRequestsKey(List<FriendInfo> incoming, List<FriendInfo> outgoing)
        {
            var builder = new StringBuilder();
            foreach (FriendInfo request in incoming) builder.Append('i').Append(request.PlayerId).Append(';');
            foreach (FriendInfo request in outgoing) builder.Append('o').Append(request.PlayerId).Append(';');
            return builder.ToString();
        }
    }
}
