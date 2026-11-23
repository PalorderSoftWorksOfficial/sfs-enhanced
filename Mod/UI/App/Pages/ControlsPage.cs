using SFSEnhanced.Mod.Controls;
using SFSEnhanced.Mod.UI.Kit;
using UnityEngine;

namespace SFSEnhanced.Mod.UI.App.Pages
{
    internal sealed class ControlsPage : AppPage
    {
        private Transform _host;

        public override string Id => AppShell.ControlsPageId;
        public override string Title => "Controls";
        public override string Subtitle => "Configure SFS Enhanced shortcuts and control behavior.";

        public override void Build(IAppActions app, Transform host)
        {
            _host = host;
            Render(app);
        }

        public override void Update(IAppActions app)
        {
            Render(app);
        }

        private void Render(IAppActions app)
        {
            if (_host == null) return;
            UiKit.Clear(_host);
            int y = Theme.ContentTop;

            UiKit.Section(_host, "SHORTCUTS", y);
            y -= 40;
            DrawBinding(_host, y, "Open / close SFS Enhanced", "Available in menus and during flight.", ShortcutAction.ToggleMenu);
            y -= 66;
            DrawBinding(_host, y, "Toggle torque", "Flight-only. Ctrl + T by default.", ShortcutAction.ToggleTorque);
            y -= 66;

            UiKit.Small(_host, Theme.ContentWidth, 0, y, ModControlBindings.Status, Theme.TextSecondary);
            y -= 36;
            UiKit.Secondary(_host, 190, 36, -265, y, "RESET DEFAULTS", ModControlBindings.ResetDefaults);
            y -= 38;
            UiKit.Small(_host, Theme.ContentWidth, 0, y, "Keyboard and joystick buttons. Ctrl, Shift and Alt modifiers. Escape cancels capture.", Theme.TextMuted);
            y -= 48;

            UiKit.Section(_host, "DISABLE AND RESTORE", y);
            y -= 26;
            UiKit.Label(_host, Theme.ContentWidth, 70, 0, y, "Disabling immediately stops networking and updates, removes SFS Enhanced UI and remote rockets, restores synchronized world time and texture/material overrides, and unpatches every Harmony patch owned by SFS Enhanced. The game loader uses Assembly.Load and cannot unload a managed DLL in-process. Restart SFS to unload the assembly or enable it again.", Theme.SmallSize, Theme.Warning);
            y -= 84;
            UiKit.Danger(_host, 270, 38, -235, y, "DISABLE UNTIL RESTART", () => app.Mod?.Disable());
        }

        private static void DrawBinding(Transform host, int y, string title, string description, ShortcutAction action)
        {
            UiKit.Card(host, Theme.ContentWidth, 58, 0, y, Theme.Row, 0.5f);
            UiKit.Label(host, 430, 20, -145, y + 9, title, Theme.BodySize, Theme.TextPrimary);
            UiKit.Label(host, 430, 18, -145, y - 12, description, Theme.SmallSize, Theme.TextMuted);
            bool capturing = ModControlBindings.CapturingAction.HasValue && ModControlBindings.CapturingAction.Value == action;
            UiKit.Secondary(host, 194, 34, 258, y, capturing ? "PRESS A KEY..." : ModControlBindings.Display(action), () => ModControlBindings.BeginCapture(action));
        }
    }
}
