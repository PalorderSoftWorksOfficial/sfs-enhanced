using SFSEnhanced.Mod.UI.App;
using SFSEnhanced.Mod.UI.Kit;
using SFSEnhanced.Shared.Protocol;
using SFS.UI.ModGUI;
using UnityEngine;

namespace SFSEnhanced.Mod.UI.App.Pages
{
    internal sealed class HomePage : AppPage
    {
        private Label _profileValue;
        private Label _serverValue;
        private Label _worldValue;
        private Transform _inviteRegion;
        private Transform _continueRegion;
        private string _inviteKey;
        private string _continueKey;

        public override string Id => AppShell.HomePageId;
        public override string Title => "Home";
        public override string Subtitle => "Your session, account and shortcuts in one place.";

        public override void Build(IAppActions app, Transform host)
        {
            int y = Theme.ContentTop;
            UiKit.Section(host, "AT A GLANCE", y);
            y -= 54;
            _profileValue = UiKit.Stat(host, -250, y, "Profile", string.Empty);
            _serverValue = UiKit.Stat(host, 0, y, "Server", string.Empty);
            _worldValue = UiKit.Stat(host, 250, y, "World", string.Empty);

            _inviteRegion = UiKit.Region(host, 0, 30, 76);
            _continueRegion = UiKit.Region(host, 0, -70, 120);

            UiKit.Section(host, "QUICK ACTIONS", -126);
            UiKit.Primary(host, 232, 40, -250, -170, "BROWSE SERVERS", () => app.Navigate(AppShell.ServersPageId));
            UiKit.Secondary(host, 232, 40, 0, -170, "BROWSE ROOMS", () => app.Navigate(AppShell.RoomsPageId));
            UiKit.Secondary(host, 232, 40, 250, -170, "YOUR PROFILE", () => app.Navigate(AppShell.ProfilePageId));

            _inviteKey = null;
            _continueKey = null;
            Update(app);
        }

        public override void Update(IAppActions app)
        {
            AppState state = app.State;
            PlayerProfile profile = app.Mod.Client.SelfProfile;

            if (_profileValue != null) _profileValue.Text = profile != null ? profile.DisplayName : (state.Connected ? "Syncing..." : "Not connected");
            if (_serverValue != null) _serverValue.Text = state.ServerLabel();
            if (_worldValue != null) _worldValue.Text = state.RoomLabel();

            string inviteKey = (state.PendingInviteWorldId ?? string.Empty) + "|" + (state.PendingInviteFrom ?? string.Empty);
            if (_inviteRegion != null && inviteKey != _inviteKey)
            {
                _inviteKey = inviteKey;
                UiKit.Clear(_inviteRegion);
                if (!string.IsNullOrEmpty(state.PendingInviteWorldId))
                {
                    UiKit.Section(_inviteRegion, "INVITE", 34);
                    UiKit.ListRow(_inviteRegion, 0, $"Invite from {state.PendingInviteFrom}", "Accepting joins their world", "ACCEPT", app.AcceptPendingInvite);
                }
            }

            string continueKey = (state.CurrentWorldId ?? string.Empty) + "|" + state.CurrentWorldBuilds + "|" + state.CurrentWorldPlayers;
            if (_continueRegion != null && continueKey != _continueKey)
            {
                _continueKey = continueKey;
                UiKit.Clear(_continueRegion);
                UiKit.Section(_continueRegion, "CONTINUE", 54);
                if (!string.IsNullOrEmpty(state.CurrentWorldId))
                {
                    UiKit.ListRow(_continueRegion, 14, state.RoomLabel(), $"{state.CurrentWorldBuilds} builds · {state.CurrentWorldPlayers} players", "OPEN", () => app.Navigate(AppShell.RoomsPageId));
                }
                else
                {
                    UiKit.EmptyState(_continueRegion, 14, "You are not in a room", "Join a public room or open your own to fly with other players.");
                }
            }
        }
    }
}
