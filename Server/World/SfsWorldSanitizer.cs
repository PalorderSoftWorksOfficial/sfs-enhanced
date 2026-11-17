using System;
using System.Collections.Generic;
using System.Linq;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Server.World
{
    public static class SfsWorldSanitizer
    {
        public const int MaxBuilds = 1500;
        public const int MaxClaims = 512;
        public const int MaxNameLength = 64;
        public const int MaxPlanetPackLength = 64;
        public const int MaxPlanetAddressLength = 64;
        public const int MaxBlueprintBytes = 512 * 1024;
        public const int MaxTrustedIds = 32;

        public static string Sanitize(WorldRecord world, string ownerPlayerId, string ownerPlayerName)
        {
            if (world == null) return "World upload was empty.";

            world.WorldId = Guid.NewGuid().ToString("N");
            world.OwnerPlayerId = ownerPlayerId;
            world.Name = TrimTo(world.Name, MaxNameLength);
            world.PlanetPackId = TrimTo(world.PlanetPackId, MaxPlanetPackLength);
            world.Whitelist = new List<string>();
            world.Builds = world.Builds ?? new List<BuildSnapshot>();
            world.Claims = world.Claims ?? new List<ClaimInfo>();

            if (world.Builds.Count > MaxBuilds) return "That world contains more builds than the server accepts.";
            if (world.Claims.Count > MaxClaims) return "That world contains more claims than the server accepts.";

            if (!IsFinite(world.WorldTime)) world.WorldTime = 0;
            if (!IsFinite(world.TimewarpMultiplier) || world.TimewarpMultiplier <= 0) world.TimewarpMultiplier = 1.0;
            world.CreatedUtc = DateTime.UtcNow;
            world.LastModifiedUtc = DateTime.UtcNow;
            world.WorldTimeUpdatedUtc = DateTime.UtcNow;

            var buildIds = new HashSet<string>(StringComparer.Ordinal);
            var builds = new List<BuildSnapshot>();
            foreach (BuildSnapshot build in world.Builds)
            {
                if (build == null) continue;
                if (string.IsNullOrWhiteSpace(build.BuildId) || build.BuildId.Length > 64) return "A build in that world has an invalid id.";
                if (!buildIds.Add(build.BuildId)) return "That world contains duplicate build ids.";
                if (!IsFinite(build.PosX) || !IsFinite(build.PosY) || !IsFinite(build.VelX) || !IsFinite(build.VelY) ||
                    !IsFinite(build.RotationDegrees) || !IsFinite(build.AngularVelocity))
                    return "A build in that world has invalid numeric state.";
                if (build.PartsBlueprintJson != null && build.PartsBlueprintJson.Length > MaxBlueprintBytes)
                    return "A build in that world is larger than the server accepts.";

                build.OwnerPlayerId = ownerPlayerId;
                build.OwnerPlayerName = ownerPlayerName;
                build.ControllingPlayerId = null;
                build.DisplayName = TrimTo(build.DisplayName, MaxNameLength);
                build.PlanetAddress = TrimTo(build.PlanetAddress, MaxPlanetAddressLength);
                build.LastUpdatedUtc = DateTime.UtcNow;
                builds.Add(build);
            }
            world.Builds = builds;

            var claimIds = new HashSet<string>(StringComparer.Ordinal);
            var buildClaims = new HashSet<string>(StringComparer.Ordinal);
            var claims = new List<ClaimInfo>();
            foreach (ClaimInfo claim in world.Claims)
            {
                if (claim == null) continue;
                claim.ClaimId = string.IsNullOrWhiteSpace(claim.ClaimId) || claim.ClaimId.Length > 64 ? Guid.NewGuid().ToString("N") : claim.ClaimId;
                if (!claimIds.Add(claim.ClaimId)) continue;

                if (claim.Shape == ClaimShape.Build)
                {
                    if (string.IsNullOrEmpty(claim.BuildId) || !buildIds.Contains(claim.BuildId)) continue;
                    if (!buildClaims.Add(claim.BuildId)) continue;
                }
                else if (claim.Shape == ClaimShape.Region)
                {
                    if (!IsFinite(claim.CenterX) || !IsFinite(claim.CenterY) || !IsFinite(claim.RadiusMeters)) continue;
                    if (claim.RadiusMeters < 1.0 || claim.RadiusMeters > 50000.0) continue;
                }
                else
                {
                    continue;
                }

                claim.OwnerPlayerId = ownerPlayerId;
                claim.OwnerPlayerName = ownerPlayerName;
                claim.TrustedPlayerIds = new List<string>();
                claims.Add(claim);
            }
            world.Claims = claims;

            return null;
        }

        public static string SafeName(string value, int maxLength)
        {
            return TrimTo(value, maxLength);
        }

        private static string TrimTo(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
