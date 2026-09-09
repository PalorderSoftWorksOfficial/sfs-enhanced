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
        private Rocket _localRocket;
        private string _localBuildId;
        private float _publishTimer;
        private float _timeCorrectionTimer;
        private long _tick;
        private double _serverWorldTime;
        private double _serverTimewarp = 1.0;
        private bool _hasServerWorldTime;
        private const float PublishInterval = 1f / 15f;
        private const float TimeSyncInterval = 0.25f;

        public string LocalBuildId => _localBuildId;

        public MultiBuildManager(NetClient client)
        {
            _client = client;
            _client.OnPacket += HandlePacket;
        }

        public void OnWorldSceneReady()
        {
            foreach (var remote in _remoteBuilds.Values.ToList())
            {
                if (remote.Rocket == null && !string.IsNullOrEmpty(remote.PendingJson)) TrySpawnRocket(remote, remote.PendingJson);
            }
        }

        public void ResetWorld()
        {
            foreach (var remote in _remoteBuilds.Values.ToList()) DestroyRemoteRocket(remote);
            _remoteBuilds.Clear();
            _localRocket = null;
            _localBuildId = null;
            _publishTimer = 0f;
            _timeCorrectionTimer = 0f;
            _hasServerWorldTime = false;
        }

        private void HandlePacket(PacketType type, string json)
        {
            try { HandlePacketInternal(type, json); }
            catch (Exception e) { Debug.LogError($"[SFSEnhanced] Build packet handling failed ({type}): {e}"); }
        }

        private void HandlePacketInternal(PacketType type, string json)
        {
            switch (type)
            {
                case PacketType.WorldJoinAck:
                    var ack = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldJoinAckPacket>(json);
                    if (ack == null || !ack.Accepted) return;
                    ResetWorld();
                    foreach (var build in ack.Builds ?? new List<BuildSnapshot>())
                    {
                        if (build.OwnerPlayerId == _client.PlayerId) _localBuildId = build.BuildId;
                        else SpawnOrUpdateRemoteBuild(build);
                    }
                    break;
                case PacketType.WorldLeave:
                    ResetWorld();
                    break;
                case PacketType.WorldTimeState:
                    var time = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldTimeStatePacket>(json);
                    if (time != null && time.WorldId == _client.CurrentWorldId)
                    {
                        _serverWorldTime = time.WorldTime;
                        _serverTimewarp = Math.Max(0.0, time.TimewarpMultiplier);
                        _hasServerWorldTime = true;
                    }
                    break;
                case PacketType.BuildSpawn:
                    SpawnOrUpdateRemoteBuild(Newtonsoft.Json.JsonConvert.DeserializeObject<BuildSnapshot>(json));
                    break;
                case PacketType.BuildStateUpdate:
                    ApplyStateUpdate(Newtonsoft.Json.JsonConvert.DeserializeObject<BuildStateUpdatePacket>(json));
                    break;
                case PacketType.BuildRemove:
                    var removed = Newtonsoft.Json.JsonConvert.DeserializeObject<BuildSnapshot>(json);
                    if (removed != null) RemoveBuild(removed.BuildId);
                    break;
                case PacketType.BuildControlGrant:
                    var grant = Newtonsoft.Json.JsonConvert.DeserializeObject<BuildControlGrantPacket>(json);
                    if (grant != null && grant.Granted && grant.ControllingPlayerId == _client.PlayerId) RequestLocalRefresh();
                    break;
            }
        }

        private void RequestLocalRefresh()
        {
            _publishTimer = PublishInterval;
        }

        private void SpawnOrUpdateRemoteBuild(BuildSnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.BuildId) || snapshot.BuildId == _localBuildId) return;
            if (_remoteBuilds.TryGetValue(snapshot.BuildId, out var existing))
            {
                existing.TargetPos = new Double2(snapshot.PosX, snapshot.PosY);
                existing.TargetVel = new Double2(snapshot.VelX, snapshot.VelY);
                existing.TargetRotation = (float)snapshot.RotationDegrees;
                existing.TargetAngularVelocity = (float)snapshot.AngularVelocity;
                existing.TargetThrottle = snapshot.ControllingPlayerId;
                existing.PendingJson = string.IsNullOrEmpty(snapshot.PartsBlueprintJson) ? existing.PendingJson : snapshot.PartsBlueprintJson;
                if (existing.Rocket == null && !string.IsNullOrEmpty(existing.PendingJson)) TrySpawnRocket(existing, existing.PendingJson);
                return;
            }
            var remote = new RemoteBuild
            {
                BuildId = snapshot.BuildId,
                OwnerName = snapshot.OwnerPlayerName,
                TargetPos = new Double2(snapshot.PosX, snapshot.PosY),
                TargetVel = new Double2(snapshot.VelX, snapshot.VelY),
                TargetRotation = (float)snapshot.RotationDegrees,
                TargetAngularVelocity = (float)snapshot.AngularVelocity,
                PendingJson = snapshot.PartsBlueprintJson
            };
            _remoteBuilds[snapshot.BuildId] = remote;
            if (GameManager.main != null && !string.IsNullOrEmpty(snapshot.PartsBlueprintJson)) TrySpawnRocket(remote, snapshot.PartsBlueprintJson);
        }

        private void TrySpawnRocket(RemoteBuild remote, string rocketSaveJson)
        {
            if (GameManager.main == null || WorldTime.main == null) return;
            RocketSave save;
            try { save = JsonWrapper.FromJson<RocketSave>(rocketSaveJson); }
            catch (Exception e) { Debug.LogError($"[SFSEnhanced] Bad RocketSave JSON for {remote.BuildId}: {e.Message}"); return; }
            if (save == null) return;
            save.rocketName = $"[MP] {remote.OwnerName}";
            var before = new HashSet<Rocket>(GameManager.main.rockets);
            try { RocketManager.LoadRocket(save, out _); }
            catch (Exception e) { Debug.LogError($"[SFSEnhanced] LoadRocket failed for {remote.BuildId}: {e.Message}"); return; }
            var spawned = GameManager.main.rockets.FirstOrDefault(r => !before.Contains(r));
            if (spawned == null) spawned = GameManager.main.rockets.LastOrDefault(r => r.rocketName == save.rocketName);
            if (spawned == null) return;
            remote.Rocket = spawned;
            remote.PendingJson = null;
            var ghost = spawned.gameObject.GetComponent<RemoteRocketGhost>() ?? spawned.gameObject.AddComponent<RemoteRocketGhost>();
            ghost.Bind(spawned);
            ApplyTransform(remote, true, 1f);
        }

        private void ApplyStateUpdate(BuildStateUpdatePacket update)
        {
            if (update == null || update.BuildId == _localBuildId) return;
            if (!_remoteBuilds.TryGetValue(update.BuildId, out var remote)) return;
            remote.TargetPos = new Double2(update.PosX, update.PosY);
            remote.TargetVel = new Double2(update.VelX, update.VelY);
            remote.TargetRotation = (float)update.RotationDegrees;
            remote.TargetAngularVelocity = (float)update.AngularVelocity;
            if (update.WorldTime > 0) _serverWorldTime = Math.Max(_serverWorldTime, update.WorldTime);
        }

        private void RemoveBuild(string buildId)
        {
            if (buildId == _localBuildId) _localBuildId = null;
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
            if (_hasServerWorldTime && WorldTime.main != null && !double.IsNaN(_serverWorldTime))
            {
                _timeCorrectionTimer += deltaTime;
                _serverWorldTime += deltaTime * _serverTimewarp;
                if (_timeCorrectionTimer >= TimeSyncInterval)
                {
                    _timeCorrectionTimer = 0f;
                    double error = _serverWorldTime - WorldTime.main.worldTime;
                    WorldTime.main.worldTime += Mathf.Clamp((float)error, -2f, 2f);
                    bool serverRealtime = _serverTimewarp <= 1.0;
                    if (Math.Abs(WorldTime.main.timewarpSpeed - _serverTimewarp) > 0.01 || WorldTime.main.realtimePhysics.Value != serverRealtime)
                        WorldTime.main.SetState(_serverTimewarp, serverRealtime, false);
                }
            }
            foreach (var remote in _remoteBuilds.Values)
            {
                if (remote.Rocket != null) ApplyTransform(remote, false, deltaTime);
            }
        }

        public void TickLocalPublish(float deltaTime)
        {
            if (!_client.IsConnected || string.IsNullOrEmpty(_client.CurrentWorldId) || GameManager.main == null) return;
            if (!(PlayerController.main?.player?.Value is Rocket local)) return;
            if (_localRocket != local)
            {
                if (_localRocket != null && !string.IsNullOrEmpty(_localBuildId)) PublishRemove(_localBuildId);
                _localRocket = local;
                _localBuildId = Guid.NewGuid().ToString("N");
                _publishTimer = PublishInterval;
            }
            _publishTimer += deltaTime;
            if (_publishTimer < PublishInterval) return;
            _publishTimer = 0f;
            if (string.IsNullOrEmpty(_localBuildId)) return;
            var loc = local.location.Value;
            PublishLocalState(new BuildStateUpdatePacket
            {
                WorldId = _client.CurrentWorldId,
                BuildId = _localBuildId,
                PosX = loc.position.x,
                PosY = loc.position.y,
                VelX = loc.velocity.x,
                VelY = loc.velocity.y,
                RotationDegrees = local.rb2d.transform.eulerAngles.z,
                AngularVelocity = local.rb2d.angularVelocity,
                PlanetAddress = loc.planet?.codeName,
                ThrottlePercent = local.throttle.throttlePercent.Value,
                WorldTime = WorldTime.main != null ? WorldTime.main.worldTime : 0,
                Tick = ++_tick
            });
            if (_tick == 1 || _tick % 15 == 0) PublishLocalBuild(SnapshotFromRocket(local, _localBuildId, _client.PlayerId));
        }

        private void ApplyTransform(RemoteBuild remote, bool immediate, float deltaTime)
        {
            var rocket = remote.Rocket;
            if (rocket == null || rocket.location?.Value?.planet == null || WorldTime.main == null) return;
            var current = rocket.location.Value;
            var pos = immediate ? remote.TargetPos : Double2.Lerp(current.position, remote.TargetPos, Mathf.Clamp01(deltaTime * 12f));
            var location = new Location(WorldTime.main.worldTime, current.planet, pos, remote.TargetVel);
            rocket.physics.SetLocationAndState(location, false);
            rocket.rb2d.simulated = false;
            rocket.rb2d.transform.eulerAngles = new Vector3(0f, 0f, immediate ? remote.TargetRotation : Mathf.LerpAngle(rocket.rb2d.transform.eulerAngles.z, remote.TargetRotation, deltaTime * 12f));
            rocket.rb2d.angularVelocity = remote.TargetAngularVelocity;
        }

        private async void PublishLocalBuild(BuildSnapshot snapshot)
        {
            await _client.SendAsync(PacketType.BuildSpawn, snapshot);
        }

        private async void PublishLocalState(BuildStateUpdatePacket update)
        {
            await _client.SendAsync(PacketType.BuildStateUpdate, update);
        }

        private async void PublishRemove(string buildId)
        {
            await _client.SendAsync(PacketType.BuildRemove, new BuildSnapshot { BuildId = buildId, OwnerPlayerId = _client.PlayerId });
        }

        private static BuildSnapshot SnapshotFromRocket(Rocket rocket, string buildId, string ownerPlayerId)
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
                PlanetAddress = loc.planet?.codeName,
                PartsBlueprintJson = JsonWrapper.ToJson(save, pretty: false),
                ControllingPlayerId = ownerPlayerId,
                LastUpdatedUtc = DateTime.UtcNow
            };
        }

        private sealed class RemoteBuild
        {
            public string BuildId;
            public string OwnerName;
            public Double2 TargetPos;
            public Double2 TargetVel;
            public float TargetRotation;
            public float TargetAngularVelocity;
            public string TargetThrottle;
            public Rocket Rocket;
            public string PendingJson;
        }
    }
}
