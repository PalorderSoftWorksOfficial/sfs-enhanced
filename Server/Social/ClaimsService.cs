using System;
using System.Linq;
using SFSEnhanced.Server.World;
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

        public ClaimInfo Create(string worldId, string ownerPlayerId, string ownerPlayerName, ClaimCreatePacket req)
        {
            var world = _worlds.Get(worldId);
            if (world == null) return null;

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
            if (world == null) return true;

            var build = world.Builds.FirstOrDefault(b => b.BuildId == buildId);

            foreach (var claim in world.Claims)
            {
                bool covers = (claim.Shape == ClaimShape.Build && claim.BuildId == buildId)
                    || (claim.Shape == ClaimShape.Region && build != null && WithinRadius(claim, build));

                if (!covers) continue;
                if (claim.OwnerPlayerId == playerId) return true;
                if (claim.TrustedPlayerIds.Contains(playerId)) return true;
                return false; // covered by a claim, and the requester isn't owner/trusted
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

        private static bool WithinRadius(ClaimInfo claim, BuildSnapshot build)
        {
            double dx = build.PosX - claim.CenterX;
            double dy = build.PosY - claim.CenterY;
            return (dx * dx + dy * dy) <= claim.RadiusMeters * claim.RadiusMeters;
        }
    }
}
