using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SFSEnhanced.Shared.Networking
{
    public interface IEndpointStorage
    {
        string Read(string key);
        bool Write(string key, string value);
        void Remove(string key);
    }

    public enum PinMatch
    {
        Missing,
        Match,
        Changed
    }

    public sealed class ServerIdentityPin
    {
        public string EndpointKey;
        public string HostInput;
        public int? DeclaredPort;
        public string ResolvedHost;
        public int? ResolvedPort;
        public string Fingerprint;
        public string ShortFingerprint;
        public string ServerName;
        public string Trust = "pinned";
        public string Origin;
        public DateTime FirstSeenUtc;
        public DateTime LastVerifiedUtc;
        public int RePinCount;
        public string PreviousFingerprint;
    }

    public sealed class PinLookup
    {
        public PinMatch Match;
        public ServerIdentityPin Pin;
        public string PinnedFingerprint;
        public bool StoreUnreadable;
    }

    internal sealed class PinDocument
    {
        public int Version = 1;
        public List<ServerIdentityPin> Pins = new List<ServerIdentityPin>();
    }

    public sealed class ServerIdentityPinStore
    {
        public const string StorageKey = "sfs_enhanced.pins";
        public const string CorruptBackupKey = "sfs_enhanced.pins.corrupt";
        public const int MaxPins = 64;

        private readonly IEndpointStorage _storage;
        private readonly object _lock = new object();

        public ServerIdentityPinStore(IEndpointStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public PinLookup Lookup(string endpointKey, string fingerprint)
        {
            if (string.IsNullOrWhiteSpace(endpointKey)) return new PinLookup { Match = PinMatch.Missing };
            lock (_lock)
            {
                PinDocument document = Load(out bool unreadable);
                if (unreadable) return new PinLookup { Match = PinMatch.Missing, StoreUnreadable = true };
                ServerIdentityPin pin = document.Pins.FirstOrDefault(x =>
                    string.Equals(x.EndpointKey, endpointKey, StringComparison.OrdinalIgnoreCase));
                if (pin == null) return new PinLookup { Match = PinMatch.Missing };
                bool matches = string.Equals(pin.Fingerprint, fingerprint, StringComparison.OrdinalIgnoreCase);
                return new PinLookup
                {
                    Match = matches ? PinMatch.Match : PinMatch.Changed,
                    Pin = pin,
                    PinnedFingerprint = pin.Fingerprint
                };
            }
        }

        public IReadOnlyList<ServerIdentityPin> All()
        {
            lock (_lock)
            {
                return Load(out _).Pins.ToList();
            }
        }

        public bool Accept(ServerIdentityPin pin)
        {
            if (pin == null || string.IsNullOrWhiteSpace(pin.EndpointKey) || string.IsNullOrWhiteSpace(pin.Fingerprint))
                return false;
            lock (_lock)
            {
                PinDocument document = Load(out bool unreadable);
                if (unreadable) BackUpCorrupt();
                ServerIdentityPin existing = document.Pins.FirstOrDefault(x =>
                    string.Equals(x.EndpointKey, pin.EndpointKey, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    document.Pins.Remove(existing);
                    if (!string.Equals(existing.Fingerprint, pin.Fingerprint, StringComparison.OrdinalIgnoreCase))
                    {
                        pin.PreviousFingerprint = existing.Fingerprint;
                        pin.RePinCount = existing.RePinCount + 1;
                        if (pin.FirstSeenUtc == default(DateTime)) pin.FirstSeenUtc = existing.FirstSeenUtc;
                    }
                    else
                    {
                        pin.RePinCount = existing.RePinCount;
                        pin.PreviousFingerprint = existing.PreviousFingerprint;
                        if (pin.FirstSeenUtc == default(DateTime)) pin.FirstSeenUtc = existing.FirstSeenUtc;
                    }
                }
                if (pin.Trust != "pinned") pin.Trust = "pinned";
                document.Pins.Add(pin);
                Trim(document);
                return Save(document);
            }
        }

        public bool Touch(string endpointKey, string resolvedHost, int? resolvedPort, string serverName, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(endpointKey)) return false;
            lock (_lock)
            {
                PinDocument document = Load(out bool unreadable);
                if (unreadable) return false;
                ServerIdentityPin pin = document.Pins.FirstOrDefault(x =>
                    string.Equals(x.EndpointKey, endpointKey, StringComparison.OrdinalIgnoreCase));
                if (pin == null) return false;
                pin.LastVerifiedUtc = now;
                if (!string.IsNullOrWhiteSpace(resolvedHost)) pin.ResolvedHost = resolvedHost;
                if (resolvedPort.HasValue) pin.ResolvedPort = resolvedPort;
                if (!string.IsNullOrWhiteSpace(serverName)) pin.ServerName = serverName;
                return Save(document);
            }
        }

        public bool Forget(string endpointKey)
        {
            if (string.IsNullOrWhiteSpace(endpointKey)) return false;
            lock (_lock)
            {
                PinDocument document = Load(out bool unreadable);
                if (unreadable) BackUpCorrupt();
                int removed = document.Pins.RemoveAll(x =>
                    string.Equals(x.EndpointKey, endpointKey, StringComparison.OrdinalIgnoreCase));
                if (removed == 0) return true;
                return Save(document);
            }
        }

        private static void Trim(PinDocument document)
        {
            if (document.Pins.Count <= MaxPins) return;
            foreach (ServerIdentityPin stale in document.Pins
                .OrderBy(x => x.LastVerifiedUtc)
                .Take(document.Pins.Count - MaxPins)
                .ToList())
            {
                document.Pins.Remove(stale);
            }
        }

        private PinDocument Load(out bool unreadable)
        {
            unreadable = false;
            string json;
            try
            {
                json = _storage.Read(StorageKey);
            }
            catch
            {
                unreadable = true;
                return new PinDocument();
            }
            if (string.IsNullOrWhiteSpace(json)) return new PinDocument();
            try
            {
                PinDocument document = JsonConvert.DeserializeObject<PinDocument>(json);
                if (document == null)
                {
                    unreadable = true;
                    return new PinDocument();
                }
                if (document.Pins == null) document.Pins = new List<ServerIdentityPin>();
                document.Pins.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.EndpointKey) || string.IsNullOrWhiteSpace(x.Fingerprint));
                return document;
            }
            catch
            {
                unreadable = true;
                return new PinDocument();
            }
        }

        private void BackUpCorrupt()
        {
            try
            {
                string json = _storage.Read(StorageKey);
                if (!string.IsNullOrWhiteSpace(json)) _storage.Write(CorruptBackupKey, json);
            }
            catch
            {
            }
        }

        private bool Save(PinDocument document)
        {
            string json = JsonConvert.SerializeObject(document);
            if (!_storage.Write(StorageKey, json)) return false;
            string readBack;
            try
            {
                readBack = _storage.Read(StorageKey);
            }
            catch
            {
                return false;
            }
            return string.Equals(readBack, json, StringComparison.Ordinal);
        }
    }
}
