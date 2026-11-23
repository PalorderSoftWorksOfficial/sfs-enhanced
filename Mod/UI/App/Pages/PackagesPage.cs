using System.Collections.Generic;
using SFSEnhanced.Mod.Packaging;
using SFSEnhanced.Mod.UI.App;
using SFSEnhanced.Mod.UI.Kit;
using SFS.UI.ModGUI;
using UnityEngine;

namespace SFSEnhanced.Mod.UI.App.Pages
{
    internal sealed class PackagesPage : AppPage
    {
        private int _pendingPage;
        private int _installedPage;
        private Label _pendingPageLabel;
        private Label _installedPageLabel;
        private Transform _pendingRegion;
        private Transform _installedRegion;

        public override string Id => AppShell.PackagesPageId;
        public override string Title => "Mods & Packs";
        public override string Subtitle => "Install .pack files, manage content and review what requires a restart.";

        public override bool NeedsRebuild(IAppActions app) => false;

        public override void Build(IAppActions app, Transform host)
        {
            UiKit.Small(host, 470, -125, 188, "Package folder: " + PackageManager.PackagesDirectory, Theme.TextMuted);
            UiKit.Secondary(host, 112, 30, 190, 186, "OPEN FOLDER", () =>
            {
                PackageManager.OpenPackagesFolder();
                app.SetStatus(PackageManager.LastOperation);
            });
            UiKit.Secondary(host, 112, 30, 300, 186, "REFRESH", () =>
            {
                PackageManager.Refresh();
                app.SetStatus(PackageManager.LastOperation);
            });

            UiKit.Section(host, "INCOMING PACKAGES", 140);
            UiKit.Secondary(host, 62, 27, 230, 140, "PREV", () =>
            {
                _pendingPage = System.Math.Max(0, _pendingPage - 1);
                RenderPending(app);
            });
            _pendingPageLabel = UiKit.Small(host, 58, 288, 140, "PAGE 1/1", Theme.TextMuted);
            UiKit.Secondary(host, 62, 27, 346, 140, "NEXT", () =>
            {
                _pendingPage++;
                RenderPending(app);
            });
            _pendingRegion = UiKit.Region(host, 0, 74, 122);

            UiKit.Section(host, "INSTALLED", -64);
            UiKit.Secondary(host, 62, 27, 230, -64, "PREV", () =>
            {
                _installedPage = System.Math.Max(0, _installedPage - 1);
                RenderInstalled(app);
            });
            _installedPageLabel = UiKit.Small(host, 58, 288, -64, "PAGE 1/1", Theme.TextMuted);
            UiKit.Secondary(host, 62, 27, 346, -64, "NEXT", () =>
            {
                _installedPage++;
                RenderInstalled(app);
            });
            _installedRegion = UiKit.Region(host, 0, -155, 126);

            RenderPending(app);
            RenderInstalled(app);
        }

        public override void Update(IAppActions app)
        {
            RenderPending(app);
            RenderInstalled(app);
        }

        private void RenderPending(IAppActions app)
        {
            UiKit.Clear(_pendingRegion);
            IReadOnlyList<string> pending = PackageManager.GetPendingArchives();
            int pageCount = (pending.Count + 2) / 3;
            _pendingPage = System.Math.Max(0, System.Math.Min(_pendingPage, System.Math.Max(0, pageCount - 1)));
            _pendingPageLabel.Text = pageCount == 0 ? "PAGE 0/0" : "PAGE " + (_pendingPage + 1) + "/" + pageCount;

            if (pending.Count == 0)
            {
                UiKit.EmptyState(_pendingRegion, 14, "No packages waiting", "Copy a .pack, .sfspkg or .zip package into the folder shown above.");
                return;
            }

            int start = _pendingPage * 3;
            int end = System.Math.Min(start + 3, pending.Count);
            for (int index = start; index < end; index++)
            {
                string fileName = pending[index];
                int y = 44 - (index - start) * 43;
                UiKit.Card(_pendingRegion, Theme.ContentWidth, Theme.RowHeight - 6, 0, y, Theme.Row, 0.5f);
                UiKit.Label(_pendingRegion, 490, 20, -155, y, fileName, Theme.BodySize - 2f, Theme.TextPrimary);
                UiKit.Primary(_pendingRegion, 104, 28, 292, y, "INSTALL", () =>
                {
                    var result = PackageManager.InstallArchive(fileName);
                    app.SetStatus(result.Success ? PackageManager.LastOperation : (result.Error ?? "Package install failed."), !result.Success);
                });
            }
        }

        private void RenderInstalled(IAppActions app)
        {
            UiKit.Clear(_installedRegion);
            IReadOnlyList<PackageEntry> installed = PackageManager.GetInstalledPackages();
            int pageCount = (installed.Count + 2) / 3;
            _installedPage = System.Math.Max(0, System.Math.Min(_installedPage, System.Math.Max(0, pageCount - 1)));
            _installedPageLabel.Text = pageCount == 0 ? "PAGE 0/0" : "PAGE " + (_installedPage + 1) + "/" + pageCount;

            if (installed.Count == 0)
            {
                UiKit.EmptyState(_installedRegion, 12, "Nothing installed yet", "Installed mods, texture packs and solar systems appear here.");
                return;
            }

            int start = _installedPage * 3;
            int end = System.Math.Min(start + 3, installed.Count);
            for (int index = start; index < end; index++)
            {
                PackageEntry entry = installed[index];
                int y = 42 - (index - start) * 43;
                UiKit.Card(_installedRegion, Theme.ContentWidth, Theme.RowHeight - 6, 0, y, Theme.Row, 0.5f);
                string type = entry.Type == "experimental-mod" ? "EXPERIMENTAL MOD" : entry.Type.ToUpperInvariant();
                string details = entry.Name + "  ·  " + type + "  ·  v" + entry.Version;
                UiKit.Label(_installedRegion, 470, 20, -120, y, details, 12f, Theme.TextPrimary);
                if (entry.Type == "texture")
                {
                    string action = entry.Enabled ? "DISABLE" : "ENABLE";
                    UiKit.Secondary(_installedRegion, 88, 28, 230, y, action, () =>
                    {
                        PackageManager.SetEnabled(entry.Id, !entry.Enabled);
                        app.SetStatus(PackageManager.LastOperation);
                    });
                    UiKit.Danger(_installedRegion, 82, 28, 322, y, "REMOVE", () =>
                    {
                        var result = PackageManager.Uninstall(entry.Id);
                        app.SetStatus(result.Success ? PackageManager.LastOperation : (result.Error ?? "Package removal failed."), !result.Success);
                    });
                }
                else
                {
                    if (entry.RequiresRestart) UiKit.Small(_installedRegion, 130, 205, y, "RESTART REQUIRED", Theme.Warning);
                    UiKit.Danger(_installedRegion, 82, 28, 322, y, "REMOVE", () =>
                    {
                        var result = PackageManager.Uninstall(entry.Id);
                        app.SetStatus(result.Success ? PackageManager.LastOperation : (result.Error ?? "Package removal failed."), !result.Success);
                    });
                }
            }
        }
    }
}
