using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFSEnhanced.Server.World;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Server.Networking
{
    public partial class NetServer
    {
        private async Task HandleWorldCreate(ClientConnection conn, string playerId, WorldCreatePacket req)
        {
            if (string.IsNullOrWhiteSpace(req?.Name))
            {
                await conn.SendAsync(PacketType.WorldJoinAck, new WorldJoinAckPacket { Accepted = false, RejectReason = "World name is required." });
                return;
            }
            HandleWorldLeave(conn, playerId);
            var world = _worlds.Create(req.Name.Trim(), playerId, req.IsPublic, req.PlanetPackId);
            conn.CurrentWorldId = world.WorldId;
            world.WorldTimeUpdatedUtc = DateTime.UtcNow;
            _friends.SetCurrentWorld(playerId, world.WorldId);
            await conn.SendAsync(PacketType.WorldJoinAck, new WorldJoinAckPacket { Accepted = true, WorldId = world.WorldId, Builds = world.Builds, Claims = world.Claims, PlayersOnline = new List<string> { conn.Account.PlayerName } });
        }

        private async Task HandleWorldJoin(ClientConnection conn, string playerId, WorldJoinPacket req)
        {
            var world = req == null ? null : _worlds.Get(req.WorldId);
            if (world == null)
            {
                await conn.SendAsync(PacketType.WorldJoinAck, new WorldJoinAckPacket { Accepted = false, RejectReason = "World not found." });
                return;
            }
            if (!world.IsPublic && world.OwnerPlayerId != playerId && !_friends.AreFriends(world.OwnerPlayerId, playerId) && !world.Whitelist.Contains(playerId))
            {
                await conn.SendAsync(PacketType.WorldJoinAck, new WorldJoinAckPacket { Accepted = false, RejectReason = "This world is private." });
                return;
            }
            HandleWorldLeave(conn, playerId);
            conn.CurrentWorldId = world.WorldId;
            world.WorldTimeUpdatedUtc = DateTime.UtcNow;
            _friends.SetCurrentWorld(playerId, world.WorldId);
            var playersInWorld = _connections.Values.Where(c => c.CurrentWorldId == world.WorldId && c.Account != null).Select(c => c.Account.PlayerName).Distinct().ToList();
            await conn.SendAsync(PacketType.WorldJoinAck, new WorldJoinAckPacket { Accepted = true, WorldId = world.WorldId, Builds = world.Builds, Claims = world.Claims, PlayersOnline = playersInWorld });
            await BroadcastToWorld(world.WorldId, PacketType.ChatMessage, new ChatMessagePacket { WorldId = world.WorldId, FromPlayerName = "Server", Message = $"{conn.Account.PlayerName} joined the world." }, null);
        }

        private void HandleWorldLeave(ClientConnection conn, string playerId)
        {
            conn.CurrentWorldId = null;
            if (playerId != null) _friends.SetCurrentWorld(playerId, null);
        }

        private async Task HandleWorldUploadChunk(ClientConnection conn, string playerId, WorldUploadChunkPacket chunk)
        {
            if (chunk == null || string.IsNullOrEmpty(chunk.UploadId) || chunk.TotalChunks <= 0 || chunk.ChunkIndex < 0 || chunk.ChunkIndex >= chunk.TotalChunks || string.IsNullOrEmpty(chunk.Base64Data))
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "Invalid upload chunk." });
                return;
            }
            string uploadKey = playerId + ":" + chunk.UploadId;
            var buffer = _pendingUploads.GetOrAdd(uploadKey, _ => new List<string>(new string[chunk.TotalChunks]));
            bool invalid;
            lock (buffer) invalid = buffer.Count != chunk.TotalChunks;
            if (invalid)
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "Upload chunk count mismatch." });
                return;
            }
            lock (buffer) buffer[chunk.ChunkIndex] = chunk.Base64Data;
            bool complete;
            lock (buffer) complete = buffer.All(b => b != null);
            if (!complete) return;
            string fullJson;
            lock (buffer) fullJson = Encoding.UTF8.GetString(Convert.FromBase64String(string.Concat(buffer)));
            WorldRecord uploaded;
            try { uploaded = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldRecord>(fullJson); }
            catch { await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "World upload is not valid JSON." }); return; }
            if (uploaded == null)
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "World upload was empty." });
                return;
            }
            var created = _worlds.Create(chunk.WorldName ?? uploaded.Name ?? "Uploaded World", playerId, chunk.IsPublic, uploaded.PlanetPackId);
            created.Builds = uploaded.Builds ?? new List<BuildSnapshot>();
            created.Claims = uploaded.Claims ?? new List<ClaimInfo>();
            foreach (var build in created.Builds)
            {
                if (build == null) continue;
                build.OwnerPlayerId = playerId;
                build.OwnerPlayerName = conn.Account.PlayerName;
            }
            foreach (var claim in created.Claims)
            {
                if (claim == null) continue;
                claim.OwnerPlayerId = playerId;
                claim.OwnerPlayerName = conn.Account.PlayerName;
            }
            _worlds.Persist(created.WorldId);
            _pendingUploads.TryRemove(uploadKey, out _);
            await conn.SendAsync(PacketType.WorldJoinAck, new WorldJoinAckPacket { Accepted = true, WorldId = created.WorldId, Builds = created.Builds, Claims = created.Claims });
        }

        private async Task HandleWorldDownloadRequest(ClientConnection conn, WorldJoinPacket req)
        {
            var world = req == null ? null : _worlds.Get(req.WorldId);
            if (world == null)
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "World not found." });
                return;
            }
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(world);
            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            const int chunkSize = 32 * 1024;
            int totalChunks = Math.Max(1, (int)Math.Ceiling(base64.Length / (double)chunkSize));
            for (int i = 0; i < totalChunks; i++)
            {
                string piece = base64.Substring(i * chunkSize, Math.Min(chunkSize, base64.Length - i * chunkSize));
                await conn.SendAsync(PacketType.WorldDownloadChunk, new WorldDownloadChunkPacket { WorldId = world.WorldId, ChunkIndex = i, TotalChunks = totalChunks, Base64Data = piece });
            }
        }

        private async Task HandleRocketPrimaryState(ClientConnection conn, string playerId, RocketPrimaryStatePacket state)
        {
            if (state == null || conn.CurrentWorldId == null || state.WorldId != conn.CurrentWorldId) return;
            var update = new BuildStateUpdatePacket
            {
                WorldId = state.WorldId,
                BuildId = state.BuildId,
                PosX = state.PosX,
                PosY = state.PosY,
                VelX = state.VelX,
                VelY = state.VelY,
                RotationDegrees = state.RotationDegrees,
                AngularVelocity = state.AngularVelocity,
                PlanetAddress = state.PlanetAddress,
                WorldTime = state.WorldTime,
                Tick = state.Tick
            };
            if (!IsFinite(state.PosX) || !IsFinite(state.PosY) || !IsFinite(state.VelX) || !IsFinite(state.VelY) || !IsFinite(state.RotationDegrees) || !IsFinite(state.AngularVelocity) || !IsFinite(state.WorldTime))
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "Rocket state contains invalid numeric values." });
                return;
            }
            var build = _worlds.FindBuild(conn.CurrentWorldId, state.BuildId);
            if (build == null || (build.OwnerPlayerId != playerId && build.ControllingPlayerId != playerId)) return;
            if (!_claims.CanInteract(conn.CurrentWorldId, state.BuildId, playerId)) return;
            _worlds.ApplyStateUpdate(conn.CurrentWorldId, update);
            await BroadcastToWorld(conn.CurrentWorldId, PacketType.RocketPrimaryState, state, playerId);
        }

        private async Task HandleRocketSecondaryState(ClientConnection conn, string playerId, RocketSecondaryStatePacket state)
        {
            if (state == null || conn.CurrentWorldId == null || state.WorldId != conn.CurrentWorldId) return;
            var build = _worlds.FindBuild(conn.CurrentWorldId, state.BuildId);
            if (build == null || (build.OwnerPlayerId != playerId && build.ControllingPlayerId != playerId)) return;
            await BroadcastToWorld(conn.CurrentWorldId, PacketType.RocketSecondaryState, state, playerId);
        }

        private async Task HandlePartModuleState(ClientConnection conn, string playerId, PartModuleStatePacket state)
        {
            if (state == null || conn.CurrentWorldId == null || state.WorldId != conn.CurrentWorldId) return;
            if (!IsFinite(state.Value))
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "Part module state contains an invalid value." });
                return;
            }
            var build = _worlds.FindBuild(conn.CurrentWorldId, state.BuildId);
            if (build == null || (build.OwnerPlayerId != playerId && build.ControllingPlayerId != playerId)) return;
            await BroadcastToWorld(conn.CurrentWorldId, PacketType.PartModuleState, state, playerId);
        }

        private async Task HandleBuildSpawn(ClientConnection conn, string playerId, BuildSnapshot build)
        {
            if (conn.CurrentWorldId == null || build == null) return;
            if (string.IsNullOrEmpty(build.BuildId)) build.BuildId = Guid.NewGuid().ToString("N");
            var existing = _worlds.FindBuild(conn.CurrentWorldId, build.BuildId);
            if (existing != null && existing.OwnerPlayerId != playerId)
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "Cannot overwrite another player's build." });
                return;
            }
            build.OwnerPlayerId = playerId;
            build.OwnerPlayerName = conn.Account.PlayerName;
            build.ControllingPlayerId = playerId;
            var saved = _worlds.AddOrUpdateBuild(conn.CurrentWorldId, build);
            if (saved == null) return;
            if (existing == null) _claims.Create(conn.CurrentWorldId, playerId, conn.Account.PlayerName, new ClaimCreatePacket { WorldId = conn.CurrentWorldId, Shape = ClaimShape.Build, BuildId = saved.BuildId });
            await BroadcastToWorld(conn.CurrentWorldId, PacketType.BuildSpawn, saved, playerId);
        }

        private async Task HandleBuildStateUpdate(ClientConnection conn, string playerId, BuildStateUpdatePacket update)
        {
            if (conn.CurrentWorldId == null || update == null || string.IsNullOrEmpty(update.BuildId)) return;
            if (!IsFinite(update.PosX) || !IsFinite(update.PosY) || !IsFinite(update.VelX) || !IsFinite(update.VelY) || !IsFinite(update.RotationDegrees) || !IsFinite(update.AngularVelocity) || !IsFinite(update.WorldTime))
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "Build state contains invalid numeric values." });
                return;
            }
            if (update.WorldId != conn.CurrentWorldId)
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "World mismatch." });
                return;
            }
            var build = _worlds.FindBuild(conn.CurrentWorldId, update.BuildId);
            if (build == null) return;
            if (build.OwnerPlayerId != playerId && build.ControllingPlayerId != playerId)
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "Not your rocket." });
                return;
            }
            if (!_claims.CanInteract(conn.CurrentWorldId, update.BuildId, playerId))
            {
                var claim = _claims.FindCovering(conn.CurrentWorldId, update.BuildId);
                await conn.SendAsync(PacketType.ClaimDenied, new ClaimDeniedPacket { ClaimId = claim?.ClaimId, OwnerPlayerName = claim?.OwnerPlayerName, Reason = "This build is claimed by another player." });
                return;
            }
            _worlds.ApplyStateUpdate(conn.CurrentWorldId, update);
            await BroadcastToWorld(conn.CurrentWorldId, PacketType.BuildStateUpdate, update, playerId);
        }

        private async Task HandleBuildRemove(ClientConnection conn, string playerId, BuildSnapshot build)
        {
            if (conn.CurrentWorldId == null || build == null) return;
            var existing = _worlds.FindBuild(conn.CurrentWorldId, build.BuildId);
            if (existing == null || existing.OwnerPlayerId != playerId || !_claims.CanInteract(conn.CurrentWorldId, build.BuildId, playerId))
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "You cannot remove this build." });
                return;
            }
            _worlds.RemoveBuild(conn.CurrentWorldId, build.BuildId);
            _claims.RemoveForBuild(conn.CurrentWorldId, build.BuildId, playerId);
            await BroadcastToWorld(conn.CurrentWorldId, PacketType.BuildRemove, build, playerId);
        }

        private async Task HandleBuildControlRequest(ClientConnection conn, string playerId, BuildControlRequestPacket req)
        {
            if (req == null || req.WorldId == null || req.WorldId != conn.CurrentWorldId) return;
            var build = _worlds.FindBuild(req.WorldId, req.BuildId);
            if (build == null) return;
            bool free = build.ControllingPlayerId == null || build.ControllingPlayerId == playerId;
            bool allowed = _claims.CanInteract(req.WorldId, req.BuildId, playerId);
            if (free && allowed)
            {
                build.ControllingPlayerId = playerId;
                _worlds.Persist(req.WorldId);
                await conn.SendAsync(PacketType.BuildControlGrant, new BuildControlGrantPacket { BuildId = req.BuildId, Granted = true, ControllingPlayerId = playerId });
                await BroadcastToWorld(req.WorldId, PacketType.BuildControlGrant, new BuildControlGrantPacket { BuildId = req.BuildId, Granted = true, ControllingPlayerId = playerId }, playerId);
            }
            else
            {
                await conn.SendAsync(PacketType.BuildControlGrant, new BuildControlGrantPacket { BuildId = req.BuildId, Granted = false, DenyReason = !allowed ? "Build is claimed by another player." : "Another player is already piloting this." });
            }
        }
    }
}
