using System;
using System.Collections.Generic;
using System.Text;
using SFSEnhanced.Mod.UI.App;
using SFSEnhanced.Mod.UI.Kit;
using SFSEnhanced.Shared.Networking;
using SFS.UI.ModGUI;
using UnityEngine;

namespace SFSEnhanced.Mod.UI.App.Pages
{
    internal sealed class SettingsPage : AppPage
    {
        private Transform _pinRegion;
        private string _pinKey;

        public override string Id => AppShell.SettingsPageId;
        public override string Title => "Settings";
        public override string Subtitle => "Local connection, discovery and hosting preferences.";

        public override void Build(IAppActions app, Transform host)
        {
            int y = Theme.ContentTop;

            UiKit.Section(host, "LOCAL PLAYER", y);
            y -= 44;
            Builder.CreateInputWithLabel(host, 460, 40, -140, y, "Player name", ModSettings.PlayerName, v => app.SetPlayerName(v));
            UiKit.Small(host, Theme.ContentWidth, 0, y - 32, "The player name is the account key on a server. Changes apply the next time you connect, not to the current session.", Theme.TextMuted);

            y -= 78;
            UiKit.Section(host, "DISCOVERY", y);
            y -= 44;
            Builder.CreateInputWithLabel(host, 460, 40, -140, y, "Directory URL", ModSettings.DirectoryUrl, v => ModSettings.DirectoryUrl = v);
            UiKit.Small(host, Theme.ContentWidth, 0, y - 32, "Used by the Servers page to list public servers. Leave empty to connect directly only.", Theme.TextMuted);

            y -= 78;
            UiKit.Section(host, "HOSTING", y);
            y -= 44;
            Builder.CreateInputWithLabel(host, 460, 40, -140, y, "Server executable", ModSettings.ServerExecutablePath, v => ModSettings.ServerExecutablePath = v);
            UiKit.Small(host, Theme.ContentWidth, 0, y - 32, "Optional path used by HOST LOCALLY on the Servers page to launch your own dedicated server.", Theme.TextMuted);

            y -= 76;
            if (!string.IsNullOrEmpty(app.State.MigrationNotice))
            {
                UiKit.Small(host, Theme.ContentWidth, 0, y, app.State.MigrationNotice, Theme.Warning);
                y -= 40;
            }
            else
            {
                UiKit.Small(host, Theme.ContentWidth, 0, y, "Everything here is stored locally in this game's player preferences.", Theme.TextMuted);
                y -= 40;
            }

            UiKit.Section(host, "TRUSTED SERVERS", y);
            y -= 30;
            UiKit.Small(host, Theme.ContentWidth, 0, y, "A pinned fingerprint is trusted on this device, not verified by a third party.", Theme.TextMuted);
            y -= 46;
            _pinRegion = UiKit.Region(host, 0, y, 120);
            _pinKey = null;
            Update(app);
        }

        public override void Update(IAppActions app)
        {
            if (_pinRegion == null) return;
            IReadOnlyList<ServerIdentityPin> pins = app.Mod?.Client?.Pins?.All() ?? new List<ServerIdentityPin>();

            string key = BuildKey(pins);
            if (string.Equals(key, _pinKey, StringComparison.Ordinal)) return;
            _pinKey = key;
            UiKit.Clear(_pinRegion);

            if (pins.Count == 0)
            {
                UiKit.Small(_pinRegion, Theme.ContentWidth, 0, 16, "No trusted servers yet. Accepting a server's identity on the Servers page adds one here.", Theme.TextMuted);
                return;
            }

            int rowY = 24;
            int shown = 0;
            foreach (ServerIdentityPin pin in pins)
            {
                if (shown >= 2) break;
                ServerIdentityPin captured = pin;
                string subtitle = $"{captured.ShortFingerprint} · trusted {captured.LastVerifiedUtc:yyyy-MM-dd}";
                UiKit.ListRow(_pinRegion, rowY, DisplayKey(captured.EndpointKey), subtitle, "FORGET", () => app.ForgetTrustedServer(captured.EndpointKey));
                rowY -= Theme.RowStep;
                shown++;
            }
            if (pins.Count > shown)
                UiKit.Small(_pinRegion, Theme.ContentWidth, 0, rowY + 6, $"Showing {shown} of {pins.Count} trusted servers.", Theme.TextMuted);
        }

        private static string DisplayKey(string endpointKey)
        {
            if (string.IsNullOrEmpty(endpointKey)) return "-";
            if (endpointKey.EndsWith(EndpointKey.AutoSuffix, StringComparison.Ordinal))
                return endpointKey.Substring(0, endpointKey.Length - EndpointKey.AutoSuffix.Length) + " (automatic port)";
            return endpointKey;
        }

        private static string BuildKey(IReadOnlyList<ServerIdentityPin> pins)
        {
            var builder = new StringBuilder();
            foreach (ServerIdentityPin pin in pins)
                builder.Append(pin.EndpointKey).Append('|').Append(pin.Fingerprint).Append(';');
            return builder.ToString();
        }
    }
}
