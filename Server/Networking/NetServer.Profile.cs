using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Server.Networking
{
    public partial class NetServer
    {
        private const double ProfileUpdateBurst = 5.0;
        private const double ProfileUpdatesPerSecond = 1.0;

        private readonly ConcurrentDictionary<string, ProfileUpdateBucket> _profileBuckets = new ConcurrentDictionary<string, ProfileUpdateBucket>();

        private sealed class ProfileUpdateBucket
        {
            public double Tokens = ProfileUpdateBurst;
            public DateTime RefilledUtc = DateTime.UtcNow;
        }

        private async Task HandleProfileRequest(ClientConnection conn, ProfileRequestPacket request)
        {
            PlayerAccount viewer = conn.Account;
            if (viewer == null) return;

            string targetKey = request == null || string.IsNullOrWhiteSpace(request.PlayerId) ? viewer.PlayerId : request.PlayerId;
            PlayerAccount target = _profiles.Resolve(targetKey);
            if (target == null)
            {
                await conn.SendAsync(PacketType.ProfileResponse, new ProfileResponsePacket
                {
                    Found = false,
                    Reason = "No player with that name or id on this server."
                });
                return;
            }

            bool online = _connections.TryGetValue(target.PlayerId, out ClientConnection targetConnection);
            await conn.SendAsync(PacketType.ProfileResponse, new ProfileResponsePacket
            {
                Found = true,
                Profile = _profiles.Build(target, viewer.PlayerId, online, online ? targetConnection.CurrentWorldId : null)
            });
        }

        private async Task HandleProfileUpdate(ClientConnection conn, ProfileUpdatePacket update)
        {
            PlayerAccount account = conn.Account;
            if (account == null) return;

            if (!TryTakeProfileToken(account.PlayerId))
            {
                await conn.SendAsync(PacketType.ProfileUpdateResult, new ProfileUpdateResultPacket
                {
                    Accepted = false,
                    Reason = "Profile updates are rate limited. Try again in a moment."
                });
                return;
            }

            if (!_profiles.TryApply(account, update, out string reason))
            {
                RefundProfileToken(account.PlayerId);
                await conn.SendAsync(PacketType.ProfileUpdateResult, new ProfileUpdateResultPacket
                {
                    Accepted = false,
                    Reason = reason
                });
                return;
            }

            await conn.SendAsync(PacketType.ProfileUpdateResult, new ProfileUpdateResultPacket
            {
                Accepted = true,
                Profile = _profiles.Build(account, account.PlayerId, true, conn.CurrentWorldId)
            });
        }

        private void RefundProfileToken(string playerId)
        {
            if (!_profileBuckets.TryGetValue(playerId, out ProfileUpdateBucket bucket)) return;
            lock (bucket)
            {
                bucket.Tokens = Math.Min(ProfileUpdateBurst, bucket.Tokens + 1.0);
            }
        }

        private bool TryTakeProfileToken(string playerId)
        {
            ProfileUpdateBucket bucket = _profileBuckets.GetOrAdd(playerId, _ => new ProfileUpdateBucket());
            lock (bucket)
            {
                DateTime now = DateTime.UtcNow;
                double refill = Math.Max(0.0, (now - bucket.RefilledUtc).TotalSeconds) * ProfileUpdatesPerSecond;
                bucket.Tokens = Math.Min(ProfileUpdateBurst, bucket.Tokens + refill);
                bucket.RefilledUtc = now;
                if (bucket.Tokens < 1.0) return false;
                bucket.Tokens -= 1.0;
                return true;
            }
        }
    }
}
