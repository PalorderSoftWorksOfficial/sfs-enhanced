using System;
using SFSEnhanced.Mod.UI.App;
using SFSEnhanced.Mod.UI.Kit;
using SFSEnhanced.Shared.Protocol;
using SFS.UI.ModGUI;
using UnityEngine;

namespace SFSEnhanced.Mod.UI.App.Pages
{
    internal sealed class ProfilePage : AppPage
    {
        private bool _initialised;
        private bool _builtWithoutProfile;
        private string _displayName = string.Empty;
        private string _bio = string.Empty;
        private string _accent = string.Empty;
        private bool _allowFriendRequests = true;
        private bool _showPresence = true;

        private Label _nameLabel;
        private Label _identityLabel;
        private Label _bioLabel;
        private Label _worldsValue;
        private Label _friendsValue;
        private Label _sessionsValue;

        public override string Id => AppShell.ProfilePageId;
        public override string Title => "Profile";
        public override string Subtitle => "The profile other players see on this server. Accounts are per-server until the platform identity service exists.";

        public override bool NeedsRebuild(IAppActions app) => (app.Mod.Client.SelfProfile == null) != _builtWithoutProfile;

        public override void Build(IAppActions app, Transform host)
        {
            PlayerProfile profile = app.Mod.Client.SelfProfile;
            _builtWithoutProfile = profile == null;

            if (profile == null)
            {
                UiKit.EmptyState(host, 80, "No account loaded", "Profiles live on the server. Connect to load your profile, stats and privacy settings.");
                UiKit.Primary(host, 220, 38, 0, -20, "GO TO SERVERS", () => app.Navigate(AppShell.ServersPageId));
                return;
            }

            if (!_initialised) Initialise(profile);

            int y = Theme.ContentTop;
            UiKit.Card(host, Theme.ContentWidth, 92, 0, y - 34, Theme.Row, 0.6f);
            UiKit.Card(host, 60, 60, -325, y - 34, Theme.Parse(profile.AvatarAccent), 0.95f);
            UiKit.Label(host, 60, 30, -325, y - 34, Initials(profile.DisplayName), 20f, Theme.Surface, TMPro.TextAlignmentOptions.Center);
            _nameLabel = UiKit.Label(host, 520, 26, -120, y - 12, profile.DisplayName, 20f, Theme.TextPrimary);
            _identityLabel = UiKit.Label(host, 520, 18, -120, y - 34, string.Empty, Theme.SmallSize, Theme.TextSecondary);
            _bioLabel = UiKit.Label(host, 520, 20, -120, y - 56, string.Empty, Theme.SmallSize, Theme.TextMuted);

            y -= 96;
            _worldsValue = UiKit.Stat(host, -250, y, "Worlds owned", profile.WorldsOwned.ToString());
            _friendsValue = UiKit.Stat(host, 0, y, "Friends", profile.FriendsCount.ToString());
            _sessionsValue = UiKit.Stat(host, 250, y, "Sessions", profile.SessionsCount.ToString());

            y -= 78;
            UiKit.Section(host, "EDIT PROFILE", y);
            y -= 44;
            int formTop = y;
            Builder.CreateInputWithLabel(host, 420, 40, -160, formTop, "Display name", _displayName, v => _displayName = v);
            Builder.CreateInputWithLabel(host, 420, 40, -160, formTop - 50, "Bio", _bio, v => _bio = v);
            Builder.CreateInputWithLabel(host, 420, 40, -160, formTop - 100, "Accent (#RRGGBB)", _accent, v => _accent = v);

            Builder.CreateToggleWithLabel(host, 240, 32, () => _showPresence, () => _showPresence = !_showPresence, 250, formTop, "Show presence");
            Builder.CreateToggleWithLabel(host, 240, 32, () => _allowFriendRequests, () => _allowFriendRequests = !_allowFriendRequests, 250, formTop - 50, "Allow friend requests");

            UiKit.Primary(host, 160, 34, -220, formTop - 150, "SAVE", Save(app));
            UiKit.Secondary(host, 160, 34, -40, formTop - 150, "RESET", () => { _initialised = false; app.Navigate(AppShell.ProfilePageId); });
            UiKit.Small(host, 180, 170, formTop - 150, "Server-validated fields.", Theme.TextMuted);

            Update(app);
        }

        public override void Update(IAppActions app)
        {
            PlayerProfile profile = app.Mod.Client.SelfProfile;
            if (profile == null || _nameLabel == null) return;

            _nameLabel.Text = profile.DisplayName;
            _identityLabel.Text = $"@{profile.PlayerName} · member since {UiKit.FriendlyDate(profile.MemberSinceUtc)}";
            _bioLabel.Text = string.IsNullOrEmpty(profile.Bio) ? "No bio yet." : profile.Bio;
            if (_worldsValue != null) _worldsValue.Text = profile.WorldsOwned.ToString();
            if (_friendsValue != null) _friendsValue.Text = profile.FriendsCount.ToString();
            if (_sessionsValue != null) _sessionsValue.Text = profile.SessionsCount.ToString();
        }

        private Action Save(IAppActions app)
        {
            return () => app.SaveProfile(new ProfileUpdatePacket
            {
                DisplayName = _displayName,
                Bio = _bio,
                AvatarAccent = _accent,
                AllowFriendRequests = _allowFriendRequests,
                ShowPresence = _showPresence,
            });
        }

        private void Initialise(PlayerProfile profile)
        {
            _displayName = profile.DisplayName ?? string.Empty;
            _bio = profile.Bio ?? string.Empty;
            _accent = string.IsNullOrEmpty(profile.AvatarAccent) ? Theme.DefaultAccentHex : profile.AvatarAccent;
            _allowFriendRequests = profile.AllowFriendRequests;
            _showPresence = profile.ShowPresence;
            _initialised = true;
        }

        private static string Initials(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return "?";
            string trimmed = displayName.Trim();
            return trimmed.Length == 1 ? trimmed.ToUpperInvariant() : trimmed.Substring(0, 2).ToUpperInvariant();
        }
    }
}
