using SFSEnhanced.Mod.UI.App;
using SFSEnhanced.Mod.UI.Kit;
using UnityEngine;

namespace SFSEnhanced.Mod.UI.App.Pages
{
    internal sealed class MainMenuPage : AppPage
    {
        public override string Id => AppShell.MainPageId;
        public override string Title => "Home";
        public override string Subtitle => "The SFS start screen, with multiplayer, socials and content tools integrated.";

        public override void Build(IAppActions app, Transform host)
        {
            UiKit.Card(host, 710, 112, 0, 142, Theme.Row, 0.7f);
            UiKit.Label(host, 650, 38, 0, 166, "SPACEFLIGHT SIMULATOR", 26f, Theme.TextPrimary);
            UiKit.Label(host, 650, 24, 0, 126, "SFS Enhanced brings the original game and its multiplayer tools together.", 13f, Theme.TextSecondary);

            UiKit.Primary(host, 210, 48, -235, 64, "PLAY", app.OpenNativePlay);
            UiKit.Primary(host, 210, 48, 0, 64, "MULTIPLAYER", () => app.Navigate(AppShell.MultiplayerPageId));
            UiKit.Primary(host, 210, 48, 235, 64, "MODS & PACKS", () => app.Navigate(AppShell.PackagesPageId));

            UiKit.Section(host, "ORIGINAL SFS MENU", 10);
            UiKit.Secondary(host, 210, 38, -235, -32, "SETTINGS", app.OpenNativeSettings);
            UiKit.Secondary(host, 210, 38, 0, -32, "COMMUNITY", app.OpenNativeCommunity);
            UiKit.Secondary(host, 210, 38, 235, -32, "TUTORIALS", app.OpenNativeTutorials);
            UiKit.Secondary(host, 210, 38, -235, -82, "CREDITS", app.OpenNativeCredits);
            UiKit.Secondary(host, 210, 38, 0, -82, "FULL VERSION", app.OpenNativeFullVersion);
            UiKit.Secondary(host, 210, 38, 235, -82, "SFS WEBSITE", app.OpenNativeWebsite);
            UiKit.Danger(host, 210, 38, -235, -132, "QUIT GAME", app.OpenNativeQuit);
            UiKit.Secondary(host, 210, 38, 0, -132, "ABOUT SFS ENHANCED", () => app.Navigate(AppShell.AboutPageId));
            UiKit.Secondary(host, 210, 38, 235, -132, "SOCIAL", () => app.Navigate(AppShell.FriendsPageId));
            UiKit.Secondary(host, 210, 38, 235, -182, "CONTROLS", () => app.Navigate(AppShell.ControlsPageId));
        }
    }
}
