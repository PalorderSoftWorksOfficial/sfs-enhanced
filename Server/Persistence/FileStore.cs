using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace SFSEnhanced.Server.Persistence
{
    /// <summary>
    /// Minimal JSON-file persistence. One file per record, grouped into folders
    /// (worlds/, accounts/). No external database dependency, so `dotnet run`
    /// works out of the box and the data is human-inspectable / easy to back up.
    /// A per-path lock keeps concurrent writes from corrupting a file; this is
    /// intentionally simple rather than maximally fast — swap in SQLite/Postgres
    /// later if you need it, the AccountService/WorldManager only depend on the
    /// methods below.
    /// </summary>
    public class FileStore
    {
        private readonly string _root;
        private readonly object _lock = new object();

        public FileStore(string rootDir)
        {
            _root = rootDir;
            Directory.CreateDirectory(Path.Combine(_root, "worlds"));
            Directory.CreateDirectory(Path.Combine(_root, "accounts"));
        }

        public string WorldsDir => Path.Combine(_root, "worlds");
        public string AccountsDir => Path.Combine(_root, "accounts");

        public void Save<T>(string folder, string id, T record)
        {
            lock (_lock)
            {
                string path = Path.Combine(_root, folder, id + ".json");
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonConvert.SerializeObject(record, Formatting.Indented));
                File.Copy(tmp, path, overwrite: true);
                File.Delete(tmp);
            }
        }

        public T Load<T>(string folder, string id) where T : class
        {
            string path = Path.Combine(_root, folder, id + ".json");
            if (!File.Exists(path)) return null;
            lock (_lock)
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            }
        }

        public bool Exists(string folder, string id) =>
            File.Exists(Path.Combine(_root, folder, id + ".json"));

        public void Delete(string folder, string id)
        {
            lock (_lock)
            {
                string path = Path.Combine(_root, folder, id + ".json");
                if (File.Exists(path)) File.Delete(path);
            }
        }

        public string[] ListIds(string folder)
        {
            string dir = Path.Combine(_root, folder);
            if (!Directory.Exists(dir)) return Array.Empty<string>();
            var files = Directory.GetFiles(dir, "*.json");
            var ids = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
                ids[i] = Path.GetFileNameWithoutExtension(files[i]);
            return ids;
        }
    }
}
