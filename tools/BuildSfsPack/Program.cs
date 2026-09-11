using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using SFSEnhanced.Shared.Packaging;

namespace SFSEnhanced.Tools.BuildSfsPack
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                return Run(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("error: " + ex.Message);
                return 2;
            }
        }

        private static int Run(string[] args)
        {
            string output = null;
            string id = null;
            string name = null;
            string version = "1.0.0";
            string author = "PalorderSoftWorksOfficial";
            string description = null;
            string minimumSfsEnhancedVersion = null;
            var files = new List<(string source, string target)>();
            var dirs = new List<(string source, string targetRoot)>();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"Missing value for {arg}.");
                switch (arg)
                {
                    case "--out": output = Next(); break;
                    case "--id": id = Next(); break;
                    case "--name": name = Next(); break;
                    case "--version": version = Next(); break;
                    case "--author": author = Next(); break;
                    case "--description": description = Next(); break;
                    case "--min-sfs-enhanced": minimumSfsEnhancedVersion = Next(); break;
                    case "--file":
                    {
                        string source = Next();
                        string target = Next();
                        if (string.IsNullOrWhiteSpace(target)) throw new ArgumentException("--file expects <source> <target>.");
                        files.Add((source, target));
                        break;
                    }
                    case "--dir":
                    {
                        string source = Next();
                        string targetRoot = Next();
                        if (string.IsNullOrWhiteSpace(targetRoot)) throw new ArgumentException("--dir expects <source> <targetRoot>.");
                        dirs.Add((source, targetRoot));
                        break;
                    }
                    default: throw new ArgumentException($"Unknown argument '{arg}'.");
                }
            }

            if (string.IsNullOrWhiteSpace(output)) throw new ArgumentException("--out is required.");
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("--id is required.");
            if (!Version.TryParse(version, out _)) throw new ArgumentException($"Invalid --version '{version}'.");
            if (files.Count == 0 && dirs.Count == 0) throw new ArgumentException("At least one --file or --dir payload is required.");

            var manifest = new PackageManifest
            {
                Id = id,
                Name = string.IsNullOrWhiteSpace(name) ? id : name,
                Version = version,
                Author = author,
                Description = description ?? $"{id} package built by BuildSfsPack.",
                MinimumSfsEnhancedVersion = minimumSfsEnhancedVersion
            };

            string payloadRoot = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output)) ?? ".", ".packpayload-" + Guid.NewGuid().ToString("N"));
            try
            {
                foreach (var (source, target) in files) AddFile(manifest, payloadRoot, source, target);
                foreach (var (source, targetRoot) in dirs) AddDir(manifest, payloadRoot, source, targetRoot);
                if (manifest.Files.Count == 0) throw new ArgumentException("Payload resolved to zero files.");
                WritePack(output, manifest, payloadRoot);
            }
            finally
            {
                if (Directory.Exists(payloadRoot)) Directory.Delete(payloadRoot, true);
            }
            Console.WriteLine(output);
            return 0;
        }

        private static void AddFile(PackageManifest manifest, string payloadRoot, string source, string target)
        {
            string sourceFull = Path.GetFullPath(source);
            if (!File.Exists(sourceFull)) throw new FileNotFoundException("Payload file not found.", source);
            ValidateTarget(target);
            string stagingPath = Path.Combine(payloadRoot, target.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(stagingPath));
            File.Copy(sourceFull, stagingPath, true);
            manifest.Files.Add(new PackageFile { Source = target, Target = target });
        }

        private static void AddDir(PackageManifest manifest, string payloadRoot, string source, string targetRoot)
        {
            string sourceFull = Path.GetFullPath(source);
            if (!Directory.Exists(sourceFull)) throw new DirectoryNotFoundException("Payload directory not found: " + source);
            ValidateTarget(targetRoot + "/payload");
            foreach (string file in Directory.GetFiles(sourceFull, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(sourceFull.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
                string extension = Path.GetExtension(relative).ToLowerInvariant();
                if (extension == ".pdb" || extension == ".xml") continue;
                AddFile(manifest, payloadRoot, file, targetRoot + "/" + relative);
            }
        }

        private static void ValidateTarget(string target)
        {
            string first = target.Replace('/', Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar)[0];
            if (!PackageInstaller.TargetRoots.Contains(first, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"Target '{target}' does not start with a supported package root ({string.Join(", ", PackageInstaller.TargetRoots)}).");
        }

        private static void WritePack(string output, PackageManifest manifest, string payloadRoot)
        {
            string outputFull = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFull));
            if (File.Exists(outputFull)) File.Delete(outputFull);
            using (var zip = ZipFile.Open(outputFull, ZipArchiveMode.Create))
            {
                string manifestPath = Path.Combine(payloadRoot, PackageInstaller.ManifestName);
                File.WriteAllText(manifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));
                zip.CreateEntryFromFile(manifestPath, PackageInstaller.ManifestName);
                foreach (var file in manifest.Files)
                    zip.CreateEntryFromFile(Path.Combine(payloadRoot, file.Source.Replace('/', Path.DirectorySeparatorChar)), file.Source);
            }
        }
    }
}
