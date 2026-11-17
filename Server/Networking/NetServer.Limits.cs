using System;
using System.Collections.Concurrent;
using System.Linq;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Server.Networking
{
    public partial class NetServer
    {
        public const int MaxUploadChunks = 256;
        public const int MaxUploadBytes = 8 * 1024 * 1024;
        public const int MaxUploadChunkBytes = 96 * 1024;
        public const int MaxWorldDownloadBytes = 8 * 1024 * 1024;
        public const int MaxUploadedBuilds = 1500;
        public const int MaxUploadedClaims = 512;

        private readonly ConcurrentDictionary<string, RateBucket> _rateBuckets = new();
        private readonly ConcurrentDictionary<string, string> _activeUploads = new();

        private sealed class RateBucket
        {
            public double Tokens;
            public DateTime RefilledUtc = DateTime.UtcNow;
        }

        private bool TryConsume(string playerId, string family, double burst, double perSecond, int cost = 1)
        {
            if (string.IsNullOrEmpty(playerId)) return false;
            string key = playerId + ":" + family;
            RateBucket bucket = _rateBuckets.GetOrAdd(key, _ => new RateBucket { Tokens = burst });
            lock (bucket)
            {
                DateTime now = DateTime.UtcNow;
                double elapsed = (now - bucket.RefilledUtc).TotalSeconds;
                if (elapsed > 0)
                {
                    bucket.Tokens = Math.Min(burst, bucket.Tokens + elapsed * perSecond);
                    bucket.RefilledUtc = now;
                }
                if (bucket.Tokens < cost) return false;
                bucket.Tokens -= cost;
                return true;
            }
        }

        private void ReleaseRateBuckets(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            foreach (string key in _rateBuckets.Keys.Where(k => k.StartsWith(playerId + ":", StringComparison.Ordinal)).ToList())
                _rateBuckets.TryRemove(key, out _);
        }

        private sealed class UploadBuffer
        {
            public string PlayerId;
            public string UploadId;
            public int TotalChunks;
            public int NextIndex;
            public int Bytes;
            public DateTime LastActivityUtc = DateTime.UtcNow;
            public readonly System.Collections.Generic.List<string> Chunks = new();
        }

        private void SweepExpiredUploads()
        {
            double idleSeconds = Math.Max(1, _config.UploadIdleSeconds);
            DateTime now = DateTime.UtcNow;
            foreach (var entry in _pendingUploads.ToList())
            {
                if ((now - entry.Value.LastActivityUtc).TotalSeconds <= idleSeconds) continue;
                AbortUpload(entry.Value.PlayerId, entry.Value.UploadId);
            }
        }

        private void AbortUpload(string playerId, string uploadId)
        {
            if (!string.IsNullOrEmpty(uploadId)) _pendingUploads.TryRemove(playerId + ":" + uploadId, out _);
            if (_activeUploads.TryGetValue(playerId, out string active) && string.Equals(active, uploadId, StringComparison.Ordinal))
                _activeUploads.TryRemove(playerId, out _);
        }

        public static bool IsValidWorldName(string name) => IsValidLabel(name, 64);

        public static bool IsValidLabel(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            string trimmed = value.Trim();
            if (trimmed.Length == 0 || trimmed.Length > maxLength) return false;
            foreach (char c in trimmed) if (c < 0x20 || c == 0x7F) return false;
            return true;
        }

        public static bool IsValidRegionClaim(ClaimCreatePacket request)
        {
            if (request == null) return false;
            if (double.IsNaN(request.CenterX) || double.IsInfinity(request.CenterX)) return false;
            if (double.IsNaN(request.CenterY) || double.IsInfinity(request.CenterY)) return false;
            if (double.IsNaN(request.RadiusMeters) || double.IsInfinity(request.RadiusMeters)) return false;
            if (Math.Abs(request.CenterX) > 1000000.0 || Math.Abs(request.CenterY) > 1000000.0) return false;
            return request.RadiusMeters >= 1.0 && request.RadiusMeters <= 50000.0;
        }

        private void ReleaseControlledBuilds(string worldId, string playerId)
        {
            if (string.IsNullOrEmpty(worldId) || string.IsNullOrEmpty(playerId)) return;
            var world = _worlds.Get(worldId);
            if (world == null) return;
            bool changed = false;
            foreach (BuildSnapshot build in world.Builds)
            {
                if (build == null || !string.Equals(build.ControllingPlayerId, playerId, StringComparison.Ordinal)) continue;
                build.ControllingPlayerId = null;
                changed = true;
            }
            if (changed) _worlds.Persist(worldId);
        }

        private bool CanAccessWorld(WorldRecord world, string playerId)
        {
            if (world == null) return false;
            if (world.IsPublic) return true;
            if (string.Equals(world.OwnerPlayerId, playerId, StringComparison.Ordinal)) return true;
            if (world.Whitelist != null && world.Whitelist.Contains(playerId)) return true;
            return _friends.AreFriends(world.OwnerPlayerId, playerId);
        }
    }
}
