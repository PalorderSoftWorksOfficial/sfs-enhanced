using System;
using System.Collections.Generic;

namespace SFSEnhanced.Shared.Models
{
    public class ServerListing
    {
        public string ServerId;
        public string Name;
        public string Host;
        public int Port;
        public string Region;
        public string GameVersion;
        public string ModVersion;
        public string Motd;
        public int OnlinePlayers;
        public int MaxPlayers;
        public bool PasswordProtected;
        public DateTime LastHeartbeatUtc;
    }

    public class ServerRegisterRequest
    {
        public string Name;
        public string Host;
        public int Port;
        public string Region;
        public string GameVersion;
        public string ModVersion;
        public string Motd;
        public int MaxPlayers;
        public bool PasswordProtected;
    }

    public class ServerRegisterResponse
    {
        public string ServerId;
        public string HeartbeatToken;
    }

    public class ServerHeartbeatRequest
    {
        public string ServerId;
        public string HeartbeatToken;
        public int OnlinePlayers;
        public int MaxPlayers;
        public string Motd;
    }

    public class ServerDirectoryResponse
    {
        public List<ServerListing> Servers = new List<ServerListing>();
    }
}
