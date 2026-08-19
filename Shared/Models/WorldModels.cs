using System;
using System.Collections.Generic;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Shared.Models
{
    /// <summary>Full on-disk record for one hosted world.</summary>
    public class WorldRecord
    {
        public string WorldId = Guid.NewGuid().ToString("N");
        public string Name;
        public string OwnerPlayerId;
        public bool IsPublic;
        public string PlanetPackId;
        public DateTime CreatedUtc = DateTime.UtcNow;
        public DateTime LastModifiedUtc = DateTime.UtcNow;

        /// <summary>Every build (rocket/base/rover/station) currently in this world.</summary>
        public List<BuildSnapshot> Builds = new List<BuildSnapshot>();

        /// <summary>Every active claim in this world.</summary>
        public List<ClaimInfo> Claims = new List<ClaimInfo>();

        /// <summary>Player ids allowed in (empty = anyone with the invite/public listing can join).</summary>
        public List<string> Whitelist = new List<string>();
    }

    public class PlayerAccount
    {
        public string PlayerId = Guid.NewGuid().ToString("N");
        public string PlayerName;
        public string AuthTokenHash;
        public DateTime FirstSeenUtc = DateTime.UtcNow;
        public DateTime LastSeenUtc = DateTime.UtcNow;
        public List<string> FriendPlayerIds = new List<string>();
        public List<string> IncomingFriendRequests = new List<string>();
        public List<string> OutgoingFriendRequests = new List<string>();
    }
}
