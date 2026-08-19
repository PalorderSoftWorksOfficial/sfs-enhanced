using System;
using System.Collections.Generic;

namespace SFSEnhanced.Shared.Protocol
{
    // ---- Connection ----

    public class HelloPacket
    {
        public string ProtocolVersion = "1.0";
        public string PlayerName;
        public string AuthToken;      // opaque token from a prior session, or null for a first-time login
        public string ClientModVersion;
    }

    public class HelloAckPacket
    {
        public bool Accepted;
        public string RejectReason;
        public string PlayerId;
        public string AuthToken;      // issued token, save it and send back next time
    }

    public class DisconnectPacket
    {
        public string Reason;
    }

    // ---- Server / world discovery ----

    public class ServerInfoResponsePacket
    {
        public string ServerName;
        public string Motd;
        public int OnlinePlayers;
        public int MaxPlayers;
        public string ServerVersion;
    }

    public class WorldListResponsePacket
    {
        public List<WorldSummary> Worlds = new List<WorldSummary>();
    }

    public class WorldSummary
    {
        public string WorldId;
        public string Name;
        public string OwnerName;
        public bool IsPublic;
        public int PlayersOnline;
        public int BuildCount;
        public DateTime LastModifiedUtc;
    }

    // ---- World lifecycle ----

    public class WorldCreatePacket
    {
        public string Name;
        public bool IsPublic;
        public string PlanetPackId; // null = stock solar system
    }

    public class WorldJoinPacket
    {
        public string WorldId;
    }

    public class WorldJoinAckPacket
    {
        public bool Accepted;
        public string RejectReason;
        public string WorldId;
        public List<BuildSnapshot> Builds = new List<BuildSnapshot>();
        public List<ClaimInfo> Claims = new List<ClaimInfo>();
        public List<string> PlayersOnline = new List<string>();
    }

    public class WorldUploadChunkPacket
    {
        public string UploadId;
        public int ChunkIndex;
        public int TotalChunks;
        public string Base64Data;
        public string WorldName;   // set on chunk 0 only
        public bool IsPublic;      // set on chunk 0 only
    }

    public class WorldDownloadChunkPacket
    {
        public string WorldId;
        public int ChunkIndex;
        public int TotalChunks;
        public string Base64Data;
    }

    // ---- Builds ----

    /// <summary>
    /// A single independently-ownable thing sitting in the world: a rocket, a base,
    /// a rover, a station module chain — anything the player builder produced.
    /// This is the core "multiple builds in one world" unit.
    /// </summary>
    public class BuildSnapshot
    {
        public string BuildId;
        public string OwnerPlayerId;
        public string OwnerPlayerName;
        public string DisplayName;
        public BuildKind Kind;
        public double PosX, PosY;           // planet-relative meters (SFS Location.position)
        public double VelX, VelY;
        public double RotationDegrees;
        public double AngularVelocity;
        public string PlanetAddress;        // SFS planet codeName, e.g. "Earth"
        public string PartsBlueprintJson;   // RocketSave JSON via JsonWrapper
        public string ControllingPlayerId;  // null if uncontrolled/idle
        public DateTime LastUpdatedUtc;
    }

    public enum BuildKind
    {
        Rocket,
        Base,
        Rover,
        Station
    }

    public class BuildStateUpdatePacket
    {
        public string WorldId;
        public string BuildId;
        public double PosX, PosY;
        public double VelX, VelY;
        public double RotationDegrees;
        public double AngularVelocity;
        public string PlanetAddress;
        public double? ThrottlePercent;
        public int? StagingIndex;
    }

    public class BuildControlRequestPacket
    {
        public string WorldId;
        public string BuildId;
    }

    public class BuildControlGrantPacket
    {
        public string BuildId;
        public bool Granted;
        public string ControllingPlayerId;
        public string DenyReason;
    }

    // ---- Time warp ----

    public class TimeWarpRequestPacket
    {
        public string WorldId;
        public double RequestedMultiplier;
    }

    public class TimeWarpStatePacket
    {
        public string WorldId;
        public double ActualMultiplier;
        public bool LockedByProximity; // true when someone else is too close to safely warp
    }

    // ---- Social ----

    public class FriendRequestPacket
    {
        public string TargetPlayerName;
    }

    public class FriendRequestResponsePacket
    {
        public string FromPlayerId;
        public string FromPlayerName;
        public bool Accepted;
    }

    public class FriendListResponsePacket
    {
        public List<FriendInfo> Friends = new List<FriendInfo>();
        public List<FriendInfo> PendingIncoming = new List<FriendInfo>();
        public List<FriendInfo> PendingOutgoing = new List<FriendInfo>();
    }

    public class FriendInfo
    {
        public string PlayerId;
        public string PlayerName;
        public bool Online;
        public string CurrentWorldId;
    }

    public class FriendInviteToWorldPacket
    {
        public string TargetPlayerName;
        public string WorldId;
    }

    // ---- Claims ----

    /// <summary>
    /// Ownership protection over either a specific build, or a circular region
    /// (e.g. "my landing site"). Non-owners (and non-trusted friends) can't
    /// interact with what falls inside.
    /// </summary>
    public class ClaimInfo
    {
        public string ClaimId;
        public string OwnerPlayerId;
        public string OwnerPlayerName;
        public ClaimShape Shape;
        public string BuildId;       // set when Shape == Build
        public double CenterX, CenterY; // set when Shape == Region
        public double RadiusMeters;     // set when Shape == Region
        public List<string> TrustedPlayerIds = new List<string>(); // friends allowed to build/interact inside
    }

    public enum ClaimShape { Build, Region }

    public class ClaimCreatePacket
    {
        public string WorldId;
        public ClaimShape Shape;
        public string BuildId;
        public double CenterX, CenterY;
        public double RadiusMeters;
    }

    public class ClaimDeniedPacket
    {
        public string ClaimId;
        public string OwnerPlayerName;
        public string Reason;
    }

    // ---- Chat ----

    public class ChatMessagePacket
    {
        public string WorldId;       // null = server-wide lobby chat
        public string FromPlayerName;
        public string Message;
        public DateTime SentUtc = DateTime.UtcNow;
    }

    // ---- Errors ----

    public class ErrorPacket
    {
        public string Message;
    }
}
