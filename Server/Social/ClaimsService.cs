using System;
using System.Linq;
using SFSEnhanced.Server.World;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Server.Social
{
    /// <summary>
    /// A claim protects either one build, or a circular region of space, from
    /// interaction (docking-and-stealing, part removal, demolition) by anyone
    /// except the owner and players the owner has explicitly trusted (typically
    /// their friends). This is what makes "shared public worlds" survivable —
    /// see docs/FEATURE_RESEARCH.md, item 5.
    /// </summary>
    public class ClaimsService
    {
        private readonly WorldManager _worlds;

        public ClaimsService(WorldManager worlds) => _worlds = worlds;

        public const int MaxClaimsPerWorld = 512;
        public const int MaxRegionClaimsPerPlayer = 32;
        public const double MinRegionRadiusMeters = 1.0;
        public const double MaxRegionRadiusMeters = 50000.0;
        public const double MaxRegionCenterMeters = 1000000.0;

        public ClaimInfo Create(string worldId, string ownerPlayerId, string ownerPlayerName, ClaimCreatePacket req, out string error)
        {
            error = null;
            var world = _worlds.Get(worldId);
            if (world == null || req == null)
            {
                error = "That world does not exist.";
                return null;
            }
            if (world.Claims.Count >= MaxClaimsPerWorld)
            {
                error = "This world already has the maximum number of claims.";
                return null;
            }

            if (req.Shape == ClaimShape.Region)
            {
                bool finite = !double.IsNaN(req.CenterX) && !double.IsInfinity(req.CenterX) &&
                              !double.IsNaN(req.CenterY) && !double.IsInfinity(req.CenterY) &&
                              !double.IsNaN(req.RadiusMeters) && !double.IsInfinity(req.RadiusMeters);
                if (!finite)
                {
                    error = "A region claim needs a finite centre and radius.";
                    return null;
                }
                if (Math.Abs(req.CenterX) > MaxRegionCenterMeters || Math.Abs(req.CenterY) > MaxRegionCenterMeters)
                {
                    error = "That region is outside the map bounds this server accepts.";
                    return null;
                }
                if (req.RadiusMeters < MinRegionRadiusMeters || req.RadiusMeters > MaxRegionRadiusMeters)
                {
                    error = $"A region radius must be between {MinRegionRadiusMeters:0} and {MaxRegionRadiusMeters:0} metres.";
                    return null;
                }
                int owned = world.Claims.Count(c => c.Shape == ClaimShape.Region && c.OwnerPlayerId == ownerPlayerId);
                if (owned >= MaxRegionClaimsPerPlayer)
                {
                    error = $"You already hold {MaxRegionClaimsPerPlayer} region claims in this world.";
                    return null;
                }
            }
            else if (req.Shape == ClaimShape.Build)
            {
                if (string.IsNullOrEmpty(req.BuildId))
                {
                    error = "A build claim needs a build id.";
                    return null;
                }
                var build = world.Builds.FirstOrDefault(b => b.BuildId == req.BuildId);
                if (build == null || build.OwnerPlayerId != ownerPlayerId)
                {
                    error = "You can only claim your own build.";
                    return null;
                }
            }
            else
            {
                error = "Unknown claim type.";
                return null;
            }

            if (req.Shape == ClaimShape.Build)
            {
                var existing = world.Claims.FirstOrDefault(c => c.Shape == ClaimShape.Build && c.BuildId == req.BuildId);
                if (existing != null) return existing;
            }
            else
            {
                var identical = world.Claims.FirstOrDefault(c =>
                    c.Shape == ClaimShape.Region &&
                    string.Equals(c.OwnerPlayerId, ownerPlayerId, StringComparison.Ordinal) &&
                    c.CenterX == req.CenterX && c.CenterY == req.CenterY && c.RadiusMeters == req.RadiusMeters);
                if (identical != null) return identical;

                error = FindRegionConflict(world, ownerPlayerId, req);
                if (error != null) return null;
            }

            var claim = new ClaimInfo
            {
                ClaimId = Guid.NewGuid().ToString("N"),
                OwnerPlayerId = ownerPlayerId,
                OwnerPlayerName = ownerPlayerName,
                Shape = req.Shape,
                BuildId = req.BuildId,
                CenterX = req.CenterX,
                CenterY = req.CenterY,
                RadiusMeters = req.RadiusMeters,
            };
            world.Claims.Add(claim);
            _worlds.Persist(worldId);
            return claim;
        }

        public bool RemoveForBuild(string worldId, string buildId, string requestingPlayerId)
        {
            var world = _worlds.Get(worldId);
            var claims = world?.Claims.Where(c => c.Shape == ClaimShape.Build && c.BuildId == buildId && c.OwnerPlayerId == requestingPlayerId).ToList();
            if (claims == null || claims.Count == 0) return false;
            foreach (var claim in claims) world.Claims.Remove(claim);
            _worlds.Persist(worldId);
            return true;
        }

        public bool Remove(string worldId, string claimId, string requestingPlayerId)
        {
            var world = _worlds.Get(worldId);
            var claim = world?.Claims.FirstOrDefault(c => c.ClaimId == claimId);
            if (claim == null || claim.OwnerPlayerId != requestingPlayerId) return false;
            world.Claims.Remove(claim);
            _worlds.Persist(worldId);
            return true;
        }

        /// <summary>True if playerId is free to act on this build (not owned/claimed by someone else,
        /// or claimed but the actor is the owner or a trusted friend).</summary>
        public bool CanInteract(string worldId, string buildId, string playerId)
        {
            var world = _worlds.Get(worldId);
            if (world == null) return false;

            var build = world.Builds.FirstOrDefault(b => b.BuildId == buildId);

            foreach (var claim in world.Claims)
            {
                if (claim == null) continue;
                bool covers = (claim.Shape == ClaimShape.Build && claim.BuildId == buildId)
                    || (claim.Shape == ClaimShape.Region && build != null && WithinRadius(claim, build));

                if (!covers) continue;
                if (claim.OwnerPlayerId == playerId) return true;
                if (claim.TrustedPlayerIds != null && claim.TrustedPlayerIds.Contains(playerId)) return true;
                return false;
            }
            return true; // unclaimed
        }

        public ClaimInfo FindCovering(string worldId, string buildId)
        {
            var world = _worlds.Get(worldId);
            if (world == null) return null;
            var build = world.Builds.FirstOrDefault(b => b.BuildId == buildId);
            return world.Claims.FirstOrDefault(c =>
                (c.Shape == ClaimShape.Build && c.BuildId == buildId) ||
                (c.Shape == ClaimShape.Region && build != null && WithinRadius(c, build)));
        }

        private static string FindRegionConflict(WorldRecord world, string ownerPlayerId, ClaimCreatePacket req)
        {
            foreach (var claim in world.Claims)
            {
                if (claim == null || claim.Shape != ClaimShape.Region) continue;
                if (string.Equals(claim.OwnerPlayerId, ownerPlayerId, StringComparison.Ordinal)) continue;
                if (!RegionsOverlap(claim, req)) continue;
                return $"That area overlaps a region claimed by {claim.OwnerPlayerName}. Move or shrink the claim until it no longer overlaps theirs.";
            }

            foreach (var build in world.Builds)
            {
                if (build == null || string.Equals(build.OwnerPlayerId, ownerPlayerId, StringComparison.Ordinal)) continue;
                double dx = build.PosX - req.CenterX;
                double dy = build.PosY - req.CenterY;
                if (dx * dx + dy * dy > req.RadiusMeters * req.RadiusMeters) continue;
                return $"That area contains a build owned by {build.OwnerPlayerName ?? "another player"}. Region claims cannot cover another player's build.";
            }

            return null;
        }

        private static bool RegionsOverlap(ClaimInfo existing, ClaimCreatePacket req)
        {
            double dx = existing.CenterX - req.CenterX;
            double dy = existing.CenterY - req.CenterY;
            double sum = existing.RadiusMeters + req.RadiusMeters;
            return dx * dx + dy * dy < sum * sum;
        }

        private static bool WithinRadius(ClaimInfo claim, BuildSnapshot build)
        {
            double dx = build.PosX - claim.CenterX;
            double dy = build.PosY - claim.CenterY;
            return (dx * dx + dy * dy) <= claim.RadiusMeters * claim.RadiusMeters;
        }
    }
}
