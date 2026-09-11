using System.IO;
using UnityEngine;
using SFSEnhanced.Shared.Packaging;

namespace SFSEnhanced.Mod.Packaging
{
    public static class PackageManager
    {
        private const string PackageDirectoryName = "Packages";
        public const string CurrentSfsEnhancedVersion = "0.1.1";

        public static string PackagesDirectory { get; private set; }
        private static string _modFolder;

        public static void Initialize(string modFolder)
        {
            _modFolder = Path.GetFullPath(modFolder);
            PackagesDirectory = Path.Combine(_modFolder, PackageDirectoryName);
            Directory.CreateDirectory(PackagesDirectory);
            ProcessPendingPackages();
        }

        public static void ProcessPendingPackages()
        {
            PackageInstaller.InstallPendingPackages(PackagesDirectory, _modFolder, Debug.Log, CurrentSfsEnhancedVersion);
        }

        public static void Uninstall(string id)
        {
            PackageInstaller.Uninstall(id, PackagesDirectory, _modFolder);
        }
    }
}
