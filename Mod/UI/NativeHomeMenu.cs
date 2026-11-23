using System;
using SFS.Input;
using SFS.Translations;
using SFS.UI;

namespace SFSEnhanced.Mod.UI
{
    internal static class NativeHomeMenu
    {
        public static SFS.UI.Button FindPlayButton()
        {
            HomeManager home = HomeManager.main;
            if (home == null || home.menuHolder == null) return null;
            string localizedPlay = "Play";
            if (Loc.main != null) localizedPlay = Loc.main.Play;

            SFS.UI.Button[] buttons = home.menuHolder.GetComponentsInChildren<SFS.UI.Button>(true);
            foreach (SFS.UI.Button button in buttons)
            {
                TextAdapter text = button.GetComponentInChildren<TextAdapter>(true);
                string label = text == null ? string.Empty : (text.Text ?? string.Empty).Trim();
                if (string.Equals(label, localizedPlay, StringComparison.OrdinalIgnoreCase) || string.Equals(label, "Play", StringComparison.OrdinalIgnoreCase))
                    return button;
            }

            foreach (SFS.UI.Button button in buttons)
            {
                if (button.name.IndexOf("Play", StringComparison.OrdinalIgnoreCase) >= 0)
                    return button;
            }
            return null;
        }

        public static bool InvokePlayButton(SFS.UI.Button button)
        {
            if (button == null) return false;
            button.onClick.Invoke(default(OnInputEndData));
            return true;
        }
    }
}
