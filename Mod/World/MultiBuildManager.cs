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
    public class MultiBuildManager
    {
        private readonly NetClient _client;
        private readonly Dictionary<string, RemoteBuild> _remoteBuilds = new Dictionary<string, RemoteBuild>();
        private float _publishTimer;
        private const float PublishInterval = 0.1f;

        public string LocalBuildId { get; private set; }

        public MultiBuildManager(NetClient client)
        {
            _client = client;
            _client.OnPacket += HandlePacket;
        }

        public void OnWorldSceneReady()
        {
            foreach (var remote in _remoteBuilds.Values.ToList())
            {
                if (remote.Rocket == null && !string.IsNullOrEmpty(remote.PendingJson))
                    TrySpawnRocket(remote, remote.PendingJson);
            }
        }

        public void ResetWorld()
        {
            foreach (var remote in _remoteBuilds.Values.ToList())
                DestroyRemoteRocket(remote);

            _remoteBuilds.Clear();
            LocalBuildId = null;
            _publishTimer = 0f;
        }

        private void HandlePacket(PacketType type, string json)
        {
            switch (type)
            {
                case PacketType.WorldJoinAck:
                    var ack = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldJoinAckPacket>(json);
                    if (ack == null || !ack.Accepted) return;
                    ResetWorld();
                    foreach (var build in ack.Builds ?? new List<Shared.Models.BuildSnapshot>())
                    {
                        if (build.OwnerPlayerId == _client.PlayerId)
                            LocalBuildId = build.BuildId;
                        else
                            SpawnOrUpdateRemoteBuild(build);
                    }
                    break;

                case PacketType.WorldLeave:
                    ResetWorld();
                    break;

                case PacketType.BuildSpawn:
                    SpawnOrUpdateRemoteBuild(Newtonsoft.Json.JsonConvert.DeserializeObject<Shared.Models.BuildSnapshot>(json));
                    break;

                case PacketType.BuildStateUpdate:
                    ApplyStateUpdate(Newtonsoft.Json.JsonConvert.DeserializeObject<BuildStateUpdatePacket>(json));
                    break;

                case PacketType.BuildRemove:
                    var removed = Newtonsoft.Json.JsonConvert.DeserializeObject<Shared.Models.BuildSnapshot>(json);
                    if (removed != null)
                    {
                        if (removed.BuildId == LocalBuildId)
                            LocalBuildId = null;
                        RemoveRemoteBuild(removed.BuildId);
                    }
                    break;
            }
        }

        private void SpawnOrUpdateRemoteBuild(Shared.Models.BuildSnapshot snapshot)
        {
            if (snapshot == null || snapshot.BuildId == LocalBuildId) return;

            if (_remoteBuilds.TryGetValue(snapshot.BuildId, out var existing))
            {
                existing.TargetPos = new Double2(snapshot.PosX, snapshot.PosY);
                existing.TargetVel = new Double2(snapshot.VelX, snapshot.VelY);
                existing.TargetRotation = (float)snapshot.RotationDegrees;
                existing.TargetAngularVelocity = (float)snapshot.AngularVelocity;
                existing.PendingJson = string.IsNullOrEmpty(snapshot.PartsBlueprintJson) ? existing.PendingJson : snapshot.PartsBlueprintJson;
                if (existing.Rocket == null && !string.IsNullOrEmpty(existing.PendingJson))
                    TrySpawnRocket(existing, existing.PendingJson);
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
            ApplyTransform(remote, true);
        }

        private void ApplyStateUpdate(BuildStateUpdatePacket update)
        {
            if (update == null || update.BuildId == LocalBuildId) return;
            if (!_remoteBuilds.TryGetValue(update.BuildId, out var remote)) return;

            remote.TargetPos = new Double2(update.PosX, update.PosY);
            remote.TargetVel = new Double2(update.VelX, update.VelY);
            remote.TargetRotation = (float)update.RotationDegrees;
            remote.TargetAngularVelocity = (float)update.AngularVelocity;
        }

        private void RemoveRemoteBuild(string buildId)
        {
            if (!_remoteBuilds.TryGetValue(buildId, out var remote)) return;
            DestroyRemoteRocket(remote);
            _remoteBuilds.Remove(buildId);
        }

        private static void DestroyRemoteRocket(RemoteBuild remote)
        {
            if (remote.Rocket == null) return;
            try { RocketManager.DestroyRocket(remote.Rocket, DestructionReason.Intentional); }
            catch (Exception e) { Debug.LogWarning($"[SFSEnhanced] DestroyRocket failed: {e.Message}"); }
            remote.Rocket = null;
        }

        public void TickInterpolation(float deltaTime)
        {
            foreach (var remote in _remoteBuilds.Values)
            {
                if (remote.Rocket != null)
                    ApplyTransform(remote, false, deltaTime);
            }
        }

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
                PublishLocalBuild(SnapshotFromRocket(local, LocalBuildId, _client.PlayerId));
                return;
            }

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

        private void ApplyTransform(RemoteBuild remote, bool immediate, float deltaTime = 0.02f)
        {
            var rocket = remote.Rocket;
            if (rocket == null || rocket.location?.Value?.planet == null || WorldTime.main == null) return;

            var current = rocket.location.Value;
            Double2 pos = immediate
                ? remote.TargetPos
                : Double2.Lerp(current.position, remote.TargetPos, Mathf.Clamp01(deltaTime * 10f));

            var location = new Location(WorldTime.main.worldTime, current.planet, pos, remote.TargetVel);
            rocket.physics.SetLocationAndState(location, physicsMode: false);
            rocket.rb2d.transform.eulerAngles = new Vector3(0f, 0f, immediate
                ? remote.TargetRotation
                : Mathf.LerpAngle(rocket.rb2d.transform.eulerAngles.z, remote.TargetRotation, deltaTime * 10f));
            rocket.rb2d.angularVelocity = remote.TargetAngularVelocity;
        }

        private async void PublishLocalBuild(Shared.Models.BuildSnapshot localSnapshot)
        {
            await _client.SendAsync(PacketType.BuildSpawn, localSnapshot);
        }

        private async void PublishLocalState(BuildStateUpdatePacket update)
        {
            await _client.SendAsync(PacketType.BuildStateUpdate, update);
        }

        private static Shared.Models.BuildSnapshot SnapshotFromRocket(Rocket rocket, string buildId, string ownerPlayerId)
        {
            var save = new RocketSave(rocket);
            var loc = rocket.location.Value;
            return new Shared.Models.BuildSnapshot
            {
                BuildId = buildId,
                OwnerPlayerId = ownerPlayerId,
                OwnerPlayerName = save.rocketName,
                DisplayName = save.rocketName,
                Kind = Shared.Models.BuildKind.Rocket,
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
            public Shared.Models.BuildKind Kind;
            public Double2 TargetPos;
            public Double2 TargetVel;
            public float TargetRotation;
            public float TargetAngularVelocity;
            public Rocket Rocket;
            public string PendingJson;
        }
    }
}
