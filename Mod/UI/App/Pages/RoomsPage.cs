using System.Text;
using SFSEnhanced.Mod.UI.App;
using SFSEnhanced.Mod.UI.Kit;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;
using SFS.UI.ModGUI;
using UnityEngine;

namespace SFSEnhanced.Mod.UI.App.Pages
{
    internal sealed class RoomsPage : AppPage
    {
        private string _roomName = string.Empty;
        private string _roomDescription = string.Empty;
        private string _roomCapacity = "8";
        private bool _isPublic = true;
        private Transform _topRegion;
        private Transform _listRegion;
        private Button _claimButton;
        private string _topKey;
        private string _listKey;
        private string _claimId;
        private bool _unconnectedBuilt;

        public override string Id => AppShell.RoomsPageId;
        public override string Title => "Rooms";
        public override string Subtitle => "A room is a live session hosting one world save on this server. The world outlives the room.";

        public override bool NeedsRebuild(IAppActions app) => (!app.State.Connected) != _unconnectedBuilt;

        public override void Build(IAppActions app, Transform host)
        {
            AppState state = app.State;
            _unconnectedBuilt = !state.Connected;

            _topRegion = UiKit.Region(host, 0, 110, 210);
            UiKit.Section(host, "OPEN ROOMS", -88);
            UiKit.Secondary(host, 110, 28, 315, -88, "REFRESH", app.RefreshRooms);
            _listRegion = UiKit.Region(host, 0, -150, 150);

            _topKey = null;
            _listKey = null;
            _claimId = null;
            Update(app);
        }

        public override void Update(IAppActions app)
        {
            AppState state = app.State;

            string topKey = state.Connected + "|" + (state.CurrentRoomId ?? string.Empty) + "|" +
                            (state.PendingVote == null ? 0 : state.PendingVote.VoteId) + "|" +
                            (state.CurrentRoomName ?? string.Empty);
            if (_topRegion != null && topKey != _topKey)
            {
                _topKey = topKey;
                _claimButton = null;
                UiKit.Clear(_topRegion);

                if (!state.Connected)
                {
                    UiKit.EmptyState(_topRegion, 40, "Not connected", "Rooms live on a server. Connect first, then open or join one.");
                    UiKit.Primary(_topRegion, 220, 38, 0, -60, "GO TO SERVERS", () => app.Navigate(AppShell.ServersPageId));
                }
                else if (!string.IsNullOrEmpty(state.CurrentRoomId))
                {
                    BuildCurrentRoom(app, state);
                }
                else
                {
                    BuildCreateRoom(app);
                }
            }

            if (_claimButton != null && state.LocalClaimId != _claimId)
            {
                _claimId = state.LocalClaimId;
                _claimButton.Text = ClaimLabel(state);
            }

            string listKey = BuildListKey(state);
            if (_listRegion != null && listKey != _listKey)
            {
                _listKey = listKey;
                UiKit.Clear(_listRegion);

                if (!state.Connected)
                {
                    UiKit.Small(_listRegion, Theme.ContentWidth, 0, 30, "Connect to a server to list its rooms.", Theme.TextMuted);
                    return;
                }
                if (state.LoadingRooms)
                {
                    UiKit.Small(_listRegion, Theme.ContentWidth, 0, 24, "Loading rooms...", Theme.TextMuted);
                    return;
                }
                if (state.Rooms.Count == 0)
                {
                    UiKit.EmptyState(_listRegion, 20, "No open rooms", state.CurrentRoomId == null
                        ? "Open the first room above, or refresh."
                        : "Open another room once you leave this one.");
                    return;
                }

                int rowY = 20;
                int shown = 0;
                foreach (RoomSummary room in state.Rooms)
                {
                    if (shown >= 3) break;
                    RoomSummary captured = room;
                    bool current = captured.RoomId == state.CurrentRoomId;
                    bool full = captured.PlayersOnline >= captured.Capacity;
                    string visibility = captured.Visibility == RoomVisibility.Public ? "Public" : "Friends only";
                    string subtitle = $"{captured.HostPlayerName} · {captured.PlayersOnline}/{captured.Capacity} · {visibility}";
                    if (!string.IsNullOrEmpty(captured.Description)) subtitle += " · " + Trim(captured.Description, 70);
                    if (full && !current) subtitle += " · FULL";
                    System.Action join = current || full ? (System.Action)null : () => app.JoinRoom(captured.RoomId, captured.Name);
                    UiKit.ListRow(_listRegion, rowY, captured.Name ?? captured.RoomId, subtitle, current || full ? null : "JOIN", join);
                    rowY -= Theme.RowStep;
                    shown++;
                }
                if (state.Rooms.Count > shown)
                    UiKit.Small(_listRegion, Theme.ContentWidth, 0, rowY + 10, $"Showing {shown} of {state.Rooms.Count} rooms.", Theme.TextMuted);
            }
        }

        private void BuildCreateRoom(IAppActions app)
        {
            UiKit.Section(_topRegion, "OPEN A ROOM", 92);
            Builder.CreateInputWithLabel(_topRegion, 300, 40, -210, 60, "Room name", _roomName, v => _roomName = v);
            Builder.CreateInputWithLabel(_topRegion, 110, 40, 90, 60, "Players", _roomCapacity, v => _roomCapacity = v);
            Builder.CreateToggleWithLabel(_topRegion, 210, 34, () => _isPublic, () => _isPublic = !_isPublic, 280, 60, "Public room");
            Builder.CreateInputWithLabel(_topRegion, 700, 40, 0, 14, "Description (optional)", _roomDescription, v => _roomDescription = v);
            UiKit.Primary(_topRegion, 190, 38, -255, -34, "OPEN ROOM", () => app.CreateRoom(_roomName, _roomDescription, _isPublic, _roomCapacity));
            UiKit.Small(_topRegion, 470, 55, -34, _isPublic ? "Listed for anyone on this server." : "Friends-only: only you and your friends on this server can join.", Theme.TextMuted);
            UiKit.Small(_topRegion, Theme.ContentWidth, 0, -72, "Opening a room creates a private world save for it. Capacity is enforced by the server (2-64).", Theme.TextMuted);
        }

        private void BuildCurrentRoom(IAppActions app, AppState state)
        {
            bool isHost = IsHost(state);
            UiKit.Section(_topRegion, "CURRENT ROOM", 92);
            UiKit.Card(_topRegion, Theme.ContentWidth, 96, 0, 34, Theme.Row, 0.6f);
            UiKit.Label(_topRegion, 430, 24, -150, 62, state.RoomLabel(), 18f, Theme.TextPrimary);
            UiKit.Label(_topRegion, 430, 18, -150, 40, $"{state.CurrentWorldBuilds} builds · {state.CurrentWorldPlayers} players · id {UiKit.ShortId(state.CurrentRoomId)}", Theme.SmallSize, Theme.TextSecondary);
            UiKit.Primary(_topRegion, 130, 32, 276, 68, "LEAVE", app.LeaveRoom);
            if (isHost) UiKit.Danger(_topRegion, 130, 32, 276, 30, "CLOSE ROOM", app.CloseRoom);
            _claimButton = UiKit.Secondary(_topRegion, 130, 32, 140, 68, ClaimLabel(state), app.ClaimToggle);
            _claimId = state.LocalClaimId;

            int voteY = -46;
            if (state.PendingVote == null)
            {
                UiKit.Section(_topRegion, "TIME WARP", voteY + 12);
                UiKit.Primary(_topRegion, 110, 30, 60, voteY - 18, "STOP", () => app.RequestTimewarp(1));
                UiKit.Secondary(_topRegion, 110, 30, 180, voteY - 18, "4X", () => app.RequestTimewarp(4));
                UiKit.Secondary(_topRegion, 110, 30, 300, voteY - 18, "16X", () => app.RequestTimewarp(16));
            }
            else
            {
                TimeWarpVotePacket vote = state.PendingVote;
                UiKit.Card(_topRegion, Theme.ContentWidth, 72, 0, voteY - 8, Theme.Surface, 0.6f);
                UiKit.Label(_topRegion, 450, 22, -120, voteY + 10, $"{vote.RequesterPlayerName} requests {vote.RequestedMultiplier:0.##}x warp", 14f, Theme.Warning);
                UiKit.Primary(_topRegion, 110, 30, 180, voteY - 10, "APPROVE", () => app.AnswerVote(true));
                UiKit.Danger(_topRegion, 110, 30, 300, voteY - 10, "REJECT", () => app.AnswerVote(false));
            }
        }

        private static bool IsHost(AppState state)
        {
            foreach (RoomSummary room in state.Rooms)
            {
                if (room.RoomId != state.CurrentRoomId) continue;
                return room.HostPlayerId == state.PlayerId;
            }
            return false;
        }

        private static string ClaimLabel(AppState state) => string.IsNullOrEmpty(state.LocalClaimId) ? "CLAIM BUILD" : "UNCLAIM";

        private static string Trim(string value, int max) => value.Length <= max ? value : value.Substring(0, max - 1) + "…";

        private static string BuildListKey(AppState state)
        {
            var builder = new StringBuilder();
            builder.Append(state.Connected).Append('|').Append(state.LoadingRooms).Append('|').Append(state.PlayerId).Append('|');
            foreach (RoomSummary room in state.Rooms)
            {
                builder.Append(room.RoomId).Append(',').Append(room.PlayersOnline).Append(',')
                    .Append(room.Capacity).Append(',').Append(room.Name).Append(',')
                    .Append(room.HostPlayerId).Append(',').Append(room.Description).Append(';');
            }
            return builder.ToString();
        }
    }
}
