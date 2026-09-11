using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;

namespace SFSEnhanced.Shared.Packaging
{
    public delegate void PackageLog(string message);

    public sealed class PackageInstallResult
    {
        public bool Success;
        public string PackageId;
        public string Error;
        public readonly List<string> InstalledFiles = new List<string>();
    }

    public sealed class PackageUninstallResult
    {
        public bool Success;
        public string Error;
        public int RestoredBackups;
        public int DeletedFiles;
    }

    public static class PackageInstaller
    {
        public static readonly string[] TargetRoots = { "Mods", "Saving", "Resources", "StreamingAssets", "UserData" };
        public static readonly string[] PackageExtensions = { ".sfspkg", ".zip", ".pack" };
        public const string ManifestName = "package.json";
        public const string InstalledDirectoryName = "installed";
        public const string BackupDirectoryName = ".sfs-enhanced-backups";

        public static string ResolveGameRoot(string modFolder)
        {
            string full = Path.GetFullPath(modFolder);
            var modsRoot = Directory.GetParent(full);
            var gameRoot = modsRoot != null ? modsRoot.Parent : null;
            if (gameRoot == null) throw new InvalidOperationException("Cannot resolve the game root from the mod folder.");
            return gameRoot.FullName;
        }

        public static void InstallPendingPackages(string packagesDirectory, string modFolder, PackageLog log, string currentSfsEnhancedVersion = null)
        {
            if (!Directory.Exists(packagesDirectory)) return;
            foreach (string extension in PackageExtensions)
                foreach (string archive in Directory.GetFiles(packagesDirectory, "*" + extension, SearchOption.TopDirectoryOnly))
                    InstallArchive(archive, packagesDirectory, modFolder, log, currentSfsEnhancedVersion);
        }

        public static PackageInstallResult InstallArchive(string archivePath, string packagesDirectory, string modFolder, PackageLog log, string currentSfsEnhancedVersion = null)
        {
            var result = new PackageInstallResult { PackageId = null };
            string staging = Path.Combine(packagesDirectory, ".staging-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(staging);
                ZipFile.ExtractToDirectory(archivePath, staging);
                var manifest = LoadManifest(staging, archivePath);
                Validate(manifest, packagesDirectory, currentSfsEnhancedVersion);
                string gameRoot = ResolveGameRoot(modFolder);
                string packageRoot = Path.Combine(packagesDirectory, InstalledDirectoryName, manifest.Id);
                if (Directory.Exists(packageRoot)) Uninstall(manifest.Id, packagesDirectory, modFolder);
                string backupRoot = Path.Combine(packageRoot, BackupDirectoryName);
                Directory.CreateDirectory(packageRoot);
                foreach (var file in manifest.Files ?? new List<PackageFile>())
                {
                    string target = InstallFile(staging, gameRoot, backupRoot, file);
                    result.InstalledFiles.Add(target);
                }
                File.WriteAllText(Path.Combine(packageRoot, ManifestName), JsonConvert.SerializeObject(manifest, Formatting.Indented));
                File.Copy(archivePath, Path.Combine(packageRoot, Path.GetFileName(archivePath)), true);
                File.Delete(archivePath);
                result.Success = true;
                result.PackageId = manifest.Id;
                log?.Invoke($"Installed package {manifest.Id} {manifest.Version}.");
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                log?.Invoke($"Package install failed for {Path.GetFileName(archivePath)}: {ex.Message}");
            }
            finally
            {
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
            }
            return result;
        }

        public static PackageUninstallResult Uninstall(string id, string packagesDirectory, string modFolder)
        {
            var result = new PackageUninstallResult();
            try
            {
                if (!IsSafeId(id)) throw new InvalidDataException("Invalid package id.");
                string packageRoot = Path.Combine(packagesDirectory, InstalledDirectoryName, id);
                string manifestPath = Path.Combine(packageRoot, ManifestName);
                if (!File.Exists(manifestPath)) throw new FileNotFoundException("Package is not installed.", id);
                var manifest = JsonConvert.DeserializeObject<PackageManifest>(File.ReadAllText(manifestPath));
                string gameRoot = ResolveGameRoot(modFolder);
                string backupRoot = Path.Combine(packageRoot, BackupDirectoryName);
                foreach (var file in manifest.Files ?? new List<PackageFile>())
                {
                    string target = ResolveTarget(file.Target, gameRoot);
                    string backup = GetSafePath(backupRoot, file.Target);
                    if (File.Exists(backup))
                    {
                        File.Copy(backup, target, true);
                        result.RestoredBackups++;
                    }
                    else if (File.Exists(target))
                    {
                        File.Delete(target);
                        result.DeletedFiles++;
                    }
                }
                Directory.Delete(packageRoot, true);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }
            return result;
        }

        public static bool IsInstalled(string id, string packagesDirectory)
        {
            return IsSafeId(id) && File.Exists(Path.Combine(packagesDirectory, InstalledDirectoryName, id, ManifestName));
        }

        public static string GetInstalledVersion(string id, string packagesDirectory)
        {
            string manifestPath = Path.Combine(packagesDirectory, InstalledDirectoryName, id, ManifestName);
            if (!File.Exists(manifestPath)) return null;
            var manifest = JsonConvert.DeserializeObject<PackageManifest>(File.ReadAllText(manifestPath));
            return manifest?.Version;
        }

        private static PackageManifest LoadManifest(string staging, string archivePath)
        {
            string manifestPath = Path.Combine(staging, ManifestName);
            if (!File.Exists(manifestPath))
            {
                var wrapper = Directory.GetDirectories(staging, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(d => File.Exists(Path.Combine(d, ManifestName)));
                if (wrapper != null) return LoadManifestFromWrapper(wrapper);
            }
            if (File.Exists(manifestPath)) return JsonConvert.DeserializeObject<PackageManifest>(File.ReadAllText(manifestPath));
            string id = SanitizeId(Path.GetFileNameWithoutExtension(archivePath));
            var legacy = new PackageManifest { Id = id, Name = id, Version = "0.0.0", Author = "Unknown", Description = "Legacy package", Files = new List<PackageFile>() };
            foreach (string root in TargetRoots)
            {
                string path = Path.Combine(staging, root);
                if (!Directory.Exists(path)) continue;
                foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    string relative = file.Substring(staging.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
                    legacy.Files.Add(new PackageFile { Source = relative, Target = relative });
                }
            }
            if (legacy.Files.Count == 0) throw new InvalidDataException("Missing package.json and no supported package folders were found.");
            return legacy;
        }

        private static PackageManifest LoadManifestFromWrapper(string wrapper)
        {
            var manifest = JsonConvert.DeserializeObject<PackageManifest>(File.ReadAllText(Path.Combine(wrapper, ManifestName)));
            if (manifest == null) throw new InvalidDataException("Invalid package manifest.");
            foreach (var file in manifest.Files ?? new List<PackageFile>())
                file.Source = Path.Combine(Path.GetFileName(wrapper), file.Source).Replace(Path.DirectorySeparatorChar, '/');
            return manifest;
        }

        private static void Validate(PackageManifest manifest, string packagesDirectory, string currentSfsEnhancedVersion)
        {
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.Id)) throw new InvalidDataException("Invalid package manifest.");
            if (!IsSafeId(manifest.Id)) throw new InvalidDataException("Invalid package id.");
            if (string.IsNullOrWhiteSpace(manifest.Version) || !Version.TryParse(manifest.Version, out _)) throw new InvalidDataException("Invalid package version.");
            if (!string.IsNullOrWhiteSpace(manifest.MinimumSfsEnhancedVersion) && !string.IsNullOrEmpty(currentSfsEnhancedVersion))
            {
                if (!Version.TryParse(manifest.MinimumSfsEnhancedVersion, out var minimum) || minimum > Version.Parse(currentSfsEnhancedVersion))
                    throw new InvalidDataException("This package requires a newer SFS Enhanced version.");
            }
            foreach (var dependency in manifest.Dependencies ?? new List<PackageDependency>())
            {
                if (dependency == null || !IsSafeId(dependency.Id)) throw new InvalidDataException($"Invalid package dependency '{dependency?.Id}'.");
                if (!IsInstalled(dependency.Id, packagesDirectory)) throw new InvalidDataException($"Missing package dependency '{dependency.Id}'.");
                if (!string.IsNullOrWhiteSpace(dependency.MinimumVersion))
                {
                    string installedVersion = GetInstalledVersion(dependency.Id, packagesDirectory);
                    if (!Version.TryParse(dependency.MinimumVersion, out var dependencyMinimum) || !Version.TryParse(installedVersion, out var installed) || installed < dependencyMinimum)
                        throw new InvalidDataException($"Package dependency '{dependency.Id}' is too old.");
                }
            }
        }

        private static string InstallFile(string staging, string gameRoot, string backupRoot, PackageFile file)
        {
            if (file == null || string.IsNullOrWhiteSpace(file.Source) || string.IsNullOrWhiteSpace(file.Target)) throw new InvalidDataException("Package contains an invalid file entry.");
            string source = GetSafePath(staging, file.Source);
            string target = ResolveTarget(file.Target, gameRoot);
            if (!File.Exists(source)) throw new FileNotFoundException("Package source file not found.", file.Source);
            string backup = GetSafePath(backupRoot, file.Target);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (File.Exists(target))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                File.Copy(target, backup, true);
            }
            File.Copy(source, target, true);
            return target;
        }

        public static string ResolveTarget(string target, string gameRoot)
        {
            if (string.IsNullOrWhiteSpace(target)) throw new InvalidDataException("Package contains an invalid file entry.");
            target = target.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
            string first = target.Split(Path.DirectorySeparatorChar)[0];
            if (!TargetRoots.Contains(first, StringComparer.OrdinalIgnoreCase)) throw new InvalidDataException($"Unsupported package target root '{first}'.");
            string resolved = GetSafePath(gameRoot, target);
            string declaredRoot = Path.Combine(Path.GetFullPath(gameRoot), first) + Path.DirectorySeparatorChar;
            if (!resolved.StartsWith(declaredRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Package target escapes its declared root.");
            return resolved;
        }

        public static string GetSafePath(string root, string relative)
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
