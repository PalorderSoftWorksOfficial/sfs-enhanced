using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

        private readonly ConcurrentDictionary<string, ClientConnection> _connections = new(); // key: PlayerId
        private readonly ConcurrentDictionary<string, List<string>> _pendingUploads = new();  // uploadId -> chunks so far (base64)

        public NetServer(ServerConfig config, AccountService accounts, WorldManager worlds,
                          FriendsService friends, ClaimsService claims)
        {
            _config = config;
            _accounts = accounts;
            _worlds = worlds;
            _friends = friends;
            _claims = claims;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            var listener = new TcpListener(IPAddress.Any, _config.Port);
            listener.Start();

            var autosave = AutosaveLoopAsync(ct);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var acceptTask = listener.AcceptTcpClientAsync();
                    var completed = await Task.WhenAny(acceptTask, Task.Delay(Timeout.Infinite, ct))
                        .ConfigureAwait(false);
                    if (completed != acceptTask) break; // cancelled

                    var tcpClient = acceptTask.Result;
                    _ = HandleClientAsync(tcpClient, ct); // fire-and-forget per-client loop
                }
            }
            catch (OperationCanceledException) { /* normal shutdown */ }
            finally
            {
                listener.Stop();
                _worlds.PersistAll();
                await autosave;
            }
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

        private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken ct)
        {
            var conn = new ClientConnection(tcpClient);
            string playerId = null;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var (type, json) = await NetMessage.ReadRawAsync(conn.Stream);
                    if (type == PacketType.Disconnect && json == null) break; // socket closed

                    switch (type)
                    {
                        case PacketType.Hello:
                            playerId = await HandleHello(conn, Deserialize<HelloPacket>(json));
                            break;

                        case PacketType.Ping:
                            await conn.SendAsync(PacketType.Pong, null);
                            break;

                        case PacketType.ServerInfoRequest:
                            await conn.SendAsync(PacketType.ServerInfoResponse, new ServerInfoResponsePacket
                            {
                                ServerName = _config.ServerName,
                                Motd = _config.Motd,
                                OnlinePlayers = _connections.Count,
                                MaxPlayers = _config.MaxPlayers,
                                ServerVersion = "0.1.0",
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
                                    LastModifiedUtc = w.LastModifiedUtc,
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

                        case PacketType.ChatMessage:
                            await HandleChat(conn, Deserialize<ChatMessagePacket>(json));
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
                    _connections.TryRemove(playerId, out _);
                    _friends.SetOnline(playerId, false);
                    Console.WriteLine($"[disconnect] {conn.Account?.PlayerName ?? playerId}");
                }
                conn.Close();
            }
        }

        // ---- Handlers ----

        private async Task<string> HandleHello(ClientConnection conn, HelloPacket hello)
        {
            PlayerAccount account = null;

            if (!string.IsNullOrEmpty(hello?.AuthToken))
            {
                var byName = _accounts.FindByName(hello.PlayerName);
                if (byName != null && _accounts.ValidateToken(byName, hello.AuthToken))
                    account = byName;
            }

            string issuedToken = null;
            if (account == null)
            {
                if (_accounts.FindByName(hello?.PlayerName) != null)
                {
                    await conn.SendAsync(PacketType.HelloAck, new HelloAckPacket
                    {
                        Accepted = false,
                        RejectReason = "That name is taken and the auth token didn't match. Use a different name or your saved token.",
                    });
                    return null;
                }
                var (newAccount, token) = _accounts.CreateAccount(hello.PlayerName);
                account = newAccount;
                issuedToken = token;
            }

            conn.Account = account;
            _accounts.Touch(account);
            _connections[account.PlayerId] = conn;
            _friends.SetOnline(account.PlayerId, true);

            await conn.SendAsync(PacketType.HelloAck, new HelloAckPacket
            {
                Accepted = true,
                PlayerId = account.PlayerId,
                AuthToken = issuedToken, // null if reusing an existing token — client keeps its own
            });

            Console.WriteLine($"[connect] {account.PlayerName} ({account.PlayerId})");
            return account.PlayerId;
        }

        private async Task HandleWorldCreate(ClientConnection conn, string playerId, WorldCreatePacket req)
        {
            var world = _worlds.Create(req.Name, playerId, req.IsPublic, req.PlanetPackId);
            await conn.SendAsync(PacketType.WorldJoinAck, new WorldJoinAckPacket
            {
                Accepted = true,
                WorldId = world.WorldId,
                Builds = world.Builds,
                Claims = world.Claims,
                PlayersOnline = new List<string> { conn.Account.PlayerName },
            });
            conn.CurrentWorldId = world.WorldId;
            _friends.SetCurrentWorld(playerId, world.WorldId);
        }

        private async Task HandleWorldJoin(ClientConnection conn, string playerId, WorldJoinPacket req)
        {
            var world = _worlds.Get(req.WorldId);
            if (world == null)
            {
                await conn.SendAsync(PacketType.WorldJoinAck, new WorldJoinAckPacket { Accepted = false, RejectReason = "World not found." });
                return;
            }

            conn.CurrentWorldId = world.WorldId;
            _friends.SetCurrentWorld(playerId, world.WorldId);

            var playersInWorld = _connections.Values
                .Where(c => c.CurrentWorldId == world.WorldId)
                .Select(c => c.Account.PlayerName)
                .ToList();

            await conn.SendAsync(PacketType.WorldJoinAck, new WorldJoinAckPacket
            {
                Accepted = true,
                WorldId = world.WorldId,
                Builds = world.Builds,
                Claims = world.Claims,
                PlayersOnline = playersInWorld,
            });

            await BroadcastToWorld(world.WorldId, PacketType.ChatMessage, new ChatMessagePacket
            {
                WorldId = world.WorldId,
                FromPlayerName = "Server",
                Message = $"{conn.Account.PlayerName} joined the world.",
            }, exceptPlayerId: null);
        }

        private void HandleWorldLeave(ClientConnection conn, string playerId)
        {
            conn.CurrentWorldId = null;
            if (playerId != null) _friends.SetCurrentWorld(playerId, null);
        }

        private async Task HandleWorldUploadChunk(ClientConnection conn, string playerId, WorldUploadChunkPacket chunk)
        {
            var buffer = _pendingUploads.GetOrAdd(chunk.UploadId, _ => new List<string>(new string[chunk.TotalChunks]));
            buffer[chunk.ChunkIndex] = chunk.Base64Data;

            if (buffer.All(b => b != null))
            {
                string fullJson = Encoding.UTF8.GetString(Convert.FromBase64String(string.Concat(buffer)));
                var uploaded = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldRecord>(fullJson);
                uploaded.WorldId = Guid.NewGuid().ToString("N");
                uploaded.OwnerPlayerId = playerId;
                uploaded.Name = chunk.WorldName ?? uploaded.Name;
                uploaded.IsPublic = chunk.IsPublic;

                var created = _worlds.Create(uploaded.Name, playerId, uploaded.IsPublic, uploaded.PlanetPackId);
                // merge builds/claims from the uploaded file into the freshly-created record
                created.Builds = uploaded.Builds;
                created.Claims = uploaded.Claims;
                _worlds.Persist(created.WorldId);

                _pendingUploads.TryRemove(chunk.UploadId, out _);

                await conn.SendAsync(PacketType.WorldJoinAck, new WorldJoinAckPacket
                {
                    Accepted = true,
                    WorldId = created.WorldId,
                    Builds = created.Builds,
                    Claims = created.Claims,
                });
            }
        }

        private async Task HandleWorldDownloadRequest(ClientConnection conn, WorldJoinPacket req)
        {
            var world = _worlds.Get(req.WorldId);
            if (world == null)
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "World not found." });
                return;
            }

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(world);
            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            const int chunkSize = 32 * 1024;
            int totalChunks = (int)Math.Ceiling(base64.Length / (double)chunkSize);

            for (int i = 0; i < totalChunks; i++)
            {
                string piece = base64.Substring(i * chunkSize, Math.Min(chunkSize, base64.Length - i * chunkSize));
                await conn.SendAsync(PacketType.WorldDownloadChunk, new WorldDownloadChunkPacket
                {
                    WorldId = world.WorldId,
                    ChunkIndex = i,
                    TotalChunks = totalChunks,
                    Base64Data = piece,
                });
            }
        }

        private async Task HandleBuildSpawn(ClientConnection conn, string playerId, BuildSnapshot build)
        {
            if (conn.CurrentWorldId == null || build == null) return;

            if (string.IsNullOrEmpty(build.BuildId))
                build.BuildId = Guid.NewGuid().ToString("N");

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

            if (existing == null)
            {
                _claims.Create(conn.CurrentWorldId, playerId, conn.Account.PlayerName, new ClaimCreatePacket
                {
                    WorldId = conn.CurrentWorldId,
                    Shape = ClaimShape.Build,
                    BuildId = saved.BuildId,
                });
            }

            await BroadcastToWorld(conn.CurrentWorldId, PacketType.BuildSpawn, saved, exceptPlayerId: playerId);
        }

        private async Task HandleBuildStateUpdate(ClientConnection conn, string playerId, BuildStateUpdatePacket update)
        {
            if (conn.CurrentWorldId == null || update == null) return;
            var build = _worlds.FindBuild(conn.CurrentWorldId, update.BuildId);
            if (build == null) return;

            bool owns = build.OwnerPlayerId == playerId;
            bool controls = build.ControllingPlayerId == playerId;
            if (!owns && !controls)
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = "Not your rocket." });
                return;
            }

            if (!_claims.CanInteract(conn.CurrentWorldId, update.BuildId, playerId))
            {
                var claim = _claims.FindCovering(conn.CurrentWorldId, update.BuildId);
                await conn.SendAsync(PacketType.ClaimDenied, new ClaimDeniedPacket
                {
                    ClaimId = claim?.ClaimId,
                    OwnerPlayerName = claim?.OwnerPlayerName,
                    Reason = "This build is claimed by another player.",
                });
                return;
            }

            update.WorldId = conn.CurrentWorldId;
            _worlds.ApplyStateUpdate(conn.CurrentWorldId, update);
            await BroadcastToWorld(conn.CurrentWorldId, PacketType.BuildStateUpdate, update, exceptPlayerId: playerId);
        }

        private async Task HandleBuildRemove(ClientConnection conn, string playerId, BuildSnapshot build)
        {
            if (conn.CurrentWorldId == null) return;
            if (!_claims.CanInteract(conn.CurrentWorldId, build.BuildId, playerId)) return;
            _worlds.RemoveBuild(conn.CurrentWorldId, build.BuildId);
            await BroadcastToWorld(conn.CurrentWorldId, PacketType.BuildRemove, build, exceptPlayerId: playerId);
        }

        private async Task HandleBuildControlRequest(ClientConnection conn, string playerId, BuildControlRequestPacket req)
        {
            var build = _worlds.FindBuild(req.WorldId, req.BuildId);
            if (build == null) return;

            bool free = build.ControllingPlayerId == null || build.ControllingPlayerId == playerId;
            bool allowed = _claims.CanInteract(req.WorldId, req.BuildId, playerId);

            if (free && allowed)
            {
                build.ControllingPlayerId = playerId;
                await conn.SendAsync(PacketType.BuildControlGrant, new BuildControlGrantPacket
                {
                    BuildId = req.BuildId, Granted = true, ControllingPlayerId = playerId,
                });
                await BroadcastToWorld(req.WorldId, PacketType.BuildControlGrant, new BuildControlGrantPacket
                {
                    BuildId = req.BuildId, Granted = true, ControllingPlayerId = playerId,
                }, exceptPlayerId: playerId);
            }
            else
            {
                await conn.SendAsync(PacketType.BuildControlGrant, new BuildControlGrantPacket
                {
                    BuildId = req.BuildId,
                    Granted = false,
                    DenyReason = !allowed ? "Build is claimed by another player." : "Another player is already piloting this.",
                });
            }
        }

        private async Task HandleFriendRequest(ClientConnection conn, FriendRequestPacket req)
        {
            bool ok = _friends.RequestFriend(conn.Account, req.TargetPlayerName, out string error);
            if (!ok)
            {
                await conn.SendAsync(PacketType.Error, new ErrorPacket { Message = error });
                return;
            }
            var target = _accounts.FindByName(req.TargetPlayerName);
            if (target != null && _connections.TryGetValue(target.PlayerId, out var targetConn))
            {
                await targetConn.SendAsync(PacketType.FriendListResponse, _friends.BuildFriendList(target));
            }
            await conn.SendAsync(PacketType.FriendListResponse, _friends.BuildFriendList(conn.Account));
        }

        private async Task HandleFriendResponse(ClientConnection conn, FriendRequestResponsePacket resp)
        {
            _friends.RespondToRequest(conn.Account, resp.FromPlayerId, resp.Accepted);
            await conn.SendAsync(PacketType.FriendListResponse, _friends.BuildFriendList(conn.Account));
            if (_connections.TryGetValue(resp.FromPlayerId, out var otherConn))
                await otherConn.SendAsync(PacketType.FriendListResponse, _friends.BuildFriendList(otherConn.Account));
        }

        private async Task HandleClaimCreate(ClientConnection conn, ClaimCreatePacket req)
        {
            var claim = _claims.Create(req.WorldId, conn.Account.PlayerId, conn.Account.PlayerName, req);
            if (claim != null)
                await BroadcastToWorld(req.WorldId, PacketType.ClaimCreate, claim, exceptPlayerId: null);
        }

        private async Task HandleChat(ClientConnection conn, ChatMessagePacket msg)
        {
            msg.FromPlayerName = conn.Account.PlayerName;
            msg.SentUtc = DateTime.UtcNow;
            if (msg.WorldId != null)
                await BroadcastToWorld(msg.WorldId, PacketType.ChatMessage, msg, exceptPlayerId: null);
            else
                await BroadcastToAll(PacketType.ChatMessage, msg, exceptPlayerId: null);
        }

        private async Task HandleTimeWarp(ClientConnection conn, string playerId, TimeWarpRequestPacket req)
        {
            if (conn.CurrentWorldId == null || req == null) return;
            var world = _worlds.Get(conn.CurrentWorldId);
            if (world == null) return;

            bool locked = IsProximityLocked(world, playerId);
            double requested = req.RequestedMultiplier < 1 ? 1 : req.RequestedMultiplier;
            double actual = locked && requested > 1.0 ? 1.0 : requested;

            await BroadcastToWorld(conn.CurrentWorldId, PacketType.TimeWarpState, new TimeWarpStatePacket
            {
                WorldId = conn.CurrentWorldId,
                ActualMultiplier = actual,
                LockedByProximity = locked,
            }, exceptPlayerId: null);
        }

        /// <summary>
        /// True when another player's build is within 50 km of one of this player's builds.
        /// Nearby co-op should not free-warp or the two sims diverge.
        /// </summary>
        private static bool IsProximityLocked(WorldRecord world, string playerId)
        {
            const double radiusMeters = 50_000;
            var mine = world.Builds.Where(b => b.OwnerPlayerId == playerId).ToList();
            var others = world.Builds.Where(b => b.OwnerPlayerId != playerId).ToList();
            if (mine.Count == 0 || others.Count == 0) return false;

            foreach (var a in mine)
            {
                foreach (var b in others)
                {
                    if (!string.Equals(a.PlanetAddress, b.PlanetAddress, StringComparison.Ordinal)) continue;
                    double dx = a.PosX - b.PosX;
                    double dy = a.PosY - b.PosY;
                    if (dx * dx + dy * dy <= radiusMeters * radiusMeters) return true;
                }
            }
            return false;
        }

        private async Task HandleFriendInvite(ClientConnection conn, FriendInviteToWorldPacket invite)
        {
            var target = _accounts.FindByName(invite?.TargetPlayerName);
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
            await targetConn.SendAsync(PacketType.FriendInviteToWorld, new FriendInviteToWorldPacket
            {
                TargetPlayerName = conn.Account.PlayerName,
                WorldId = worldId,
            });
            await targetConn.SendAsync(PacketType.ChatMessage, new ChatMessagePacket
            {
                FromPlayerName = "Server",
                Message = $"{conn.Account.PlayerName} invited you to a world. WorldId: {worldId}",
            });
        }

        // ---- Broadcast helpers ----

        private async Task BroadcastToWorld(string worldId, PacketType type, object payload, string exceptPlayerId)
        {
            var targets = _connections.Where(kv => kv.Value.CurrentWorldId == worldId && kv.Key != exceptPlayerId);
            foreach (var kv in targets)
                await kv.Value.SendAsync(type, payload);
        }

        private async Task BroadcastToAll(PacketType type, object payload, string exceptPlayerId)
        {
            foreach (var kv in _connections.Where(kv => kv.Key != exceptPlayerId))
                await kv.Value.SendAsync(type, payload);
        }

        private static T Deserialize<T>(string json) =>
            json == null ? default : Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);

        // ---- Console diagnostics ----

        public void PrintConnectedPlayers()
        {
            if (_connections.IsEmpty) { Console.WriteLine("No players connected."); return; }
            foreach (var c in _connections.Values)
                Console.WriteLine($"  {c.Account.PlayerName}  world={c.CurrentWorldId ?? "(lobby)"}");
        }

        public void PrintWorlds()
        {
            foreach (var w in _worlds.All())
                Console.WriteLine($"  {w.Name} [{w.WorldId}]  builds={w.Builds.Count}  public={w.IsPublic}");
        }
    }
}
