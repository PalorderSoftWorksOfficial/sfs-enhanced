using System;

namespace SFSEnhanced.Shared.Models
{
    public enum RoomVisibility
    {
        Public,
        Friends
    }

    public class RoomSummary
    {
        public string RoomId;
        public string Name;
        public string Description;
        public string HostPlayerId;
        public string HostPlayerName;
        public string WorldId;
        public RoomVisibility Visibility;
        public int Capacity;
        public int PlayersOnline;
        public DateTime CreatedUtc;
    }
}
