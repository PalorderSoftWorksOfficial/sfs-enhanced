using System;
using System.Collections.Generic;
using SFSEnhanced.Mod.UI.App.Pages;
using SFSEnhanced.Mod.UI.Kit;
using SFSEnhanced.Shared.Protocol;
using SFS.UI.ModGUI;
using TMPro;
using UnityEngine;

namespace SFSEnhanced.Mod.UI.App
{
    internal sealed class AppShell
    {
        public const string HomePageId = "home";
        public const string ServersPageId = "servers";
        public const string WorldsPageId = "worlds";
        public const string FriendsPageId = "friends";
        public const string ChatPageId = "chat";
        public const string ProfilePageId = "profile";
        public const string SettingsPageId = "settings";

        private readonly IAppActions _app;
        private readonly List<AppPage> _pages = new List<AppPage>();
        private readonly List<string> _history = new List<string>();
        private readonly Dictionary<string, SFS.UI.ModGUI.Button> _navButtons = new Dictionary<string, SFS.UI.ModGUI.Button>();

        private Window _window;
        private Transform _root;
        private Transform _content;
        private Label _header;
        private Label _subheader;
        private Label _status;
        private Label _presence;
        private Label _navServer;
        private Label _navAccount;
        private string _currentId = HomePageId;
        private bool _built;

        public AppShell(IAppActions app)
        {
            _app = app;
            _pages.Add(new HomePage());
            _pages.Add(new ServersPage());
            _pages.Add(new RoomsPage());
            _pages.Add(new FriendsPage());
            _pages.Add(new ChatPage());
            _pages.Add(new ProfilePage());
            _pages.Add(new SettingsPage());
        }

        public void Build(Transform parent)
        {
            if (_built) return;

            _window = Builder.CreateWindow(parent, Builder.GetRandomID(), 1000, 640, 0, 0, false, false, 0.99f, "SFS ENHANCED");
            _window.WindowColor = Theme.Surface;
            _window.WindowOpacity = 0.97f;
            _root = _window.ChildrenHolder;

            _header = UiKit.Label(_root, Theme.ContentWidth, 34, 105, 268, string.Empty, Theme.TitleSize, Theme.TextPrimary);
            _subheader = UiKit.Label(_root, Theme.ContentWidth, 20, 105, 238, string.Empty, 13f, Theme.TextSecondary);
            UiKit.Divider(_root, 940, 216);

            UiKit.Label(_root, 180, 18, -405, 286, "SFS ENHANCED", 12f, Theme.TextMuted);
            int navY = 246;
            foreach (AppPage page in _pages)
            {
                string pageId = page.Id;
                SFS.UI.ModGUI.Button button = UiKit.Secondary(_root, 180, 36, -405, navY, page.Title.ToUpperInvariant(), () => Navigate(pageId));
                _navButtons[pageId] = button;
                navY -= 44;
            }

            _navServer = UiKit.Label(_root, 180, 18, -405, -132, string.Empty, 11f, Theme.TextMuted);
            _navAccount = UiKit.Label(_root, 180, 20, -405, -156, string.Empty, 12f, Theme.TextPrimary);

            Box rail = Builder.CreateBox(_root, 2, 496, -300, 14, 0.08f);
            rail.Color = Theme.TextMuted;
            rail.Opacity = 0.22f;

            Container container = Builder.CreateContainer(_root, 105, -8);
            container.Size = new Vector2(Theme.ContentWidth, 440);
            _content = container.rectTransform;

            _status = UiKit.Label(_root, 360, 22, -80, -292, string.Empty, 12f, Theme.TextSecondary);
            _presence = UiKit.Label(_root, 260, 22, 235, -292, string.Empty, 12f, Theme.TextMuted, TextAlignmentOptions.Right);

            _app.State.Changed += RenderChrome;
            _app.State.ContentChanged += UpdateContent;
            _built = true;
            RenderPage();
            RenderChrome();
        }

        public void Navigate(string pageId)
        {
            if (string.IsNullOrEmpty(pageId) || !_navButtons.ContainsKey(pageId)) return;
            if (pageId != _currentId)
            {
                _history.Add(_currentId);
                if (_history.Count > 8) _history.RemoveAt(0);
                _currentId = pageId;
            }
            RenderPage();
            RenderChrome();
        }

        public bool Back()
        {
            if (_history.Count == 0) return false;
            _currentId = _history[_history.Count - 1];
            _history.RemoveAt(_history.Count - 1);
            RenderPage();
            RenderChrome();
            return true;
        }

        public void UpdateContent()
        {
            if (!_built || _content == null) return;
            AppPage page = CurrentPage();
            if (page.NeedsRebuild(_app)) RenderPage();
            else page.Update(_app);
            RenderChrome();
        }

        public void RenderPage()
        {
            if (!_built || _content == null) return;
            AppPage page = CurrentPage();
            ClearContent();
            try
            {
                page.Build(_app, _content);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SFSEnhanced] Page '{page.Id}' failed to render: {e}");
                UiKit.EmptyState(_content, 60, "This page could not be drawn", "Details are in the log.");
            }
        }

        public void RenderChrome()
        {
            if (!_built) return;

            AppPage page = CurrentPage();
            if (_header != null) _header.Text = page.Title;
            if (_subheader != null) _subheader.Text = page.Subtitle;

            foreach (KeyValuePair<string, SFS.UI.ModGUI.Button> entry in _navButtons)
                entry.Value.TextColor = entry.Key == _currentId ? Theme.Accent : Theme.TextSecondary;

            AppState state = _app.State;
            if (_status != null)
            {
                _status.Text = state.Status;
                _status.Color = state.StatusIsError ? Theme.Danger : Theme.TextSecondary;
            }

            if (_presence != null)
            {
                if (state.Connected)
                {
                    _presence.Text = $"{state.ServerPlayers}/{state.ServerMaxPlayers} online";
                    _presence.Color = Theme.Positive;
                }
                else
                {
                    _presence.Text = "OFFLINE";
                    _presence.Color = Theme.TextMuted;
                }
            }

            PlayerProfile profile = _app.Mod.Client.SelfProfile;
            if (_navServer != null) _navServer.Text = state.Connected ? Shorten(state.ServerLabel(), 22).ToUpperInvariant() : "NO SERVER";
            if (_navAccount != null) _navAccount.Text = profile != null ? Shorten(profile.DisplayName, 20) : (state.Connected ? "Syncing account..." : "Not connected");
        }

        public void Dispose()
        {
            if (!_built) return;
            _app.State.Changed -= RenderChrome;
            _app.State.ContentChanged -= UpdateContent;
            if (_window != null) UnityEngine.Object.Destroy(_window.gameObject);
            _window = null;
            _root = null;
            _content = null;
            _built = false;
        }

        private AppPage CurrentPage()
        {
            foreach (AppPage page in _pages)
                if (page.Id == _currentId) return page;
            _currentId = HomePageId;
            return _pages[0];
        }

        private void ClearContent()
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                GameObject child = _content.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }

        private static string Shorten(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= max ? value : value.Substring(0, max - 1) + "\u2026";
        }
    }
}
