namespace SFSEnhanced.Shared.Protocol
{
    /// <summary>
    /// Every message type the client &lt;-&gt; server protocol supports.
    /// Keep this in sync on both ends — it's the wire contract.
    /// </summary>
    public enum PacketType : byte
    {
        // --- Connection lifecycle ---
        Hello = 0,              // C->S: client version + player name handshake
        HelloAck = 1,           // S->C: accepted, assigns PlayerId
        Disconnect = 2,         // either way: clean disconnect with reason
        Ping = 3,
        Pong = 4,

        // --- Server / world discovery ---
        ServerInfoRequest = 10, // C->S
        ServerInfoResponse = 11,// S->C: name, motd, player count, world list
        WorldListRequest = 12,
        WorldListResponse = 13,

        // --- World lifecycle ---
        WorldCreate = 20,       // C->S: create a new world on this server
        WorldUpload = 21,       // C->S: upload a full world file (chunked, see WorldUploadChunk)
        WorldUploadChunk = 22,
        WorldDownloadRequest = 23,
        WorldDownloadChunk = 24,
        WorldJoin = 25,         // C->S: join an existing world
        WorldJoinAck = 26,      // S->C: full world snapshot (builds, claims, players)
        WorldLeave = 27,

        // --- Live build/rocket sync ---
        BuildSpawn = 30,        // a new build (rocket or base) entered the world
        BuildRemove = 31,
        BuildStateUpdate = 32,  // position/velocity/rotation/throttle/staging deltas
        BuildOwnershipTransfer = 33,
        BuildControlRequest = 34, // "let me pilot this" — server arbitrates conflicts
        BuildControlGrant = 35,

        // --- Time control ---
        TimeWarpRequest = 40,   // client asking to warp; server may deny if others are nearby
        TimeWarpState = 41,     // server broadcasting authoritative warp state

        // --- Social ---
        FriendRequest = 50,
        FriendRequestResponse = 51,
        FriendListRequest = 52,
        FriendListResponse = 53,
        FriendInviteToWorld = 54,

        // --- Claims ---
        ClaimCreate = 60,
        ClaimRemove = 61,
        ClaimListRequest = 62,
        ClaimListResponse = 63,
        ClaimDenied = 64,       // S->C: your action touched a claim you don't own

        // --- Chat ---
        ChatMessage = 70,

        Error = 255,
    }
}
