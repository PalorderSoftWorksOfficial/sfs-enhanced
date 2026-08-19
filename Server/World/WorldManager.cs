using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SFSEnhanced.Server.Persistence;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Server.World
{
    /// <summary>
    /// Owns every hosted world's authoritative state. Worlds are cached in memory
    /// while active and flushed to the FileStore on every meaningful change
    /// (build add/remove/claim change) plus periodically for position updates —
    /// see NetServer's autosave loop.
    /// </summary>
    public class WorldManager
    {
        private readonly FileStore _store;
        private readonly ConcurrentDictionary<string, WorldRecord> _cache = new();

        public WorldManager(FileStore store)
        {
            _store = store;
            foreach (var id in _store.ListIds("worlds"))
            {
                var world = _store.Load<WorldRecord>("worlds", id);
                if (world != null) _cache[id] = world;
            }
        }

        public WorldRecord Create(string name, string ownerPlayerId, bool isPublic, string planetPackId)
        {
            var world = new WorldRecord
            {
                Name = name,
                OwnerPlayerId = ownerPlayerId,
                IsPublic = isPublic,
                PlanetPackId = planetPackId,
            };
            _cache[world.WorldId] = world;
            _store.Save("worlds", world.WorldId, world);
            return world;
        }

        public WorldRecord Get(string worldId) => _cache.TryGetValue(worldId, out var w) ? w : null;

        public IEnumerable<WorldRecord> ListPublic() => _cache.Values.Where(w => w.IsPublic);

        public IEnumerable<WorldRecord> All() => _cache.Values;

        public void Persist(string worldId)
        {
            if (_cache.TryGetValue(worldId, out var world))
            {
                world.LastModifiedUtc = DateTime.UtcNow;
                _store.Save("worlds", worldId, world);
            }
        }

        public void PersistAll()
        {
            foreach (var id in _cache.Keys) Persist(id);
        }

        // ---- Builds ----

        public BuildSnapshot AddOrUpdateBuild(string worldId, BuildSnapshot build)
        {
            var world = Get(worldId);
            if (world == null) return null;
            world.Builds.RemoveAll(b => b.BuildId == build.BuildId);
            build.LastUpdatedUtc = DateTime.UtcNow;
            world.Builds.Add(build);
            return build;
        }

        public void ApplyStateUpdate(string worldId, BuildStateUpdatePacket update)
        {
            var world = Get(worldId);
            var build = world?.Builds.FirstOrDefault(b => b.BuildId == update.BuildId);
            if (build == null) return;

            build.PosX = update.PosX;
            build.PosY = update.PosY;
            build.VelX = update.VelX;
            build.VelY = update.VelY;
            build.RotationDegrees = update.RotationDegrees;
            build.AngularVelocity = update.AngularVelocity;
            if (!string.IsNullOrEmpty(update.PlanetAddress))
                build.PlanetAddress = update.PlanetAddress;
            build.LastUpdatedUtc = DateTime.UtcNow;
        }

        public bool RemoveBuild(string worldId, string buildId)
        {
            var world = Get(worldId);
            return world != null && world.Builds.RemoveAll(b => b.BuildId == buildId) > 0;
        }

        public BuildSnapshot FindBuild(string worldId, string buildId) =>
            Get(worldId)?.Builds.FirstOrDefault(b => b.BuildId == buildId);
    }
}
