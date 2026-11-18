using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFSEnhanced.Shared.Models;

namespace SFSEnhanced.Server.Rooms
{
    public class RoomManager
    {
        public const int MinCapacity = 2;
        public const int MaxCapacity = 64;
        public const int MaxNameLength = 48;
        public const int MaxDescriptionLength = 160;
        public const int MaxRooms = 128;

        private readonly ConcurrentDictionary<string, RoomSummary> _rooms = new ConcurrentDictionary<string, RoomSummary>();

        public int Count => _rooms.Count;

        public IReadOnlyList<RoomSummary> All() => _rooms.Values.ToList();

        public RoomSummary Get(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return null;
            return _rooms.TryGetValue(roomId, out RoomSummary room) ? room : null;
        }

        public RoomSummary FindByWorld(string worldId)
        {
            if (string.IsNullOrEmpty(worldId)) return null;
            return _rooms.Values.FirstOrDefault(r => string.Equals(r.WorldId, worldId, StringComparison.Ordinal));
        }

        public bool Remove(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return false;
            return _rooms.TryRemove(roomId, out _);
        }

        public bool TryCreate(string name, string description, RoomVisibility visibility, int capacity, string hostPlayerId, string hostPlayerName, string worldId, out RoomSummary room, out string error)
        {
            room = null;
            error = null;

            if (_rooms.Count >= MaxRooms)
            {
                error = "This server is hosting the maximum number of rooms. Wait for one to close.";
                return false;
            }
            if (!TryCleanName(name, out string cleanedName, out error)) return false;
            if (!TryCleanDescription(description, out string cleanedDescription, out error)) return false;
            if (visibility != RoomVisibility.Public && visibility != RoomVisibility.Friends)
            {
                error = "Unknown room visibility.";
                return false;
            }
            if (capacity < MinCapacity || capacity > MaxCapacity)
            {
                error = $"Room capacity must be between {MinCapacity} and {MaxCapacity} players.";
                return false;
            }
            if (string.IsNullOrEmpty(worldId))
            {
                error = "A room needs a world.";
                return false;
            }

            room = new RoomSummary
            {
                RoomId = Guid.NewGuid().ToString("N"),
                Name = cleanedName,
                Description = cleanedDescription,
                HostPlayerId = hostPlayerId,
                HostPlayerName = hostPlayerName,
                WorldId = worldId,
                Visibility = visibility,
                Capacity = capacity,
                PlayersOnline = 0,
                CreatedUtc = DateTime.UtcNow
            };
            _rooms[room.RoomId] = room;
            return true;
        }

        public static bool TryCleanName(string input, out string cleaned, out string error)
            => TryClean(input, MaxNameLength, required: true, out cleaned, out error);

        public static bool TryCleanDescription(string input, out string cleaned, out string error)
            => TryClean(input, MaxDescriptionLength, required: false, out cleaned, out error);

        private static bool TryClean(string input, int maxLength, bool required, out string cleaned, out string error)
        {
            cleaned = null;
            error = null;
            if (string.IsNullOrWhiteSpace(input))
            {
                if (!required) { cleaned = string.Empty; return true; }
                error = "A room name is required.";
                return false;
            }
            if (input.IndexOf('<') >= 0 || input.IndexOf('>') >= 0)
            {
                error = "Room text cannot contain < or >.";
                return false;
            }

            var builder = new StringBuilder(input.Length);
            bool lastWasSpace = false;
            foreach (char c in input)
            {
                if (c >= '\u200B' && c <= '\u200F') continue;
                if (c >= '\u202A' && c <= '\u202E') continue;
                if (c >= '\u2066' && c <= '\u2069') continue;
                if (c == '\uFEFF') continue;
                if (char.IsControl(c) || char.IsWhiteSpace(c))
                {
                    if (builder.Length > 0 && !lastWasSpace)
                    {
                        builder.Append(' ');
                        lastWasSpace = true;
                    }
                    continue;
                }
                builder.Append(c);
                lastWasSpace = false;
            }

            string result = builder.ToString().Trim();
            if (result.Length == 0)
            {
                if (!required) { cleaned = string.Empty; return true; }
                error = "A room name is required.";
                return false;
            }
            if (result.Length > maxLength)
            {
                error = $"That text is limited to {maxLength} characters.";
                return false;
            }
            cleaned = result;
            return true;
        }

        public static RoomSummary Copy(RoomSummary room, int playersOnline)
        {
            if (room == null) return null;
            return new RoomSummary
            {
                RoomId = room.RoomId,
                Name = room.Name,
                Description = room.Description,
                HostPlayerId = room.HostPlayerId,
                HostPlayerName = room.HostPlayerName,
                WorldId = room.WorldId,
                Visibility = room.Visibility,
                Capacity = room.Capacity,
                PlayersOnline = playersOnline,
                CreatedUtc = room.CreatedUtc
            };
        }
    }
}
