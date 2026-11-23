using SFSEnhanced.Mod.Packaging;
using SFSEnhanced.Mod.UI.App;
using SFSEnhanced.Mod.UI.Kit;
using UnityEngine;

namespace SFSEnhanced.Mod.UI.App.Pages
{
    internal sealed class AboutPage : AppPage
    {
        public override string Id => AppShell.AboutPageId;
        public override string Title => "About";
        public override string Subtitle => "Project information, compatibility notes and support links.";

        public override void Build(IAppActions app, Transform host)
        {
            UiKit.Card(host, 700, 100, 0, 148, Theme.Row, 0.65f);
            UiKit.Label(host, 650, 34, 0, 175, "SFS ENHANCED", 26f, Theme.TextPrimary);
            UiKit.Label(host, 650, 20, 0, 137, "Version " + PackageManager.CurrentSfsEnhancedVersion, 13f, Theme.TextSecondary);

            UiKit.Section(host, "WHAT IT ADDS", 78);
            UiKit.Body(host, "A unified SFS start screen with the native game actions, multiplayer servers, rooms, friends, chat and profiles.", 42);
            UiKit.Body(host, "The package library supports installable mods, experimental mods, texture overrides and custom solar systems.", 10);
            UiKit.Body(host, "Texture overrides can be applied to loaded planets. Custom solar systems become available in the native world-creation selector.", -22);

            UiKit.Section(host, "COMPATIBILITY", -66);
            UiKit.Small(host, 680, 0, -96, "Code assemblies cannot be unloaded safely by the current SFS Mono mod loader. Mod DLLs therefore require a game restart.", Theme.Warning);
            UiKit.Small(host, 680, 0, -126, "Solar-system files are installed and removed without restarting SFS. Exit a world before removing a system package.", Theme.TextSecondary);

            UiKit.Section(host, "PROJECT", -160);
            UiKit.Secondary(host, 220, 36, -230, -198, "SOURCE CODE", () => Application.OpenURL("https://github.com/PalorderSoftWorksOfficial/sfs-enhanced"));
            UiKit.Secondary(host, 220, 36, 0, -198, "ISSUES & SUPPORT", () => Application.OpenURL("https://github.com/PalorderSoftWorksOfficial/sfs-enhanced/issues"));
            UiKit.Secondary(host, 220, 36, 230, -198, "LICENSE", () => Application.OpenURL("https://github.com/PalorderSoftWorksOfficial/sfs-enhanced/blob/main/LICENSE"));
        }
    }
}
