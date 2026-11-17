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
        private bool _isPublic = true;
        private Transform _topRegion;
        private Transform _listRegion;
        private Button _claimButton;
        private string _topKey;
        private string _listKey;
        private string _claimId;
        private bool _unconnectedBuilt;

        public override string Id => AppShell.WorldsPageId;
        public override string Title => "Worlds";
        public override string Subtitle => "Joinable worlds on this server. Today a world is both the live save and the discovery entry; rooms are a planned separate layer.";

        public override bool NeedsRebuild(IAppActions app) => (!app.State.Connected) != _unconnectedBuilt;

        public override void Build(IAppActions app, Transform host)
        {
            AppState state = app.State;
            _unconnectedBuilt = !state.Connected;

            _topRegion = UiKit.Region(host, 0, 110, 210);
            UiKit.Section(host, "PUBLIC WORLDS", -88);
            UiKit.Secondary(host, 110, 28, 315, -88, "REFRESH", app.RefreshWorlds);
            _listRegion = UiKit.Region(host, 0, -150, 150);

            _topKey = null;
            _listKey = null;
            _claimId = null;
            Update(app);
        }

        public override void Update(IAppActions app)
        {
            AppState state = app.State;

            string topKey = state.Connected + "|" + (state.CurrentWorldId ?? string.Empty) + "|" + (state.PendingVote == null ? 0 : state.PendingVote.VoteId);
            if (_topRegion != null && topKey != _topKey)
            {
                _topKey = topKey;
                _claimButton = null;
                UiKit.Clear(_topRegion);

                if (!state.Connected)
                {
                    UiKit.EmptyState(_topRegion, 40, "Not connected", "Worlds live on a server. Connect first, then browse or create one.");
                    UiKit.Primary(_topRegion, 220, 38, 0, -60, "GO TO SERVERS", () => app.Navigate(AppShell.ServersPageId));
                }
                else if (!string.IsNullOrEmpty(state.CurrentWorldId))
                {
                    UiKit.Section(_topRegion, "CURRENT WORLD", 90);
                    UiKit.Card(_topRegion, Theme.ContentWidth, 104, 0, 30, Theme.Row, 0.6f);
                    UiKit.Label(_topRegion, 470, 24, -120, 66, state.RoomLabel(), 18f, Theme.TextPrimary);
                    UiKit.Label(_topRegion, 470, 18, -120, 42, $"{state.CurrentWorldBuilds} builds · {state.CurrentWorldPlayers} players · id {UiKit.ShortId(state.CurrentWorldId)}", Theme.SmallSize, Theme.TextSecondary);
                    UiKit.Primary(_topRegion, 130, 32, 270, 68, "LEAVE", app.LeaveWorld);
                    _claimButton = UiKit.Secondary(_topRegion, 130, 32, 270, 34, ClaimLabel(state), app.ClaimToggle);
                    _claimId = state.LocalClaimId;

                    int voteY = -40;
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
                else
                {
                    UiKit.Section(_topRegion, "CREATE A WORLD", 90);
                    Builder.CreateInputWithLabel(_topRegion, 420, 40, -140, 46, "World name", _roomName, v => _roomName = v);
                    Builder.CreateToggleWithLabel(_topRegion, 220, 34, () => _isPublic, () => _isPublic = !_isPublic, 250, 46, "Public world");
                    UiKit.Primary(_topRegion, 180, 38, -170, -10, "CREATE WORLD", () => app.CreateWorld(_roomName, _isPublic));
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
                    UiKit.Small(_listRegion, Theme.ContentWidth, 0, 30, "Connect to a server to list its worlds.", Theme.TextMuted);
                    return;
                }
                if (state.LoadingWorlds)
                {
                    UiKit.Small(_listRegion, Theme.ContentWidth, 0, 30, "Loading worlds...", Theme.TextMuted);
                    return;
                }
                if (state.Worlds.Count == 0)
                {
                    UiKit.EmptyState(_listRegion, 20, "No public worlds yet", state.CurrentWorldId == null
                        ? "Create the first world above, or refresh."
                        : "Create another world once you leave this one.");
                    return;
                }

                int rowY = 20;
                int shown = 0;
                foreach (WorldSummary world in state.Worlds)
                {
                    if (shown >= 2) break;
                    WorldSummary captured = world;
                    string subtitle = $"{captured.PlayersOnline} online · {captured.BuildCount} builds · by {captured.OwnerName ?? "unknown"}";
                    bool inThisWorld = captured.WorldId == state.CurrentWorldId;
                    System.Action join = inThisWorld ? (System.Action)null : () => app.JoinWorld(captured.WorldId, captured.Name);
                    UiKit.ListRow(_listRegion, rowY, captured.Name ?? captured.WorldId, subtitle, inThisWorld ? null : "JOIN", join);
                    rowY -= Theme.RowStep;
                    shown++;
                }
                if (state.Worlds.Count > shown)
                    UiKit.Small(_listRegion, Theme.ContentWidth, 0, rowY + 10, $"Showing {shown} of {state.Worlds.Count} worlds.", Theme.TextMuted);
            }
        }

        private static string ClaimLabel(AppState state) => string.IsNullOrEmpty(state.LocalClaimId) ? "CLAIM BUILD" : "UNCLAIM";

        private static string BuildListKey(AppState state)
        {
            var builder = new StringBuilder();
            builder.Append(state.Connected).Append('|').Append(state.LoadingWorlds).Append('|');
            foreach (WorldSummary world in state.Worlds)
            {
                builder.Append(world.WorldId).Append(',').Append(world.PlayersOnline).Append(',')
                    .Append(world.BuildCount).Append(',').Append(world.Name).Append(';');
            }
            return builder.ToString();
        }
    }
}
