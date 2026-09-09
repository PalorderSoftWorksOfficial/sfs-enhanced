using System.Collections.Generic;

namespace SFSEnhanced.Mod.Packaging
{
    public sealed class PackageManifest
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string MinimumSfsEnhancedVersion { get; set; }
        public List<PackageDependency> Dependencies { get; set; } = new List<PackageDependency>();
        public List<PackageFile> Files { get; set; } = new List<PackageFile>();
    }

    public sealed class PackageDependency
    {
        public string Id { get; set; }
        public string MinimumVersion { get; set; }
    }

    public sealed class PackageFile
    {
        public string Source { get; set; }
        public string Target { get; set; }
    }
}
