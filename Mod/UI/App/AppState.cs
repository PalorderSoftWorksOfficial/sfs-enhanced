using System;
using System.Collections.Generic;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Mod.UI.App
{
    internal sealed class ChatLine
    {
        public string From;
        public string Text;
        public string Scope;
        public long Sequence;
    }

    internal sealed class AppState
    {
        public bool Connected;
        public string PlayerId;
        public string ServerName;
        public string ServerHost;
        public int ServerPlayers;
        public int ServerMaxPlayers;

        public string Status = "Not connected";
        public bool StatusIsError;

        public readonly List<ServerListing> Servers = new List<ServerListing>();
        public readonly List<ChatLine> Chat = new List<ChatLine>();

        public bool LoadingServers;
        public string DirectoryError;
        public readonly List<RoomSummary> Rooms = new List<RoomSummary>();
        public bool LoadingRooms;
        public string CurrentRoomId;
        public string CurrentRoomName;

        public string CurrentWorldId;
        public string CurrentWorldName;
        public int CurrentWorldBuilds;
        public int CurrentWorldPlayers;

        public SFSEnhanced.Mod.Networking.ServerIdentityPrompt PendingIdentity;
        public string MigrationNotice;
        public string LastConnectError;
        public string LastConnectErrorCode;

        public string LocalClaimId;
        public TimeWarpVotePacket PendingVote;
        public string PendingInviteWorldId;
        public string PendingInviteFrom;

        public event Action Changed;
        public event Action ContentChanged;

        public void Notify()
        {
            Action handler = Changed;
            if (handler != null) handler();
        }

        public void NotifyContent()
        {
            Notify();
            Action handler = ContentChanged;
            if (handler != null) handler();
        }

        public void SetStatus(string text, bool isError = false)
        {
            if (!string.IsNullOrEmpty(text)) Status = text;
            StatusIsError = isError;
            Notify();
        }

        public string ServerLabel()
        {
            if (!Connected) return "Offline";
            if (!string.IsNullOrEmpty(ServerName)) return ServerName;
            return string.IsNullOrEmpty(ServerHost) ? "Connected" : ServerHost;
        }

        public string RoomLabel()
        {
            if (string.IsNullOrEmpty(CurrentWorldId)) return "No world joined";
            return string.IsNullOrEmpty(CurrentWorldName) ? CurrentWorldId : CurrentWorldName;
        }

        public void ResetConnection()
        {
            Connected = false;
            PlayerId = null;
            ServerName = null;
            ServerPlayers = 0;
            ServerMaxPlayers = 0;
            CurrentWorldId = null;
            CurrentWorldName = null;
            CurrentWorldBuilds = 0;
            CurrentWorldPlayers = 0;
            LocalClaimId = null;
            PendingVote = null;
            LoadingServers = false;
            LoadingRooms = false;
            CurrentRoomId = null;
            CurrentRoomName = null;
            DirectoryError = null;
            Rooms.Clear();
        }
    }
}
