using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Parsers.Json;
using SFS.World;
using SFSEnhanced.Mod.Networking;
using SFSEnhanced.Shared.Protocol;
using UnityEngine;

namespace SFSEnhanced.Mod.World
{
    /// <summary>
    /// Syncs every build in the joined world into the live World scene via
    /// <see cref="RocketManager.LoadRocket"/> / <see cref="RocketSave"/>.
    /// </summary>
    public class MultiBuildManager
    {
        private readonly NetClient _client;
        private readonly Dictionary<string, RemoteBuild> _remoteBuilds = new();
        private float _publishTimer;
        private const float PublishInterval = 0.1f; // 10 Hz local state broadcast

        public string LocalBuildId { get; set; }

        public MultiBuildManager(NetClient client)
        {
            _client = client;
            _client.OnPacket += HandlePacket;
        }

        public void OnWorldSceneReady()
        {
            // Re-apply any builds that arrived before the World scene was up.
            foreach (var remote in _remoteBuilds.Values.ToList())
            {
                if (remote.Rocket == null && !string.IsNullOrEmpty(remote.PendingJson))
                    TrySpawnRocket(remote, remote.PendingJson);
            }
        }

        private void HandlePacket(PacketType type, string json)
        {
            switch (type)
            {
                case PacketType.WorldJoinAck:
                    var ack = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldJoinAckPacket>(json);
                    if (ack?.Builds == null) return;
                    foreach (var build in ack.Builds)
                        SpawnOrUpdateRemoteBuild(build);
                    break;

                case PacketType.BuildSpawn:
                    SpawnOrUpdateRemoteBuild(Newtonsoft.Json.JsonConvert.DeserializeObject<BuildSnapshot>(json));
                    break;

                case PacketType.BuildStateUpdate:
                    ApplyStateUpdate(Newtonsoft.Json.JsonConvert.DeserializeObject<BuildStateUpdatePacket>(json));
                    break;

                case PacketType.BuildRemove:
                    var removed = Newtonsoft.Json.JsonConvert.DeserializeObject<BuildSnapshot>(json);
                    if (removed != null) RemoveRemoteBuild(removed.BuildId);
                    break;
            }
        }

        private void SpawnOrUpdateRemoteBuild(BuildSnapshot snapshot)
        {
            if (snapshot == null || snapshot.BuildId == LocalBuildId) return;

            if (_remoteBuilds.TryGetValue(snapshot.BuildId, out var existing))
            {
                existing.TargetPos = new Double2(snapshot.PosX, snapshot.PosY);
                existing.TargetVel = new Double2(snapshot.VelX, snapshot.VelY);
                existing.TargetRotation = (float)snapshot.RotationDegrees;
                existing.TargetAngularVelocity = (float)snapshot.AngularVelocity;
                if (existing.Rocket == null && !string.IsNullOrEmpty(snapshot.PartsBlueprintJson))
                    TrySpawnRocket(existing, snapshot.PartsBlueprintJson);
                return;
            }

            var remote = new RemoteBuild
            {
                BuildId = snapshot.BuildId,
                OwnerName = snapshot.OwnerPlayerName,
                Kind = snapshot.Kind,
                TargetPos = new Double2(snapshot.PosX, snapshot.PosY),
                TargetVel = new Double2(snapshot.VelX, snapshot.VelY),
                TargetRotation = (float)snapshot.RotationDegrees,
                TargetAngularVelocity = (float)snapshot.AngularVelocity,
                PendingJson = snapshot.PartsBlueprintJson,
            };

            _remoteBuilds[snapshot.BuildId] = remote;

            if (GameManager.main != null && !string.IsNullOrEmpty(snapshot.PartsBlueprintJson))
                TrySpawnRocket(remote, snapshot.PartsBlueprintJson);
            else
                Debug.Log($"[SFSEnhanced] Queued remote build '{snapshot.DisplayName}' until World scene is ready");
        }

        private void TrySpawnRocket(RemoteBuild remote, string rocketSaveJson)
        {
            if (GameManager.main == null) return;

            RocketSave save;
            try
            {
                save = JsonWrapper.FromJson<RocketSave>(rocketSaveJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SFSEnhanced] Bad RocketSave JSON for {remote.BuildId}: {e.Message}");
                return;
            }

            if (save == null)
            {
                Debug.LogError($"[SFSEnhanced] Null RocketSave for {remote.BuildId}");
                return;
            }

            // Tag so we can find it after LoadRocket (API returns void).
            save.rocketName = $"[MP]{remote.OwnerName}:{remote.BuildId}";

            var before = new HashSet<Rocket>(GameManager.main.rockets);
            try
            {
                RocketManager.LoadRocket(save, out _);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SFSEnhanced] LoadRocket failed for {remote.BuildId}: {e.Message}");
                return;
            }

            Rocket spawned = GameManager.main.rockets.FirstOrDefault(r => !before.Contains(r));
            if (spawned == null)
                spawned = GameManager.main.rockets.LastOrDefault(r => r.rocketName == save.rocketName);

            if (spawned == null)
            {
                Debug.LogWarning($"[SFSEnhanced] Spawned rocket not found for {remote.BuildId}");
                return;
            }

            remote.Rocket = spawned;
            remote.PendingJson = null;
            ApplyTransform(remote, immediate: true);
            Debug.Log($"[SFSEnhanced] Spawned remote rocket for {remote.OwnerName} ({remote.BuildId})");
        }

        private void ApplyStateUpdate(BuildStateUpdatePacket update)
        {
            if (update == null) return;
            if (!_remoteBuilds.TryGetValue(update.BuildId, out var remote)) return;

            remote.TargetPos = new Double2(update.PosX, update.PosY);
            remote.TargetVel = new Double2(update.VelX, update.VelY);
            remote.TargetRotation = (float)update.RotationDegrees;
            remote.TargetAngularVelocity = (float)update.AngularVelocity;
        }

        private void RemoveRemoteBuild(string buildId)
        {
            if (!_remoteBuilds.TryGetValue(buildId, out var remote)) return;

            if (remote.Rocket != null)
            {
                try { RocketManager.DestroyRocket(remote.Rocket, DestructionReason.Intentional); }
                catch (Exception e) { Debug.LogWarning($"[SFSEnhanced] DestroyRocket: {e.Message}"); }
                remote.Rocket = null;
            }
            _remoteBuilds.Remove(buildId);
        }

        public void TickInterpolation(float deltaTime)
        {
            foreach (var remote in _remoteBuilds.Values)
            {
                if (remote.Rocket == null) continue;
                ApplyTransform(remote, immediate: false, deltaTime);
            }
        }

        /// <summary>Broadcast the local player's rocket at a fixed rate while connected + in world.</summary>
        public void TickLocalPublish(float deltaTime)
        {
            if (!_client.IsConnected || string.IsNullOrEmpty(_client.CurrentWorldId)) return;
            if (GameManager.main == null) return;
            if (!(PlayerController.main?.player?.Value is Rocket local)) return;

            _publishTimer += deltaTime;
            if (_publishTimer < PublishInterval) return;
            _publishTimer = 0f;

            if (string.IsNullOrEmpty(LocalBuildId))
            {
                LocalBuildId = Guid.NewGuid().ToString("N");
                var snap = SnapshotFromRocket(local, LocalBuildId, _client.PlayerId);
                PublishLocalBuild(snap);
            }
            else
            {
                var loc = local.location.Value;
                PublishLocalState(new BuildStateUpdatePacket
                {
                    WorldId = _client.CurrentWorldId,
                    BuildId = LocalBuildId,
                    PosX = loc.position.x,
                    PosY = loc.position.y,
                    VelX = loc.velocity.x,
                    VelY = loc.velocity.y,
                    RotationDegrees = local.rb2d.transform.eulerAngles.z,
                    AngularVelocity = local.rb2d.angularVelocity,
                    ThrottlePercent = local.throttle.throttlePercent.Value,
                });
            }
        }

        private void ApplyTransform(RemoteBuild remote, bool immediate, float deltaTime = 0.02f)
        {
            var rocket = remote.Rocket;
            if (rocket == null || rocket.location?.Value?.planet == null) return;

            var current = rocket.location.Value;
            Double2 pos = immediate
                ? remote.TargetPos
                : Double2.Lerp(current.position, remote.TargetPos, Mathf.Clamp01(deltaTime * 10f));
            Double2 vel = remote.TargetVel;

            var location = new Location(WorldTime.main.worldTime, current.planet, pos, vel);
            rocket.physics.SetLocationAndState(location, physicsMode: false);
            rocket.rb2d.transform.eulerAngles = new Vector3(0f, 0f, immediate
                ? remote.TargetRotation
                : Mathf.LerpAngle(rocket.rb2d.transform.eulerAngles.z, remote.TargetRotation, deltaTime * 10f));
            rocket.rb2d.angularVelocity = remote.TargetAngularVelocity;
        }

        public async void PublishLocalBuild(BuildSnapshot localSnapshot)
        {
            LocalBuildId = localSnapshot.BuildId;
            await _client.SendAsync(PacketType.BuildSpawn, localSnapshot);
        }

        public async void PublishLocalState(BuildStateUpdatePacket update)
        {
            await _client.SendAsync(PacketType.BuildStateUpdate, update);
        }

        public static BuildSnapshot SnapshotFromRocket(Rocket rocket, string buildId, string ownerPlayerId)
        {
            var save = new RocketSave(rocket);
            var loc = rocket.location.Value;
            return new BuildSnapshot
            {
                BuildId = buildId,
                OwnerPlayerId = ownerPlayerId,
                OwnerPlayerName = save.rocketName,
                DisplayName = save.rocketName,
                Kind = BuildKind.Rocket,
                PosX = loc.position.x,
                PosY = loc.position.y,
                VelX = loc.velocity.x,
                VelY = loc.velocity.y,
                RotationDegrees = rocket.rb2d.transform.eulerAngles.z,
                AngularVelocity = rocket.rb2d.angularVelocity,
                PartsBlueprintJson = JsonWrapper.ToJson(save, pretty: false),
                ControllingPlayerId = ownerPlayerId,
                LastUpdatedUtc = DateTime.UtcNow,
            };
        }

        private class RemoteBuild
        {
            public string BuildId;
            public string OwnerName;
            public BuildKind Kind;
            public Double2 TargetPos, TargetVel;
            public float TargetRotation, TargetAngularVelocity;
            public Rocket Rocket;
            public string PendingJson;
        }
    }
}
