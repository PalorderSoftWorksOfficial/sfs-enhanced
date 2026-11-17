using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SFSEnhanced.Shared.Networking
{
    public enum TokenMigrationStatus
    {
        NoLegacyToken,
        Migrated,
        SkippedAmbiguous,
        SkippedExisting,
        WriteFailed
    }

    internal sealed class TokenDocument
    {
        public int Version = 1;
        public Dictionary<string, string> Tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class EndpointTokenStore
    {
        public const string StorageKey = "sfs_enhanced.tokens";
        public const string LegacyStorageKey = "sfs_enhanced.token";

        private readonly IEndpointStorage _storage;
        private readonly object _lock = new object();

        public EndpointTokenStore(IEndpointStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public string GetToken(string endpointKey)
        {
            if (string.IsNullOrWhiteSpace(endpointKey)) return null;
            lock (_lock)
            {
                if (!TryLoad(out TokenDocument document)) return null;
                return document.Tokens.TryGetValue(endpointKey, out string token) ? token : null;
            }
        }

        public bool HasToken(string endpointKey) => !string.IsNullOrEmpty(GetToken(endpointKey));

        public bool SetToken(string endpointKey, string token)
        {
            if (string.IsNullOrWhiteSpace(endpointKey)) return false;
            lock (_lock)
            {
                if (!TryLoad(out TokenDocument document)) return false;
                if (string.IsNullOrEmpty(token)) document.Tokens.Remove(endpointKey);
                else document.Tokens[endpointKey] = token;
                return Save(document);
            }
        }

        public TokenMigrationStatus MigrateLegacyToken(string endpointKey, string legacyToken)
        {
            if (string.IsNullOrEmpty(legacyToken)) return TokenMigrationStatus.NoLegacyToken;
            if (string.IsNullOrWhiteSpace(endpointKey)) return TokenMigrationStatus.SkippedAmbiguous;

            lock (_lock)
            {
                if (!TryLoad(out TokenDocument document)) return TokenMigrationStatus.WriteFailed;
                if (document.Tokens.TryGetValue(endpointKey, out string existing) && !string.IsNullOrEmpty(existing))
                    return TokenMigrationStatus.SkippedExisting;
                document.Tokens[endpointKey] = legacyToken;
                return Save(document) ? TokenMigrationStatus.Migrated : TokenMigrationStatus.WriteFailed;
            }
        }

        private bool TryLoad(out TokenDocument document)
        {
            document = new TokenDocument();
            string json;
            try
            {
                json = _storage.Read(StorageKey);
            }
            catch
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(json)) return true;
            try
            {
                TokenDocument parsed = JsonConvert.DeserializeObject<TokenDocument>(json);
                if (parsed == null) return false;
                if (parsed.Tokens == null)
                    parsed.Tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                else if (parsed.Tokens.Comparer != StringComparer.OrdinalIgnoreCase)
                    parsed.Tokens = new Dictionary<string, string>(parsed.Tokens, StringComparer.OrdinalIgnoreCase);
                document = parsed;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool Save(TokenDocument document)
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
