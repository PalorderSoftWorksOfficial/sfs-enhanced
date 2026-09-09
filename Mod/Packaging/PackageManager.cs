using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace SFSEnhanced.Mod.Packaging
{
    public static class PackageManager
    {
        private const string PackageDirectoryName = "Packages";
        private const string InstalledDirectoryName = "installed";
        private const string ManifestName = "package.json";
        private const string BackupDirectoryName = ".sfs-enhanced-backups";
        private static string _gameRoot;
        private static string _modFolder;

        public static string PackagesDirectory { get; private set; }
        public static string InstalledDirectory { get; private set; }

        public static void Initialize(string modFolder)
        {
            _modFolder = Path.GetFullPath(modFolder);
            _gameRoot = Directory.GetParent(_modFolder).FullName;
            PackagesDirectory = Path.Combine(_modFolder, PackageDirectoryName);
            InstalledDirectory = Path.Combine(PackagesDirectory, InstalledDirectoryName);
            Directory.CreateDirectory(PackagesDirectory);
            Directory.CreateDirectory(InstalledDirectory);
            ProcessPendingPackages();
        }

        public static void ProcessPendingPackages()
        {
            if (!Directory.Exists(PackagesDirectory)) return;
            foreach (var archive in Directory.GetFiles(PackagesDirectory, "*.sfspkg", SearchOption.TopDirectoryOnly)) InstallArchive(archive);
            foreach (var archive in Directory.GetFiles(PackagesDirectory, "*.zip", SearchOption.TopDirectoryOnly)) InstallArchive(archive);
        }

        public static void Uninstall(string id)
        {
            if (!IsSafeId(id)) return;
            string packageRoot = Path.Combine(InstalledDirectory, id);
            string manifestPath = Path.Combine(packageRoot, ManifestName);
            if (!File.Exists(manifestPath)) return;
            var manifest = JsonConvert.DeserializeObject<PackageManifest>(File.ReadAllText(manifestPath));
            string backupRoot = Path.Combine(packageRoot, BackupDirectoryName);
            foreach (var file in manifest.Files ?? new List<PackageFile>())
            {
                string target = ResolveTarget(file.Target);
                string backup = GetSafePath(backupRoot, file.Target);
                if (File.Exists(backup)) File.Copy(backup, target, true);
                else if (File.Exists(target)) File.Delete(target);
            }
            Directory.Delete(packageRoot, true);
        }

        private static void InstallArchive(string archive)
        {
            string staging = Path.Combine(PackagesDirectory, ".staging-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(staging);
                ZipFile.ExtractToDirectory(archive, staging);
                var manifest = LoadManifest(staging, archive);
                Validate(manifest);
                string packageRoot = Path.Combine(InstalledDirectory, manifest.Id);
                if (Directory.Exists(packageRoot)) Uninstall(manifest.Id);
                string backupRoot = Path.Combine(packageRoot, BackupDirectoryName);
                Directory.CreateDirectory(packageRoot);
                foreach (var file in manifest.Files ?? new List<PackageFile>()) InstallFile(staging, backupRoot, file);
                File.WriteAllText(Path.Combine(packageRoot, ManifestName), JsonConvert.SerializeObject(manifest, Formatting.Indented));
                File.Copy(archive, Path.Combine(packageRoot, Path.GetFileName(archive)), true);
                File.Delete(archive);
                Debug.Log($"[SFSEnhanced] Installed package {manifest.Id} {manifest.Version}.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SFSEnhanced] Package install failed for {Path.GetFileName(archive)}: {ex.Message}");
            }
            finally
            {
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
            }
        }

        private static PackageManifest LoadManifest(string staging, string archive)
        {
            string manifestPath = Path.Combine(staging, ManifestName);
            if (File.Exists(manifestPath)) return JsonConvert.DeserializeObject<PackageManifest>(File.ReadAllText(manifestPath));
            string id = SanitizeId(Path.GetFileNameWithoutExtension(archive));
            var manifest = new PackageManifest { Id = id, Name = id, Version = "0.0.0", Author = "Unknown", Description = "Legacy package", Files = new List<PackageFile>() };
            foreach (string root in new[] { "Mods", "Saving", "Resources", "StreamingAssets", "UserData" })
            {
                string path = Path.Combine(staging, root);
                if (!Directory.Exists(path)) continue;
                foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    string relative = file.Substring(staging.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
                    manifest.Files.Add(new PackageFile { Source = relative, Target = relative });
                }
            }
            if (manifest.Files.Count == 0) throw new InvalidDataException("Missing package.json and no supported package folders were found.");
            return manifest;
        }

        private static void Validate(PackageManifest manifest)
        {
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.Id)) throw new InvalidDataException("Invalid package manifest.");
            if (!IsSafeId(manifest.Id)) throw new InvalidDataException("Invalid package id.");
            if (string.IsNullOrWhiteSpace(manifest.Version) || !Version.TryParse(manifest.Version, out _)) throw new InvalidDataException("Invalid package version.");
            if (!string.IsNullOrWhiteSpace(manifest.MinimumSfsEnhancedVersion) && !Version.TryParse(manifest.MinimumSfsEnhancedVersion, out _)) throw new InvalidDataException("Invalid MinimumSfsEnhancedVersion.");
            foreach (var dependency in manifest.Dependencies ?? new List<PackageDependency>())
                if (!IsInstalled(dependency.Id)) throw new InvalidDataException($"Missing package dependency '{dependency.Id}'.");
        }

        private static bool IsInstalled(string id) => IsSafeId(id) && File.Exists(Path.Combine(InstalledDirectory, id, ManifestName));

        private static void InstallFile(string staging, string backupRoot, PackageFile file)
        {
            if (file == null || string.IsNullOrWhiteSpace(file.Source) || string.IsNullOrWhiteSpace(file.Target)) throw new InvalidDataException("Package contains an invalid file entry.");
            string source = GetSafePath(staging, file.Source);
            string target = ResolveTarget(file.Target);
            if (!File.Exists(source)) throw new FileNotFoundException("Package source file not found.", file.Source);
            string backup = GetSafePath(backupRoot, file.Target);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (File.Exists(target))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                File.Copy(target, backup, true);
            }
            File.Copy(source, target, true);
        }

        private static string ResolveTarget(string target)
        {
            target = target.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
            string first = target.Split(Path.DirectorySeparatorChar)[0];
            if (!new[] { "Mods", "Saving", "Resources", "StreamingAssets", "UserData" }.Any(x => string.Equals(x, first, StringComparison.OrdinalIgnoreCase))) throw new InvalidDataException($"Unsupported package target root '{first}'.");
            return GetSafePath(_gameRoot, target);
        }

        private static string GetSafePath(string root, string relative)
        {
            relative = relative.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
            string full = Path.GetFullPath(Path.Combine(root, relative));
            string rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Package path escapes its root.");
            return full;
        }

        private static string SanitizeId(string value) => new string(value.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.').ToArray());
        private static bool IsSafeId(string id) => !string.IsNullOrWhiteSpace(id) && id.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.');
    }
}
