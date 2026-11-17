using SFSEnhanced.Mod.UI.App;
using SFSEnhanced.Mod.UI.Kit;
using SFSEnhanced.Shared.Models;
using SFS.UI.ModGUI;
using UnityEngine;

namespace SFSEnhanced.Mod.UI.App.Pages
{
    internal sealed class ChatPage : AppPage
    {
        private const int VisibleLines = 12;
        private const int LineStep = 26;
        private const int TopLineY = 130;

        private string _message = string.Empty;
        private Transform _logRegion;
        private int _rendered;
        private int _renderedStart;
        private long _renderedTailSequence;
        private int _mode = -1;

        public override string Id => AppShell.ChatPageId;
        public override string Title => "Chat";
        public override string Subtitle => "World chat while you are in a world, otherwise server lobby chat.";

        public override bool NeedsRebuild(IAppActions app) => (!app.State.Connected) != _unconnectedBuilt;

        private bool _unconnectedBuilt;

        public override void Build(IAppActions app, Transform host)
        {
            _unconnectedBuilt = !app.State.Connected;

            UiKit.Secondary(host, 100, 26, 320, 200, "CLEAR", app.ClearChat);
            UiKit.Small(host, 560, -80, 200, "Showing the latest 12 messages.", Theme.TextMuted);
            _logRegion = UiKit.Region(host, 0, 40, 300);

            Builder.CreateInputWithLabel(host, 500, 40, -100, -140, "Message", _message, v => _message = v);
            UiKit.Primary(host, 130, 38, 280, -140, "SEND", () =>
            {
                app.SendChat(_message);
                _message = string.Empty;
            });
            UiKit.Small(host, Theme.ContentWidth, 0, -174, "Messages are relayed by the server, so everyone in the world sees the same log.", Theme.TextMuted);

            _mode = -1;
            _rendered = 0;
            _renderedStart = 0;
            _renderedTailSequence = 0;
            Update(app);
        }

        public override void Update(IAppActions app)
        {
            AppState state = app.State;
            if (_logRegion == null) return;

            int mode = !state.Connected ? 0 : (state.Chat.Count == 0 ? 1 : 2);
            if (mode != _mode)
            {
                _mode = mode;
                _rendered = 0;
                _renderedStart = 0;
                _renderedTailSequence = 0;
                UiKit.Clear(_logRegion);

                if (mode == 0)
                {
                    UiKit.EmptyState(_logRegion, 40, "Not connected", "Chat is delivered by the server. Connect to send and receive messages.");
                    UiKit.Primary(_logRegion, 220, 38, 0, -60, "GO TO SERVERS", () => app.Navigate(AppShell.ServersPageId));
                    return;
                }
                if (mode == 1)
                {
                    UiKit.Small(_logRegion, Theme.ContentWidth, 0, 60, "No messages yet.", Theme.TextMuted);
                    return;
                }
            }

            if (mode != 2) return;

            int count = state.Chat.Count;
            int start = ChatLogWindow.Start(count, VisibleLines);
            long tailSequence = count > 0 ? state.Chat[count - 1].Sequence : 0;

            if (ChatLogWindow.NeedsRedraw(_renderedStart, start, _rendered, count, _renderedTailSequence, tailSequence))
            {
                UiKit.Clear(_logRegion);
                _renderedStart = start;
                _rendered = start;
            }

            for (int i = _rendered; i < count; i++)
            {
                ChatLine line = state.Chat[i];
                int slot = ChatLogWindow.Slot(i, start);
                int y = TopLineY - slot * LineStep;
                string scope = line.Scope == "room" ? string.Empty : "[lobby] ";
                UiKit.Label(_logRegion, Theme.ContentWidth, 22, 0, y, $"{scope}{line.From}: {line.Text}", 13f, Theme.TextPrimary);
            }
            _rendered = count;
            _renderedTailSequence = tailSequence;
        }
    }
}
