using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SFSEnhanced.Server.Rooms;
using SFSEnhanced.Server.World;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Server.Networking
{
    public partial class NetServer
    {
        private readonly object _roomCreateGate = new object();
        private readonly object _roomJoinGate = new object();

        public int RoomCount => _rooms.Count;

        private int RoomPopulation(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return 0;
            return _connections.Values.Count(c => string.Equals(c.CurrentRoomId, roomId, StringComparison.Ordinal));
        }

        private bool CanJoinRoom(RoomSummary room, string playerId)
        {
            if (room == null) return false;
            if (room.Visibility == RoomVisibility.Public) return true;
            if (string.Equals(room.HostPlayerId, playerId, StringComparison.Ordinal)) return true;
            return _friends.AreFriends(room.HostPlayerId, playerId);
        }

        private async Task HandleRoomList(ClientConnection conn, string playerId)
        {
            var rooms = _rooms.All()
                .Where(r => r.Visibility == RoomVisibility.Public
                    || string.Equals(r.HostPlayerId, playerId, StringComparison.Ordinal)
                    || _friends.AreFriends(r.HostPlayerId, playerId))
                .Select(r => RoomManager.Copy(r, RoomPopulation(r.RoomId)))
                .OrderByDescending(r => r.PlayersOnline)
                .ThenBy(r => r.Name)
                .ToList();
            await conn.SendAsync(PacketType.RoomListResponse, new RoomListResponsePacket { Rooms = rooms });
        }

        private async Task HandleRoomCreate(ClientConnection conn, string playerId, RoomCreatePacket req)
        {
            if (!TryConsume(playerId, "room-create", 6, 0.5))
            {
                await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = "You are creating rooms too quickly. Wait a moment and retry." });
                return;
            }
            if (req == null)
            {
                await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = "Empty room request." });
                return;
            }
            if (!RoomManager.TryCleanName(req.Name, out string roomName, out string nameError))
            {
                await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = nameError });
                return;
            }
            if (!RoomManager.TryCleanDescription(req.Description, out string description, out string descriptionError))
            {
                await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = descriptionError });
                return;
            }
            if (req.Capacity < RoomManager.MinCapacity || req.Capacity > RoomManager.MaxCapacity)
            {
                await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = $"Room capacity must be between {RoomManager.MinCapacity} and {RoomManager.MaxCapacity} players." });
                return;
            }
            int capacity = Math.Min(req.Capacity, Math.Max(RoomManager.MinCapacity, _config.MaxPlayers));
            if (_rooms.Count >= RoomManager.MaxRooms)
            {
                await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = "This server is hosting the maximum number of rooms. Wait for one to close." });
                return;
            }

            WorldRecord world;
            if (string.IsNullOrEmpty(req.WorldId))
            {
                world = _worlds.Create(roomName, playerId, req.Visibility == RoomVisibility.Public, null);
                world.WorldTimeUpdatedUtc = DateTime.UtcNow;
            }
            else
            {
                world = _worlds.Get(req.WorldId);
                if (world == null)
                {
                    await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = "That world no longer exists on this server." });
                    return;
                }
                if (!string.Equals(world.OwnerPlayerId, playerId, StringComparison.Ordinal))
                {
                    await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = "You can only host a room on your own world." });
                    return;
                }
                RoomSummary hosted;
                lock (_roomCreateGate) hosted = _rooms.FindByWorld(world.WorldId);
                if (hosted != null)
                {
                    await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = $"That world is already hosted by the room \"{hosted.Name}\"." });
                    return;
                }
                if (req.Visibility == RoomVisibility.Public && !world.IsPublic)
                {
                    await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = "A public room needs a public world. Make the world public first, or open a friends-only room." });
                    return;
                }
            }

            RoomSummary created;
            string createError;
            bool createdOk;
            lock (_roomCreateGate)
            {
                createdOk = _rooms.TryCreate(roomName, description, req.Visibility, capacity, playerId, conn.Account.PlayerName, world.WorldId, out created, out createError);
            }
            if (!createdOk)
            {
                await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = createError });
                return;
            }

            HandleWorldLeave(conn, playerId);
            conn.CurrentWorldId = world.WorldId;
            conn.CurrentRoomId = created.RoomId;
            _friends.SetCurrentWorld(playerId, world.WorldId);

            await conn.SendAsync(PacketType.RoomState, new RoomStatePacket
            {
                Accepted = true,
                Room = RoomManager.Copy(created, RoomPopulation(created.RoomId)),
                World = new WorldJoinAckPacket
                {
                    Accepted = true,
                    WorldId = world.WorldId,
                    Builds = world.Builds,
                    Claims = world.Claims,
                    PlayersOnline = new List<string> { conn.Account.PlayerName }
                }
            });
            Console.WriteLine($"[room] opened '{created.Name}' ({created.RoomId}) host={conn.Account.PlayerName} world={world.WorldId} capacity={created.Capacity}");
        }

        private async Task HandleRoomJoin(ClientConnection conn, string playerId, RoomJoinPacket req)
        {
            if (!TryConsume(playerId, "room-join", 8, 1.0))
            {
                await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = "You are joining rooms too quickly. Wait a moment and retry." });
                return;
            }
            RoomSummary room = _rooms.Get(req?.RoomId);
            if (room == null)
            {
                await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = "Room not found. It may have closed." });
                return;
            }
            if (!CanJoinRoom(room, playerId))
            {
                await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = "This room is friends-only. Ask the host to add you as a friend." });
                return;
            }
            int population = RoomPopulation(room.RoomId);
            bool alreadyInside = string.Equals(conn.CurrentRoomId, room.RoomId, StringComparison.Ordinal);
            if (!alreadyInside && population >= room.Capacity)
            {
                await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = $"This room is full ({population}/{room.Capacity})." });
                return;
            }
            WorldRecord world = _worlds.Get(room.WorldId);
            if (world == null)
            {
                await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = "The room's world is no longer available." });
                return;
            }
            if (!CanAccessWorld(world, playerId))
            {
                await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = "This room's world is not accessible to you." });
                return;
            }

            HandleWorldLeave(conn, playerId);
            bool admitted;
            lock (_roomJoinGate)
            {
                admitted = alreadyInside || RoomPopulation(room.RoomId) < room.Capacity;
                if (admitted)
                {
                    conn.CurrentWorldId = world.WorldId;
                    conn.CurrentRoomId = room.RoomId;
                }
            }
            if (!admitted)
            {
                await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = $"This room is full ({room.Capacity}/{room.Capacity})." });
                return;
            }
            world.WorldTimeUpdatedUtc = DateTime.UtcNow;
            _friends.SetCurrentWorld(playerId, world.WorldId);

            var playersInRoom = _connections.Values
                .Where(c => string.Equals(c.CurrentRoomId, room.RoomId, StringComparison.Ordinal) && c.Account != null)
                .Select(c => c.Account.PlayerName)
                .Distinct()
                .ToList();

            await conn.SendAsync(PacketType.RoomState, new RoomStatePacket
            {
                Accepted = true,
                Room = RoomManager.Copy(room, RoomPopulation(room.RoomId)),
                World = new WorldJoinAckPacket
                {
                    Accepted = true,
                    WorldId = world.WorldId,
                    Builds = world.Builds,
                    Claims = world.Claims,
                    PlayersOnline = playersInRoom
                }
            });
            await BroadcastToWorld(world.WorldId, PacketType.ChatMessage, new ChatMessagePacket
            {
                WorldId = world.WorldId,
                FromPlayerName = "Server",
                Message = $"{conn.Account.PlayerName} joined the world."
            }, null);
        }

        private async Task HandleRoomLeave(ClientConnection conn, string playerId)
        {
            HandleWorldLeave(conn, playerId);
            await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = true, Room = null, World = null });
        }

        private async Task HandleRoomClose(ClientConnection conn, string playerId, RoomClosePacket req)
        {
            RoomSummary room = _rooms.Get(req?.RoomId);
            if (room == null)
            {
                await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = "Room not found. It may already be closed." });
                return;
            }
            if (!string.Equals(room.HostPlayerId, playerId, StringComparison.Ordinal))
            {
                await conn.SendAsync(PacketType.RoomState, new RoomStatePacket { Accepted = false, Reason = "Only the room host can close this room." });
                return;
            }
            await CloseRoomAsync(room, "The host closed the room.");
        }

        private async Task CloseRoomAsync(RoomSummary room, string reason)
        {
            if (room == null) return;
            if (!_rooms.Remove(room.RoomId)) return;

            foreach (var entry in _connections.ToList())
            {
                ClientConnection member = entry.Value;
                if (!string.Equals(member.CurrentRoomId, room.RoomId, StringComparison.Ordinal)) continue;
                string worldId = member.CurrentWorldId;
                member.CurrentRoomId = null;
                member.CurrentWorldId = null;
                if (!string.IsNullOrEmpty(worldId))
                {
                    ReleaseControlledBuilds(worldId, entry.Key);
                    _friends.SetCurrentWorld(entry.Key, null);
                }
                try
                {
                    await member.SendAsync(PacketType.RoomClosed, new RoomClosedPacket { RoomId = room.RoomId, Reason = reason });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[room] could not notify {entry.Key}: {e.Message}");
                }
            }
            Console.WriteLine($"[room] closed '{room.Name}' ({room.RoomId}) world={room.WorldId} reason={reason}");
        }

        private void CloseRoomIfEmpty(string roomId)
        {
            RoomSummary room = _rooms.Get(roomId);
            if (room == null) return;
            if (RoomPopulation(roomId) > 0) return;
            _rooms.Remove(room.RoomId);
            Console.WriteLine($"[room] closed '{room.Name}' ({room.RoomId}) because it became empty");
        }

        public void PrintRooms()
        {
            if (_rooms.Count == 0)
            {
                Console.WriteLine("No rooms are open.");
                return;
            }
            foreach (RoomSummary room in _rooms.All().OrderBy(r => r.Name))
                Console.WriteLine($"  {room.Name} [{room.RoomId}]  host={room.HostPlayerName}  players={RoomPopulation(room.RoomId)}/{room.Capacity}  visibility={room.Visibility}  world={room.WorldId}");
        }
    }
}
