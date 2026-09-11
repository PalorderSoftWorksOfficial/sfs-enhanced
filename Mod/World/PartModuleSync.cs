using System;
using System.Collections.Generic;
using SFS.Parts.Modules;
using SFS.World;
using SFSEnhanced.Mod.Networking;
using SFSEnhanced.Shared.Protocol;
using UnityEngine;

namespace SFSEnhanced.Mod.World
{
    internal static class PartModuleSync
    {
        private const float ChangeEpsilon = 0.01f;

        public static void Publish(NetClient client, string worldId, string buildId, Rocket rocket, long tick, Dictionary<string, float> cache)
        {
            if (client == null || string.IsNullOrEmpty(worldId) || string.IsNullOrEmpty(buildId) || rocket == null || rocket.partHolder == null) return;
            var parachutes = rocket.partHolder.GetModules<ParachuteModule>();
            for (int i = 0; i < parachutes.Length; i++)
            {
                if (parachutes[i] == null || parachutes[i].targetState == null) continue;
                PublishOne(client, worldId, buildId, cache, tick, "parachute", i, parachutes[i].targetState.Value);
            }
            var boosters = rocket.partHolder.GetModules<BoosterModule>();
            for (int i = 0; i < boosters.Length; i++)
            {
                if (boosters[i] == null || boosters[i].fuelPercent == null) continue;
                PublishOne(client, worldId, buildId, cache, tick, "fuel", i, boosters[i].fuelPercent.Value);
            }
        }

        private static void PublishOne(NetClient client, string worldId, string buildId, Dictionary<string, float> cache, long tick, string moduleType, int index, float value)
        {
            string key = buildId + ":" + moduleType + ":" + index;
            if (cache.TryGetValue(key, out float previous) && Mathf.Abs(previous - value) <= ChangeEpsilon) return;
            cache[key] = value;
            _ = client.SendAsync(PacketType.PartModuleState, new PartModuleStatePacket
            {
                WorldId = worldId,
                BuildId = buildId,
                PartId = moduleType + ":" + index,
                ModuleType = moduleType,
                Enabled = true,
                Value = value,
                Tick = tick
            });
        }

        public static void Apply(Rocket rocket, PartModuleStatePacket state)
        {
            if (rocket == null || rocket.partHolder == null || state == null || string.IsNullOrEmpty(state.PartId)) return;
            int separator = state.PartId.LastIndexOf(':');
            if (separator < 0 || !int.TryParse(state.PartId.Substring(separator + 1), out int index) || index < 0) return;
            try
            {
                if (state.ModuleType == "parachute")
                {
                    var parachutes = rocket.partHolder.GetModules<ParachuteModule>();
                    if (index < parachutes.Length && parachutes[index] != null && parachutes[index].targetState != null)
                        parachutes[index].targetState.Value = Mathf.Clamp01((float)state.Value);
                }
                else if (state.ModuleType == "fuel")
                {
                    var boosters = rocket.partHolder.GetModules<BoosterModule>();
                    if (index < boosters.Length && boosters[index] != null && boosters[index].fuelPercent != null)
                        boosters[index].fuelPercent.Value = Mathf.Clamp01((float)state.Value);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SFSEnhanced] Part module apply failed ({state.ModuleType}): {e.Message}");
            }
        }
    }
}
