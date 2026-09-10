using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SFSEnhanced.Server.Persistence;
using SFSEnhanced.Server.Social;
using SFSEnhanced.Server.World;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Server.Networking
{
    public class NetServer
    {
        private readonly ServerConfig _config;
        private readonly AccountService _accounts;
        private readonly WorldManager _worlds;
        private readonly FriendsService _friends;
        private readonly ClaimsService _claims;
        private readonly ConcurrentDictionary<string, ClientConnection> _connections = new();
        private readonly ConcurrentDictionary<string, List<string>> _pendingUploads = new();
        private readonly ConcurrentDictionary<string, TimeWarpVoteState> _timeWarpVotes = new();
        private int _nextVoteId;

        public int ConnectedPlayerCount => _connections.Count;

        public NetServer(ServerConfig config, AccountService accounts, WorldManager worlds, FriendsService friends, ClaimsService claims)
        {
            _config = config;
            _accounts = accounts;
            _worlds = worlds;
            _friends = friends;
            _claims = claims;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            SecureSessionHandshake.InitializeStaticKey(new FileStore(_config.DataDir));
            var transport = new LidgrenServerTransport(_config.Port, _config.MaxPlayers);
            transport.ConnectionAccepted += session => _ = HandleClientAsync(session, ct);
            var autosave = AutosaveLoopAsync(ct);
            var worldTime = WorldTimeLoopAsync(ct);
            try
            {
                await transport.StartAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            finally
            {
                transport.Stop();
                _worlds.PersistAll();
                await autosave;
                await worldTime;
            }
        }

        private async Task WorldTimeLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(250, ct);
                    foreach (var world in _worlds.All().ToList())
                    {
                        AdvanceWorldTime(world);
                        await BroadcastToWorld(world.WorldId, PacketType.WorldTimeState, new WorldTimeStatePacket
                        {
                            WorldId = world.WorldId,
                            WorldTime = world.WorldTime,
                            TimewarpMultiplier = world.TimewarpMultiplier,
                            Tick = DateTime.UtcNow.Ticks
                        }, null);
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private static void AdvanceWorldTime(WorldRecord world)
        {
            var now = DateTime.UtcNow;
            double elapsed = (now - world.WorldTimeUpdatedUtc).TotalSeconds;
            if (elapsed > 0) world.WorldTime += elapsed * Math.Max(0.0, world.TimewarpMultiplier);
            world.WorldTimeUpdatedUtc = now;
        }
        private async Task AutosaveLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), ct);
                    _worlds.PersistAll();
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task HandleClientAsync(LidgrenServerConnection transport, CancellationToken ct)
        {
            var conn = new ClientConnection(transport);
            string playerId = null;

            try
            {
                var handshake = new SecureSessionHandshake();
                PlayerAccount account = await handshake.AuthenticateAsync(conn, _accounts, _config, ct).ConfigureAwait(false);
                if (account == null)
                {
                    Console.WriteLine("[auth-failed] handshake rejected");
                    return;
                }
                playerId = account.PlayerId;
                _accounts.Touch(account);

                if (!_connections.ContainsKey(account.PlayerId) && _connections.Count >= _config.MaxPlayers)
                {
                    await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "Server is full." });
                    return;
                }
                if (_connections.TryGetValue(account.PlayerId, out ClientConnection oldConnection) && !ReferenceEquals(oldConnection, conn))
                    oldConnection.Close();
                _connections[account.PlayerId] = conn;
                _friends.SetOnline(account.PlayerId, true);
                Console.WriteLine($"[connect] {account.PlayerName} ({account.PlayerId})");

                while (!ct.IsCancellationRequested)
                {
                    var (ok, type, json) = await conn.ReceiveSecureAsync(ct).ConfigureAwait(false);
                    if (!ok) break;

                    switch (type)
                    {
                        case PacketType.Disconnect:
                            return;
                        case PacketType.ServerInfoRequest:
                            await conn.SendAsync(PacketType.ServerInfoResponse, new ServerInfoResponsePacket
                            {
                                ServerName = _config.ServerName,
                                Motd = _config.Motd,
                                OnlinePlayers = _connections.Count,
                                MaxPlayers = _config.MaxPlayers,
                                ServerVersion = _config.ModVersion
                            });
                            break;
                        case PacketType.WorldListRequest:
                            await conn.SendAsync(PacketType.WorldListResponse, new WorldListResponsePacket
                            {
                                Worlds = _worlds.ListPublic().Select(w => new WorldSummary
                                {
                                    WorldId = w.WorldId,
                                    Name = w.Name,
                                    OwnerName = _accounts.FindById(w.OwnerPlayerId)?.PlayerName ?? "?",
                                    IsPublic = w.IsPublic,
                                    PlayersOnline = _connections.Values.Count(c => c.CurrentWorldId == w.WorldId),
                                    BuildCount = w.Builds.Count,
                                    LastModifiedUtc = w.LastModifiedUtc
                                }).ToList()
                            });
                            break;
                        case PacketType.WorldCreate:
                            await HandleWorldCreate(conn, playerId, Deserialize<WorldCreatePacket>(json));
                            break;
                        case PacketType.WorldJoin:
                            await HandleWorldJoin(conn, playerId, Deserialize<WorldJoinPacket>(json));
                            break;
                        case PacketType.WorldLeave:
                            HandleWorldLeave(conn, playerId);
                            break;
                        case PacketType.WorldUploadChunk:
                            await HandleWorldUploadChunk(conn, playerId, Deserialize<WorldUploadChunkPacket>(json));
                            break;
                        case PacketType.WorldDownloadRequest:
                            await HandleWorldDownloadRequest(conn, Deserialize<WorldJoinPacket>(json));
                            break;
                        case PacketType.BuildSpawn:
                            await HandleBuildSpawn(conn, playerId, Deserialize<BuildSnapshot>(json));
                            break;
                        case PacketType.BuildStateUpdate:
                            await HandleBuildStateUpdate(conn, playerId, Deserialize<BuildStateUpdatePacket>(json));
                            break;
                        case PacketType.BuildRemove:
                            await HandleBuildRemove(conn, playerId, Deserialize<BuildSnapshot>(json));
                            break;
                        case PacketType.RocketPrimaryState:
                            await HandleRocketPrimaryState(conn, playerId, Deserialize<RocketPrimaryStatePacket>(json));
                            break;
                        case PacketType.RocketSecondaryState:
                            await HandleRocketSecondaryState(conn, playerId, Deserialize<RocketSecondaryStatePacket>(json));
                            break;
                        case PacketType.PartModuleState:
                            await HandlePartModuleState(conn, playerId, Deserialize<PartModuleStatePacket>(json));
                            break;
                        case PacketType.BuildControlRequest:
                            await HandleBuildControlRequest(conn, playerId, Deserialize<BuildControlRequestPacket>(json));
                            break;
                        case PacketType.FriendRequest:
                            await HandleFriendRequest(conn, Deserialize<FriendRequestPacket>(json));
                            break;
                        case PacketType.FriendRequestResponse:
                            await HandleFriendResponse(conn, Deserialize<FriendRequestResponsePacket>(json));
                            break;
                        case PacketType.FriendListRequest:
                            await conn.SendAsync(PacketType.FriendListResponse, _friends.BuildFriendList(conn.Account));
                            break;
                        case PacketType.ClaimCreate:
                            await HandleClaimCreate(conn, Deserialize<ClaimCreatePacket>(json));
                            break;
                        case PacketType.ClaimRemove:
                            await HandleClaimRemove(conn, Deserialize<ClaimRemovePacket>(json));
                            break;
                        case PacketType.ChatMessage:
                            await HandleChat(conn, Deserialize<ChatMessagePacket>(json));
                            break;
                        case PacketType.TimeWarpVoteResponse:
                            await HandleTimeWarpVoteResponse(conn, playerId, Deserialize<TimeWarpVoteResponsePacket>(json));
                            break;
                        case PacketType.TimeWarpRequest:
                            await HandleTimeWarp(conn, playerId, Deserialize<TimeWarpRequestPacket>(json));
                            break;
                        case PacketType.FriendInviteToWorld:
                            await HandleFriendInvite(conn, Deserialize<FriendInviteToWorldPacket>(json));
                            break;
                        default:
                            await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = $"Unhandled packet {type}" });
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[conn error] {ex.Message}");
            }
            finally
            {
                if (playerId != null)
                {
                    HandleWorldLeave(conn, playerId);
                    foreach (var key in _pendingUploads.Keys.Where(key => key.StartsWith(playerId + ":", StringComparison.Ordinal)).ToList())
                        _pendingUploads.TryRemove(key, out _);
                    if (_connections.TryGetValue(playerId, out var current) && ReferenceEquals(current, conn))
                    {
                        _connections.TryRemove(playerId, out _);
                        _friends.SetOnline(playerId, false);
                    }
                    Console.WriteLine($"[disconnect] {conn.Account?.PlayerName ?? playerId}");
                }
                conn.Close();
            }
        }

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

        private async Task HandleFriendRequest(ClientConnection conn, FriendRequestPacket req)
        {
            bool ok = _friends.RequestFriend(conn.Account, req?.TargetPlayerName, out string error);
            if (!ok)
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = error });
                return;
            }
            var target = _accounts.FindByName(req.TargetPlayerName);
            if (target != null && _connections.TryGetValue(target.PlayerId, out var targetConn)) await targetConn.SendAsync(PacketType.FriendListResponse, _friends.BuildFriendList(target));
            await conn.SendAsync(PacketType.FriendListResponse, _friends.BuildFriendList(conn.Account));
        }

        private async Task HandleFriendResponse(ClientConnection conn, FriendRequestResponsePacket resp)
        {
            _friends.RespondToRequest(conn.Account, resp?.FromPlayerId, resp != null && resp.Accepted);
            await conn.SendAsync(PacketType.FriendListResponse, _friends.BuildFriendList(conn.Account));
            if (resp != null && _connections.TryGetValue(resp.FromPlayerId, out var otherConn)) await otherConn.SendAsync(PacketType.FriendListResponse, _friends.BuildFriendList(otherConn.Account));
        }

        private async Task HandleClaimCreate(ClientConnection conn, ClaimCreatePacket req)
        {
            if (req == null || req.WorldId != conn.CurrentWorldId) return;
            if (req.Shape == ClaimShape.Build)
            {
                var build = _worlds.FindBuild(req.WorldId, req.BuildId);
                if (build == null || build.OwnerPlayerId != conn.Account.PlayerId)
                {
                    await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "You can only claim your own build." });
                    return;
                }
            }
            var claim = _claims.Create(req.WorldId, conn.Account.PlayerId, conn.Account.PlayerName, req);
            if (claim != null) await BroadcastToWorld(req.WorldId, PacketType.ClaimCreate, claim, null);
        }

        private async Task HandleClaimRemove(ClientConnection conn, ClaimRemovePacket req)
        {
            if (req == null || req.WorldId != conn.CurrentWorldId || string.IsNullOrEmpty(req.ClaimId)) return;
            if (_claims.Remove(req.WorldId, req.ClaimId, conn.Account.PlayerId)) await BroadcastToWorld(req.WorldId, PacketType.ClaimRemove, req, null);
        }

        private async Task HandleChat(ClientConnection conn, ChatMessagePacket msg)
        {
            if (msg == null || string.IsNullOrWhiteSpace(msg.Message)) return;
            msg.Message = msg.Message.Trim();
            if (msg.Message.Length > 512) msg.Message = msg.Message.Substring(0, 512);
            msg.FromPlayerName = conn.Account.PlayerName;
            msg.SentUtc = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(msg.WorldId))
            {
                if (msg.WorldId != conn.CurrentWorldId) return;
                await BroadcastToWorld(msg.WorldId, PacketType.ChatMessage, msg, null);
            }
            else await BroadcastToAll(PacketType.ChatMessage, msg, null);
        }

        private async Task HandleTimeWarpVoteResponse(ClientConnection conn, string playerId, TimeWarpVoteResponsePacket response)
        {
            if (response == null || conn.CurrentWorldId == null || response.WorldId != conn.CurrentWorldId) return;
            if (!_timeWarpVotes.TryGetValue(response.WorldId, out var vote) || vote.VoteId != response.VoteId) return;
            lock (vote)
            {
                if (vote.Completed || DateTime.UtcNow > vote.ExpiresUtc) return;
                vote.Votes[playerId] = response.Approved;
                if (vote.Votes.Count < vote.PlayerIds.Count) return;
                vote.Completed = true;
            }
            bool approved = vote.Votes.Values.All(v => v);
            var world = _worlds.Get(response.WorldId);
            if (world == null) return;
            AdvanceWorldTime(world);
            double actual = approved ? vote.RequestedMultiplier : 1.0;
            if (actual > 1.0 && IsAnyPlayerNearby(world)) actual = 1.0;
            world.TimewarpMultiplier = actual;
            _worlds.Persist(response.WorldId);
            _timeWarpVotes.TryRemove(response.WorldId, out _);
            await BroadcastToWorld(response.WorldId, PacketType.TimeWarpResult, new TimeWarpResultPacket { WorldId = response.WorldId, VoteId = response.VoteId, Approved = approved, ActualMultiplier = actual, Reason = approved ? "Vote approved." : "A player rejected the request." }, null);
        }

        private static bool IsAnyPlayerNearby(WorldRecord world)
        {
            const double radiusMeters = 50000;
            var builds = world.Builds.Where(b => b != null).ToList();
            for (int i = 0; i < builds.Count; i++)
            for (int j = i + 1; j < builds.Count; j++)
            {
                if (!string.Equals(builds[i].PlanetAddress, builds[j].PlanetAddress, StringComparison.Ordinal)) continue;
                double dx = builds[i].PosX - builds[j].PosX;
                double dy = builds[i].PosY - builds[j].PosY;
                if (dx * dx + dy * dy <= radiusMeters * radiusMeters) return true;
            }
            return false;
        }
        private async Task HandleTimeWarp(ClientConnection conn, string playerId, TimeWarpRequestPacket req)
        {
            if (req == null || conn.CurrentWorldId == null || req.WorldId != conn.CurrentWorldId) return;
            var world = _worlds.Get(conn.CurrentWorldId);
            if (world == null) return;
            AdvanceWorldTime(world);
            double requested = req.RequestedMultiplier < 1 ? 1 : Math.Min(req.RequestedMultiplier, 1000);
            var players = _connections.Values.Where(c => c.CurrentWorldId == world.WorldId && c.Account != null).Select(c => c.Account.PlayerId).Distinct().ToList();
            if (players.Count <= 1)
            {
                world.TimewarpMultiplier = requested;
                _worlds.Persist(world.WorldId);
                await BroadcastToWorld(world.WorldId, PacketType.TimeWarpResult, new TimeWarpResultPacket { WorldId = world.WorldId, VoteId = 0, Approved = true, ActualMultiplier = requested, Reason = "Single-player world." }, null);
                return;
            }
            if (_timeWarpVotes.ContainsKey(world.WorldId))
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "A time-warp vote is already active." });
                return;
            }
            var vote = new TimeWarpVoteState { VoteId = Interlocked.Increment(ref _nextVoteId), WorldId = world.WorldId, RequestedMultiplier = requested, ExpiresUtc = DateTime.UtcNow.AddSeconds(15) };
            vote.PlayerIds.AddRange(players);
            vote.Votes[playerId] = true;
            _timeWarpVotes[world.WorldId] = vote;
            await BroadcastToWorld(world.WorldId, PacketType.TimeWarpVote, new TimeWarpVotePacket { WorldId = world.WorldId, RequesterPlayerId = playerId, RequesterPlayerName = conn.Account.PlayerName, VoteId = vote.VoteId, RequestedMultiplier = requested }, null);
            _ = CompleteExpiredVoteAsync(vote);
        }

        private async Task CompleteExpiredVoteAsync(TimeWarpVoteState vote)
        {
            await Task.Delay(TimeSpan.FromSeconds(15));
            if (!_timeWarpVotes.TryGetValue(vote.WorldId, out var current) || !ReferenceEquals(current, vote)) return;
            lock (vote) vote.Completed = true;
            _timeWarpVotes.TryRemove(vote.WorldId, out _);
            await BroadcastToWorld(vote.WorldId, PacketType.TimeWarpResult, new TimeWarpResultPacket { WorldId = vote.WorldId, VoteId = vote.VoteId, Approved = false, ActualMultiplier = 1.0, Reason = "Time-warp vote timed out." }, null);
        }
        private async Task HandleFriendInvite(ClientConnection conn, FriendInviteToWorldPacket invite)
        {
            if (invite == null) return;
            var target = _accounts.FindByName(invite.TargetPlayerName);
            if (target == null)
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "Player not found." });
                return;
            }
            if (!_connections.TryGetValue(target.PlayerId, out var targetConn))
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "That player is offline." });
                return;
            }
            string worldId = invite.WorldId ?? conn.CurrentWorldId;
            await targetConn.SendAsync(PacketType.FriendInviteToWorld, new FriendInviteToWorldPacket { TargetPlayerName = conn.Account.PlayerName, WorldId = worldId });
        }

        private sealed class TimeWarpVoteState
        {
            public string WorldId;
            public int VoteId;
            public double RequestedMultiplier;
            public DateTime ExpiresUtc;
            public bool Completed;
            public readonly List<string> PlayerIds = new();
            public readonly Dictionary<string, bool> Votes = new();
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private async Task BroadcastToWorld(string worldId, PacketType type, object payload, string exceptPlayerId)
        {
            var targets = _connections.Where(kv => kv.Value.CurrentWorldId == worldId && kv.Key != exceptPlayerId);
            foreach (var kv in targets.ToList())
            {
                try { await kv.Value.SendAsync(type, payload); }
                catch (Exception ex) { Console.WriteLine($"[broadcast error] {ex.Message}"); }
            }
        }

        private async Task BroadcastToAll(PacketType type, object payload, string exceptPlayerId)
        {
            foreach (var kv in _connections.Where(kv => kv.Key != exceptPlayerId).ToList())
            {
                try { await kv.Value.SendAsync(type, payload); }
                catch (Exception ex) { Console.WriteLine($"[broadcast error] {ex.Message}"); }
            }
        }

        private static T Deserialize<T>(string json) => json == null ? default : Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);

        public void PrintConnectedPlayers()
        {
            if (_connections.IsEmpty) { Console.WriteLine("No players connected."); return; }
            foreach (var c in _connections.Values) Console.WriteLine($"  {c.Account.PlayerName}  world={c.CurrentWorldId ?? "(lobby)"}");
        }

        public void PrintWorlds()
        {
            foreach (var w in _worlds.All()) Console.WriteLine($"  {w.Name} [{w.WorldId}]  builds={w.Builds.Count}  public={w.IsPublic}");
        }
    }
}
