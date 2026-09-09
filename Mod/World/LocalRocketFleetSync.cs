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
    public sealed class LocalRocketFleetSync
    {
        private readonly NetClient _client;
        private readonly Dictionary<Rocket, string> _ids = new Dictionary<Rocket, string>();
        private float _timer;
        private long _tick;
        private const float Interval = 1f / 15f;

        public LocalRocketFleetSync(NetClient client)
        {
            _client = client;
        }

        public void Reset()
        {
            foreach (var id in _ids.Values.ToList())
                _ = _client.SendAsync(PacketType.BuildRemove, new BuildSnapshot { BuildId = id, OwnerPlayerId = _client.PlayerId });
            _ids.Clear();
            _timer = 0f;
            _tick = 0;
        }

        public void Tick(float deltaTime)
        {
            if (!_client.IsConnected || string.IsNullOrEmpty(_client.CurrentWorldId) || GameManager.main == null)
                return;
            _timer += deltaTime;
            if (_timer < Interval)
                return;
            _timer = 0f;
            var controlled = PlayerController.main?.player?.Value as Rocket;
            var active = new HashSet<Rocket>();
            foreach (var rocket in GameManager.main.rockets.ToList())
            {
                if (rocket == null || rocket == controlled || rocket.gameObject.GetComponent<RemoteRocketGhost>() != null)
                    continue;
                active.Add(rocket);
                if (!_ids.TryGetValue(rocket, out var id))
                {
                    id = Guid.NewGuid().ToString("N");
                    _ids[rocket] = id;
                    PublishSpawn(rocket, id);
                }
                PublishPrimary(rocket, id);
                PublishSecondary(rocket, id);
            }
            foreach (var entry in _ids.Where(x => !active.Contains(x.Key)).ToList())
            {
                _ = _client.SendAsync(PacketType.BuildRemove, new BuildSnapshot { BuildId = entry.Value, OwnerPlayerId = _client.PlayerId });
                _ids.Remove(entry.Key);
            }
            ++_tick;
        }

        private void PublishSpawn(Rocket rocket, string id)
        {
            try
            {
                var save = new RocketSave(rocket);
                var loc = rocket.location.Value;
                _ = _client.SendAsync(PacketType.BuildSpawn, new BuildSnapshot
                {
                    BuildId = id,
                    OwnerPlayerId = _client.PlayerId,
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
                    ControllingPlayerId = _client.PlayerId,
                    LastUpdatedUtc = DateTime.UtcNow
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SFSEnhanced] Fleet spawn failed: {e.Message}");
            }
        }
        private void PublishPrimary(Rocket rocket, string id)
        {
            var loc = rocket.location.Value;
            _ = _client.SendAsync(PacketType.RocketPrimaryState, new RocketPrimaryStatePacket
            {
                WorldId = _client.CurrentWorldId,
                BuildId = id,
                PosX = loc.position.x,
                PosY = loc.position.y,
                VelX = loc.velocity.x,
                VelY = loc.velocity.y,
                RotationDegrees = rocket.rb2d.transform.eulerAngles.z,
                AngularVelocity = rocket.rb2d.angularVelocity,
                PlanetAddress = loc.planet?.codeName,
                WorldTime = WorldTime.main != null ? WorldTime.main.worldTime : 0,
                Tick = _tick
            });
        }

        private void PublishSecondary(Rocket rocket, string id)
        {
            _ = _client.SendAsync(PacketType.RocketSecondaryState, new RocketSecondaryStatePacket
            {
                WorldId = _client.CurrentWorldId,
                BuildId = id,
                ThrottlePercent = rocket.throttle.throttlePercent.Value,
                Tick = _tick
            });
        }
    }
}
