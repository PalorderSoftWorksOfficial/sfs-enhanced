using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SFSEnhanced.Server.Persistence;
using SFSEnhanced.Server.Social;
using SFSEnhanced.Server.World;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Server.Networking
{
    public partial class NetServer
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
