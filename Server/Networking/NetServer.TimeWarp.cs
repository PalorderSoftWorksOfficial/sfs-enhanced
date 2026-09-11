using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Server.Networking
{
    public partial class NetServer
    {
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
                if (string.Equals(builds[i].OwnerPlayerId, builds[j].OwnerPlayerId, StringComparison.Ordinal)) continue;
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
    }
}
