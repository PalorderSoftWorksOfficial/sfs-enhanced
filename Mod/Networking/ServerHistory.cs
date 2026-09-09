using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SFSEnhanced.Mod.Networking
{
    public sealed class ServerHistoryEntry
    {
        public string Host { get; set; }
        public int? Port { get; set; }
        public string Name { get; set; }
        public DateTime LastConnectedUtc { get; set; }
    }

    public static class ServerHistory
    {
        private const string Key = "sfs_enhanced.server_history";
        private const int MaxEntries = 12;

        public static List<ServerHistoryEntry> GetAll()
        {
            var json = UnityEngine.PlayerPrefs.GetString(Key, "[]");
            try
            {
                return JsonConvert.DeserializeObject<List<ServerHistoryEntry>>(json) ?? new List<ServerHistoryEntry>();
            }
            catch
            {
                return new List<ServerHistoryEntry>();
            }
        }

        public static void Record(string host, int? port, string name)
        {
            if (string.IsNullOrWhiteSpace(host)) return;
            host = host.Trim();
            var entries = GetAll();
            entries.RemoveAll(x => string.Equals(x.Host, host, StringComparison.OrdinalIgnoreCase) && x.Port == port);
            entries.Insert(0, new ServerHistoryEntry
            {
                Host = host,
                Port = port,
                Name = string.IsNullOrWhiteSpace(name) ? host : name.Trim(),
                LastConnectedUtc = DateTime.UtcNow
            });
            Save(entries.Take(MaxEntries).ToList());
        }

        public static void Remove(ServerHistoryEntry entry)
        {
            if (entry == null) return;
            var entries = GetAll();
            entries.RemoveAll(x => string.Equals(x.Host, entry.Host, StringComparison.OrdinalIgnoreCase) && x.Port == entry.Port);
            Save(entries);
        }

        private static void Save(List<ServerHistoryEntry> entries)
        {
            UnityEngine.PlayerPrefs.SetString(Key, JsonConvert.SerializeObject(entries));
            UnityEngine.PlayerPrefs.Save();
        }
    }
}
