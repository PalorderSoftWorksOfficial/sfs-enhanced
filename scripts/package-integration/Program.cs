using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SFSEnhanced.Shared.Packaging;

internal static class Program
{
    private static int _failures;

    private static void Main(string[] args)
    {
        string repoRoot = FindRepoRoot();
        string artifacts = Path.Combine(repoRoot, "artifacts");
        string modPack = Path.Combine(artifacts, "SFSEnhanced-Mod-1.0.0.pack");
        string serverPack = Path.Combine(artifacts, "SFSEnhanced-Server-1.0.0.pack");
        if (!File.Exists(modPack) || !File.Exists(serverPack))
        {
            Console.Error.WriteLine("Pack artifacts missing. Build Mod and Server in Release first.");
            Environment.Exit(2);
        }

        string work = Path.Combine(Path.GetTempPath(), "sfs-pkgtest-" + Guid.NewGuid().ToString("N"));
        try
        {
            TestBuiltPacksInstallIntoRealLayout(modPack, serverPack, work);
            TestBackupAndUninstallRestores(work);
            TestTraversalRejection(work);
            TestUnknownTargetRootRejection(work);
            TestDependencyValidation(work);
            TestLegacyManifestlessPackage(work);
        }
        finally
        {
            if (Directory.Exists(work)) Directory.Delete(work, true);
        }

        if (_failures > 0)
        {
            Console.Error.WriteLine($"PACKAGE_INTEGRATION_FAILED ({_failures} failures)");
            Environment.Exit(1);
        }
        Console.WriteLine("PACKAGE_INTEGRATION_PASSED");
    }

    private static void TestBuiltPacksInstallIntoRealLayout(string modPack, string serverPack, string work)
    {
        string gameRoot = CreateGameRoot(work, "game");
        string packages = Path.Combine(gameRoot, "Mods", "SFS Enhanced", "Packages");
        Directory.CreateDirectory(packages);
        File.Copy(modPack, Path.Combine(packages, "SFSEnhanced-Mod-1.0.0.pack"));
        File.Copy(serverPack, Path.Combine(packages, "SFSEnhanced-Server-1.0.0.pack"));

        PackageInstaller.InstallPendingPackages(packages, Path.Combine(gameRoot, "Mods", "SFS Enhanced"), m => { }, "0.1.1");

        Check(File.Exists(Path.Combine(gameRoot, "Mods", "SFSEnhanced", "SFSEnhanced.dll")), "mod pack installs SFSEnhanced.dll");
        Check(File.Exists(Path.Combine(gameRoot, "Mods", "SFSEnhanced", "mod.json")), "mod pack installs mod.json");
        Check(File.Exists(Path.Combine(gameRoot, "Mods", "SFSEnhanced", "Lidgren.Network.dll")), "mod pack installs Lidgren.Network.dll");
        Check(File.Exists(Path.Combine(gameRoot, "Mods", "SFSEnhancedServer", "SFSEnhanced.Server.dll")), "server pack installs server dll");
        Check(File.Exists(Path.Combine(gameRoot, "Mods", "SFSEnhancedServer", "server.example.json")), "server pack installs server.example.json");
        Check(PackageInstaller.IsInstalled("sfs-enhanced", packages), "sfs-enhanced marked installed");
        Check(PackageInstaller.IsInstalled("sfs-enhanced-server", packages), "sfs-enhanced-server marked installed");
        Check(PackageInstaller.GetInstalledVersion("sfs-enhanced", packages) == "1.0.0", "installed version recorded");
        Check(Directory.GetFiles(packages, "*.pack").Length == 0, "packs are consumed on install");
    }

    private static void TestBackupAndUninstallRestores(string work)
    {
        string gameRoot = CreateGameRoot(work, "game2");
        string packages = Path.Combine(gameRoot, "Mods", "SFS Enhanced", "Packages");
        Directory.CreateDirectory(packages);
        string existing = Path.Combine(gameRoot, "Mods", "SFSEnhanced", "SFSEnhanced.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(existing));
        File.WriteAllText(existing, "original");

        string pack = Path.Combine(packages, "uninstall-test.pack");
        BuildPack(pack, "uninstall-test", m =>
        {
            m.Files.Add(new PackageFile { Source = "Mods/SFSEnhanced/SFSEnhanced.dll", Target = "Mods/SFSEnhanced/SFSEnhanced.dll" });
        }, null, ("Mods/SFSEnhanced/SFSEnhanced.dll", "packaged"));

        var installResult = PackageInstaller.InstallArchive(pack, packages, Path.Combine(gameRoot, "Mods", "SFS Enhanced"), m => { }, "0.1.1");
        Check(installResult.Success, "uninstall-test package installs: " + installResult.Error);
        Check(File.ReadAllText(existing) == "packaged", "install overwrites existing file");
        var uninstallResult = PackageInstaller.Uninstall("uninstall-test", packages, Path.Combine(gameRoot, "Mods", "SFS Enhanced"));
        Check(uninstallResult.Success, "uninstall succeeds: " + uninstallResult.Error);
        Check(File.ReadAllText(existing) == "original", "uninstall restores the pre-existing file from backup");
        Check(!Directory.Exists(Path.Combine(packages, "installed", "uninstall-test")), "uninstall removes package record");
    }

    private static void TestTraversalRejection(string work)
    {
        string gameRoot = CreateGameRoot(work, "game3");
        string packages = Path.Combine(gameRoot, "Mods", "SFS Enhanced", "Packages");
        Directory.CreateDirectory(packages);
        string escapePath = Path.Combine(gameRoot, "ESCAPED.txt");

        string pack = Path.Combine(packages, "traversal.pack");
        BuildPack(pack, "traversal", m =>
        {
            m.Files.Add(new PackageFile { Source = "Mods/payload.txt", Target = "Mods/../Evil.txt" });
        }, null, ("Mods/payload.txt", "evil"));

        var result = PackageInstaller.InstallArchive(pack, packages, Path.Combine(gameRoot, "Mods", "SFS Enhanced"), m => { }, "0.1.1");
        Check(!result.Success && result.Error != null, "dot-dot inside target root is rejected: " + result.Error);
        Check(!File.Exists(Path.Combine(gameRoot, "Evil.txt")), "file did not escape into game root");
        Check(!File.Exists(escapePath), "file did not escape anywhere");

        string pack2 = Path.Combine(packages, "traversal2.pack");
        BuildPack(pack2, "traversal2", m =>
        {
            m.Files.Add(new PackageFile { Source = "Saving/payload.txt", Target = "Mods/../../ESCAPED.txt" });
        }, null, ("Saving/payload.txt", "evil2"));

        var result2 = PackageInstaller.InstallArchive(pack2, packages, Path.Combine(gameRoot, "Mods", "SFS Enhanced"), m => { }, "0.1.1");
        Check(!result2.Success, "cross-root escape is rejected");
        Check(!File.Exists(Path.Combine(gameRoot, "ESCAPED.txt")), "file did not escape the game root");
    }

    private static void TestUnknownTargetRootRejection(string work)
    {
        string gameRoot = CreateGameRoot(work, "game4");
        string packages = Path.Combine(gameRoot, "Mods", "SFS Enhanced", "Packages");
        Directory.CreateDirectory(packages);

        string pack = Path.Combine(packages, "badroot.pack");
        BuildPack(pack, "badroot", m =>
        {
            m.Files.Add(new PackageFile { Source = "Anything/x.txt", Target = "Anything/x.txt" });
        }, null, ("Anything/x.txt", "nope"));

        var result = PackageInstaller.InstallArchive(pack, packages, Path.Combine(gameRoot, "Mods", "SFS Enhanced"), m => { }, "0.1.1");
        Check(!result.Success && result.Error.Contains("Unsupported package target root"), "unknown target root is rejected");
        Check(!Directory.Exists(Path.Combine(gameRoot, "Anything")), "nothing was written outside declared roots");
    }

    private static void TestDependencyValidation(string work)
    {
        string gameRoot = CreateGameRoot(work, "game5");
        string packages = Path.Combine(gameRoot, "Mods", "SFS Enhanced", "Packages");
        Directory.CreateDirectory(packages);

        string missing = Path.Combine(packages, "needs-missing.pack");
        BuildPack(missing, "needs-missing", m =>
        {
            m.Dependencies.Add(new PackageDependency { Id = "not-installed", MinimumVersion = "1.0.0" });
            m.Files.Add(new PackageFile { Source = "Mods/a.txt", Target = "Mods/a.txt" });
        }, null, ("Mods/a.txt", "a"));

        var result = PackageInstaller.InstallArchive(missing, packages, Path.Combine(gameRoot, "Mods", "SFS Enhanced"), m => { }, "0.1.1");
        Check(!result.Success && result.Error.Contains("Missing package dependency"), "missing dependency is rejected");

        string basePack = Path.Combine(packages, "base-lib.pack");
        BuildPack(basePack, "base-lib", m =>
        {
            m.Files.Add(new PackageFile { Source = "Mods/base.txt", Target = "Mods/base.txt" });
        }, null, ("Mods/base.txt", "base"));
        var baseResult = PackageInstaller.InstallArchive(basePack, packages, Path.Combine(gameRoot, "Mods", "SFS Enhanced"), m => { }, "0.1.1");
        Check(baseResult.Success, "dependency-free package installs");

        string stale = Path.Combine(packages, "needs-newer.pack");
        BuildPack(stale, "needs-newer", m =>
        {
            m.Dependencies.Add(new PackageDependency { Id = "base-lib", MinimumVersion = "2.0.0" });
            m.Files.Add(new PackageFile { Source = "Mods/b.txt", Target = "Mods/b.txt" });
        }, null, ("Mods/b.txt", "b"));

        var staleResult = PackageInstaller.InstallArchive(stale, packages, Path.Combine(gameRoot, "Mods", "SFS Enhanced"), m => { }, "0.1.1");
        Check(!staleResult.Success && staleResult.Error.Contains("too old"), "stale dependency is rejected");

        string good = Path.Combine(packages, "needs-base.pack");
        BuildPack(good, "needs-base", m =>
        {
            m.Dependencies.Add(new PackageDependency { Id = "base-lib", MinimumVersion = "1.0.0" });
            m.Files.Add(new PackageFile { Source = "Mods/c.txt", Target = "Mods/c.txt" });
        }, null, ("Mods/c.txt", "c"));

        var goodResult = PackageInstaller.InstallArchive(good, packages, Path.Combine(gameRoot, "Mods", "SFS Enhanced"), m => { }, "0.1.1");
        Check(goodResult.Success, "satisfied dependency installs");
    }

    private static void TestLegacyManifestlessPackage(string work)
    {
        string gameRoot = CreateGameRoot(work, "game6");
        string packages = Path.Combine(gameRoot, "Mods", "SFS Enhanced", "Packages");
        Directory.CreateDirectory(packages);

        string legacy = Path.Combine(packages, "legacy.zip");
        string payloadRoot = Path.Combine(work, "legacy-payload", "Mods", "SomeMod");
        Directory.CreateDirectory(payloadRoot);
        File.WriteAllText(Path.Combine(payloadRoot, "SomeMod.dll"), "legacy");
        using (var zip = ZipFile.Open(legacy, ZipArchiveMode.Create))
            zip.CreateEntryFromFile(Path.Combine(work, "legacy-payload", "Mods", "SomeMod", "SomeMod.dll"), "Mods/SomeMod/SomeMod.dll");

        var result = PackageInstaller.InstallArchive(legacy, packages, Path.Combine(gameRoot, "Mods", "SFS Enhanced"), m => { }, "0.1.1");
        Check(result.Success && result.PackageId == "legacy", "manifest-less zip installs with inferred id");
        Check(File.Exists(Path.Combine(gameRoot, "Mods", "SomeMod", "SomeMod.dll")), "legacy layout maps to game root");
    }

    private static void BuildPack(string path, string id, Action<PackageManifest> configure, string unused, params (string entryName, string content)[] extraEntries)
    {
        var manifest = new PackageManifest
        {
            Id = id,
            Name = id,
            Version = "1.0.0",
            Author = "test",
            Description = "integration test package"
        };
        configure?.Invoke(manifest);
        if (File.Exists(path)) File.Delete(path);
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            zip.CreateEntryFromText("package.json", JsonConvert.SerializeObject(manifest, Formatting.Indented));
            foreach (var (entryName, content) in extraEntries)
                zip.CreateEntryFromText(entryName, content);
        }
    }

    private static string CreateGameRoot(string work, string name)
    {
        string gameRoot = Path.Combine(work, name);
        Directory.CreateDirectory(Path.Combine(gameRoot, "Mods", "SFS Enhanced"));
        return gameRoot;
    }

    private static void Check(bool condition, string label)
    {
        if (condition)
        {
            Console.WriteLine("  ok: " + label);
            return;
        }
        _failures++;
        Console.Error.WriteLine("  FAIL: " + label);
    }

    private static string FindRepoRoot()
    {
        string dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "SFSEnhanced.sln"))) return dir;
            dir = Path.GetDirectoryName(dir);
            if (dir == null) break;
        }
        throw new InvalidOperationException("Repository root not found.");
    }
}

internal static class ZipExtensions
{
    public static void CreateEntryFromText(this ZipArchive zip, string entryName, string content)
    {
        var entry = zip.CreateEntry(entryName);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }
}
