using System;
using System.Text;
using SFSEnhanced.Mod.Networking;
using SFSEnhanced.Mod.UI.App;
using SFSEnhanced.Mod.UI.Kit;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;
using SFS.UI.ModGUI;
using UnityEngine;

namespace SFSEnhanced.Mod.UI.App.Pages
{
    internal sealed class ServersPage : AppPage
    {
        private string _host;
        private string _port = string.Empty;
        private string _password = string.Empty;
        private Label _statusLine;
        private Transform _actionRegion;
        private Transform _errorRegion;
        private Transform _listRegion;
        private string _actionKey;
        private string _errorKey;
        private string _listKey;
        private string _identityKey;

        public override string Id => AppShell.ServersPageId;
        public override string Title => "Servers";
        public override string Subtitle => "Connect directly, host locally, or browse the public directory.";

        public override bool NeedsRebuild(IAppActions app)
        {
            ServerIdentityPrompt prompt = app.State.PendingIdentity;
            string key = prompt == null ? string.Empty : prompt.EndpointKey + "|" + prompt.PresentedFingerprint + "|" + prompt.Decision;
            if (!string.Equals(key, _identityKey, StringComparison.Ordinal)) return true;
            return false;
        }

        public override void Build(IAppActions app, Transform host)
        {
            ServerIdentityPrompt prompt = app.State.PendingIdentity;
            _identityKey = prompt == null ? string.Empty : prompt.EndpointKey + "|" + prompt.PresentedFingerprint + "|" + prompt.Decision;
            _statusLine = null;
            _actionRegion = null;
            _errorRegion = null;
            _listRegion = null;
            _actionKey = null;
            _errorKey = null;
            _listKey = null;

            if (prompt != null)
            {
                BuildIdentityPanel(app, host, prompt);
                return;
            }

            if (string.IsNullOrEmpty(_host)) _host = ModSettings.Host;
            if (string.IsNullOrEmpty(_port) && ModSettings.Port > 0) _port = ModSettings.Port.ToString();

            int y = Theme.ContentTop;
            UiKit.Section(host, "DIRECT CONNECT", y);
            y -= 48;
            Builder.CreateInputWithLabel(host, 420, 40, -140, y, "Server host", _host, v => _host = v);
            Builder.CreateInputWithLabel(host, 200, 40, 240, y, "Port (optional)", _port, v => _port = v);

            y -= 48;
            UiKit.Primary(host, 170, 38, -175, y, "CONNECT", () => app.Connect(_host, _port));
            UiKit.Secondary(host, 190, 38, 15, y, "CREATE ACCOUNT", () => app.CreateAccount(_host, _port, null));
            _actionRegion = UiKit.Region(host, 240, y, 60);

            y -= 44;
            _statusLine = UiKit.Small(host, Theme.ContentWidth, 0, y, string.Empty, Theme.TextMuted);

            y -= 22;
            _errorRegion = UiKit.Region(host, 0, y, 150);

            y -= 168;
            UiKit.Divider(host, Theme.ContentWidth, y);
            y -= 22;
            UiKit.Section(host, "PUBLIC DIRECTORY", y);
            UiKit.Secondary(host, 110, 28, 315, y, "REFRESH", app.RefreshServers);

            _listRegion = UiKit.Region(host, 0, y - 120, 170);
            _actionKey = null;
            _listKey = null;
            Update(app);
        }

        public override void Update(IAppActions app)
        {
            AppState state = app.State;

            if (_statusLine != null)
            {
                _statusLine.Text = state.Connected ? $"Connected · {state.ServerLabel()}" : "Not connected";
                _statusLine.Color = state.Connected ? Theme.Positive : Theme.TextMuted;
            }

            if (_errorRegion != null)
            {
                string errorKey = (state.LastConnectErrorCode ?? string.Empty) + "|" + (state.LastConnectError ?? string.Empty) + "|" + _host + "|" + state.Connected;
                if (!string.Equals(errorKey, _errorKey, StringComparison.Ordinal))
                {
                    _errorKey = errorKey;
                    UiKit.Clear(_errorRegion);
                    BuildAuthFeedback(app, state);
                }
            }

            string actionKey = state.Connected ? "connected" : "offline";
            if (_actionRegion != null && actionKey != _actionKey)
            {
                _actionKey = actionKey;
                UiKit.Clear(_actionRegion);
                if (state.Connected) UiKit.Danger(_actionRegion, 180, 38, 0, 0, "DISCONNECT", app.Disconnect);
                else UiKit.Secondary(_actionRegion, 180, 38, 0, 0, "HOST LOCALLY", app.HostLocalServer);
            }

            string listKey = BuildListKey(state);
            if (_listRegion != null && listKey != _listKey)
            {
                _listKey = listKey;
                UiKit.Clear(_listRegion);

                if (string.IsNullOrWhiteSpace(ModSettings.DirectoryUrl))
                {
                    UiKit.EmptyState(_listRegion, 24, "No server directory configured", "Add a directory URL in Settings to discover public servers.");
                    UiKit.Secondary(_listRegion, 200, 34, 0, -66, "OPEN SETTINGS", () => app.Navigate(AppShell.SettingsPageId));
                    return;
                }
                if (state.LoadingServers)
                {
                    UiKit.Small(_listRegion, Theme.ContentWidth, 0, 24, "Loading public servers...", Theme.TextMuted);
                    return;
                }
                if (!string.IsNullOrEmpty(state.DirectoryError))
                {
                    UiKit.Label(_listRegion, Theme.ContentWidth, 20, 0, 32, "COULD NOT LOAD THE DIRECTORY", Theme.BodySize, Theme.Warning);
                    UiKit.Label(_listRegion, Theme.ContentWidth, 44, 0, 4, state.DirectoryError, Theme.SmallSize, Theme.TextPrimary);
                    UiKit.Secondary(_listRegion, 140, 32, -180, -40, "RETRY", app.RefreshServers);
                    UiKit.Small(_listRegion, 520, 60, -40, "Direct connect above still works.", Theme.TextMuted);
                    return;
                }
                if (state.Servers.Count == 0)
                {
                    UiKit.EmptyState(_listRegion, 24, "No public servers listed", "The directory answered with an empty list. Refresh, or connect directly above.");
                    return;
                }

                int rowY = 20;
                int shown = 0;
                foreach (ServerListing server in state.Servers)
                {
                    if (shown >= 3) break;
                    ServerListing captured = server;
                    string port = captured.Port.ToString();
                    string subtitle = $"{captured.OnlinePlayers}/{captured.MaxPlayers} players · {captured.Region ?? "unknown"}";
                    UiKit.ListRow(_listRegion, rowY, captured.Name ?? captured.Host, subtitle, "JOIN", () =>
                    {
                        _host = captured.Host;
                        _port = port;
                        app.Connect(captured.Host, port);
                    });
                    rowY -= Theme.RowStep;
                    shown++;
                }
                if (state.Servers.Count > shown)
                    UiKit.Small(_listRegion, Theme.ContentWidth, 0, rowY + 10, $"Showing {shown} of {state.Servers.Count} servers.", Theme.TextMuted);
            }
        }

        private void BuildAuthFeedback(IAppActions app, AppState state)
        {
            if (state.Connected || state.PendingIdentity != null) return;
            string code = state.LastConnectErrorCode;
            if (string.IsNullOrEmpty(code)) return;
            string message = state.LastConnectError ?? string.Empty;

            switch (code)
            {
                case AuthErrorCode.UnknownAccount:
                    UiKit.Small(_errorRegion, Theme.ContentWidth, 0, 60, "NO ACCOUNT ON THIS SERVER", Theme.Warning);
                    UiKit.Small(_errorRegion, Theme.ContentWidth, 0, 38, message, Theme.TextPrimary);
                    UiKit.Secondary(_errorRegion, 200, 32, -150, 2, "CREATE ACCOUNT", () => app.CreateAccount(_host, _port, null));
                    UiKit.Secondary(_errorRegion, 190, 32, 70, 2, "CHANGE PLAYER NAME", () => app.Navigate(AppShell.SettingsPageId));
                    UiKit.Small(_errorRegion, Theme.ContentWidth, 0, -34, "Creating an account registers this player name on this server only.", Theme.TextMuted);
                    break;

                case AuthErrorCode.NameTaken:
                    UiKit.Small(_errorRegion, Theme.ContentWidth, 0, 60, "NAME ALREADY REGISTERED", Theme.Warning);
                    UiKit.Small(_errorRegion, Theme.ContentWidth, 0, 38, message, Theme.TextPrimary);
                    UiKit.Secondary(_errorRegion, 170, 32, -110, 2, "SIGN IN WITH IT", () => app.Connect(_host, _port));
                    UiKit.Secondary(_errorRegion, 190, 32, 90, 2, "CHANGE PLAYER NAME", () => app.Navigate(AppShell.SettingsPageId));
                    UiKit.Small(_errorRegion, Theme.ContentWidth, 0, -34, "Signing in does not create a second account.", Theme.TextMuted);
                    break;

                case AuthErrorCode.BadToken:
                    UiKit.Small(_errorRegion, Theme.ContentWidth, 0, 60, "SAVED LOGIN REJECTED", Theme.Warning);
                    UiKit.Small(_errorRegion, Theme.ContentWidth, 0, 38, message, Theme.TextPrimary);
                    UiKit.Small(_errorRegion, Theme.ContentWidth, 0, 8, "An account only exists on servers where it was created. Nothing was registered automatically.", Theme.TextMuted);
                    break;

                case AuthErrorCode.RegistrationDisabled:
                    UiKit.Small(_errorRegion, Theme.ContentWidth, 0, 60, "REGISTRATION CLOSED", Theme.Warning);
                    UiKit.Small(_errorRegion, Theme.ContentWidth, 0, 38, message, Theme.TextPrimary);
                    UiKit.Small(_errorRegion, Theme.ContentWidth, 0, 8, "This server does not accept new accounts. Ask the operator to create one for you.", Theme.TextMuted);
                    break;

                case AuthErrorCode.RegistrationPasswordRequired:
                case AuthErrorCode.BadRegistrationPassword:
                    UiKit.Small(_errorRegion, Theme.ContentWidth, 0, 60, code == AuthErrorCode.BadRegistrationPassword ? "SERVER PASSWORD REFUSED" : "SERVER PASSWORD REQUIRED", Theme.Warning);
                    UiKit.Small(_errorRegion, Theme.ContentWidth, 0, 38, message, Theme.TextPrimary);
                    Builder.CreateInputWithLabel(_errorRegion, 420, 40, -140, 4, "Server password", _password, v => _password = v);
                    UiKit.Primary(_errorRegion, 190, 34, -155, -44, "CONTINUE", () => app.RetryWithPassword(_password));
                    UiKit.Small(_errorRegion, Theme.ContentWidth, 0, -64, "The password is sent only inside the encrypted channel.", Theme.TextMuted);
                    break;

                default:
                    UiKit.Small(_errorRegion, Theme.ContentWidth, 0, 60, "CONNECTION FAILED", Theme.Warning);
                    UiKit.Small(_errorRegion, Theme.ContentWidth, 0, 38, message, Theme.TextPrimary);
                    break;
            }
        }

        private static void BuildIdentityPanel(IAppActions app, Transform host, ServerIdentityPrompt prompt)
        {
            int y = Theme.ContentTop;

            if (!prompt.IsTrustPrompt)
            {
                UiKit.Section(host, "SERVER IDENTITY UNAVAILABLE", y);
                y -= 34;
                UiKit.Body(host, prompt.Reason ?? "The server did not present a usable identity key.", y);
                y -= 40;
                UiKit.Secondary(host, 160, 36, 0, y, "CLOSE", app.CancelIdentityPrompt);
                return;
            }

            UiKit.Section(host, prompt.IsChanged ? "SERVER IDENTITY CHANGED" : "SERVER IDENTITY NOT TRUSTED", y);

            y -= 34;
            UiKit.Body(host, prompt.DisplayAuthority, y);

            if (!string.IsNullOrEmpty(prompt.ResolvedHost) &&
                !string.Equals(prompt.ResolvedHost, prompt.DisplayAuthority, StringComparison.OrdinalIgnoreCase))
            {
                y -= 22;
                UiKit.Small(host, Theme.ContentWidth, 0, y, $"Resolved to {prompt.ResolvedHost}:{prompt.ResolvedPort}", Theme.TextMuted);
            }

            y -= 30;
            UiKit.Small(host, Theme.ContentWidth, 0, y, "FINGERPRINT", Theme.TextSecondary);
            foreach (string line in FingerprintLines(prompt.PresentedFingerprint))
            {
                y -= 22;
                UiKit.Label(host, Theme.ContentWidth, 20, 0, y, line, 15f, Theme.TextPrimary);
            }

            y -= 24;
            UiKit.Small(host, Theme.ContentWidth, 0, y, "Compare out of band: " + prompt.PresentedShortFingerprint, Theme.Accent);

            if (prompt.IsChanged && !string.IsNullOrEmpty(prompt.PinnedFingerprint))
            {
                y -= 22;
                UiKit.Small(host, Theme.ContentWidth, 0, y, "Previously trusted: " + prompt.PinnedFingerprint, Theme.Warning);
            }

            y -= 26;
            if (prompt.IsChanged)
            {
                UiKit.Small(host, Theme.ContentWidth, 0, y, "This server's identity changed. Someone may be impersonating it. Cancel unless you expected this.", Theme.Danger);
                y -= 20;
                UiKit.Small(host, Theme.ContentWidth, 0, y, "Nothing was sent and no account was created. Replacing the pin takes two presses and keeps the previous fingerprint on record.", Theme.TextMuted);
            }
            else
            {
                UiKit.Small(host, Theme.ContentWidth, 0, y, "This fingerprint is not verified. Your server console prints it at startup; compare it with the owner out of band.", Theme.TextMuted);
                y -= 20;
                UiKit.Small(host, Theme.ContentWidth, 0, y, "Accepting without comparing cannot detect an impostor on this first connection.", Theme.TextMuted);
            }

            y -= 44;
            if (prompt.IsChanged)
            {
                UiKit.Danger(host, 260, 36, -95, y, "REPLACE TRUSTED FINGERPRINT", app.ReplaceServerIdentity);
                UiKit.Secondary(host, 140, 36, 175, y, "CANCEL", app.CancelIdentityPrompt);
            }
            else
            {
                UiKit.Primary(host, 200, 36, -110, y, "TRUST AND CONNECT", app.TrustServer);
                UiKit.Secondary(host, 140, 36, 140, y, "CANCEL", app.CancelIdentityPrompt);
            }

            y -= 32;
            UiKit.Small(host, Theme.ContentWidth, 0, y, "Trusted servers are listed in Settings, where a fingerprint can be removed.", Theme.TextMuted);
        }

        private static string[] FingerprintLines(string fingerprint)
        {
            string value = fingerprint ?? string.Empty;
            int lines = value.Length <= 16 ? 1 : (value.Length + 15) / 16;
            var result = new string[lines];
            for (int index = 0; index < lines; index++)
            {
                int start = index * 16;
                result[index] = value.Substring(start, Math.Min(16, value.Length - start));
            }
            return result;
        }

        private static string BuildListKey(AppState state)
        {
            var builder = new StringBuilder();
            builder.Append(ModSettings.DirectoryUrl).Append('|').Append(state.LoadingServers).Append('|').Append(state.DirectoryError).Append('|');
            foreach (ServerListing server in state.Servers)
            {
                builder.Append(server.ServerId).Append(',').Append(server.Host).Append(',')
                    .Append(server.Port).Append(',').Append(server.OnlinePlayers).Append(',')
                    .Append(server.MaxPlayers).Append(',').Append(server.Region).Append(';');
            }
            return builder.ToString();
        }
    }
}
