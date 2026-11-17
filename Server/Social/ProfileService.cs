using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SFSEnhanced.Server.Persistence;
using SFSEnhanced.Server.World;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Server.Social
{
    public class ProfileService
    {
        public const int MaxDisplayNameLength = 32;
        public const int MaxBioLength = 280;
        public const string DefaultAccent = "#4FA8FF";

        private static readonly Regex AccentPattern = new Regex("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

        private readonly AccountService _accounts;
        private readonly WorldManager _worlds;

        public ProfileService(AccountService accounts, WorldManager worlds)
        {
            _accounts = accounts;
            _worlds = worlds;
        }

        public static string DisplayNameOf(PlayerAccount account)
        {
            if (account == null) return "Pilot";
            return string.IsNullOrWhiteSpace(account.DisplayName) ? account.PlayerName : account.DisplayName;
        }

        public static string AccentOf(PlayerAccount account)
        {
            string accent = account == null ? null : account.AvatarAccent;
            return !string.IsNullOrEmpty(accent) && AccentPattern.IsMatch(accent) ? accent.ToUpperInvariant() : DefaultAccent;
        }

        public PlayerAccount Resolve(string playerIdOrName)
        {
            if (string.IsNullOrWhiteSpace(playerIdOrName)) return null;
            string key = playerIdOrName.Trim();
            return _accounts.FindById(key) ?? _accounts.FindByName(key);
        }

        public PlayerProfile Build(PlayerAccount account, string viewerPlayerId, bool online, string currentWorldId)
        {
            if (account == null) return null;

            bool isSelf = string.Equals(account.PlayerId, viewerPlayerId, StringComparison.Ordinal);
            ProfilePrivacy privacy = account.Privacy ?? new ProfilePrivacy();
            bool presenceVisible = isSelf || privacy.ShowPresence;

            return new PlayerProfile
            {
                PlayerId = account.PlayerId,
                PlayerName = account.PlayerName,
                DisplayName = DisplayNameOf(account),
                Bio = account.Bio ?? string.Empty,
                AvatarAccent = AccentOf(account),
                MemberSinceUtc = account.FirstSeenUtc,
                LastSeenUtc = presenceVisible ? account.LastSeenUtc : default(DateTime),
                Online = presenceVisible && online,
                CurrentWorldId = presenceVisible && online ? currentWorldId : null,
                IsSelf = isSelf,
                ShowPresence = privacy.ShowPresence,
                AllowFriendRequests = privacy.AllowFriendRequests,
                WorldsOwned = _worlds.All().Count(w => string.Equals(w.OwnerPlayerId, account.PlayerId, StringComparison.Ordinal)),
                FriendsCount = account.FriendPlayerIds == null ? 0 : account.FriendPlayerIds.Count,
                SessionsCount = account.SessionsCount,
            };
        }

        public bool TryApply(PlayerAccount account, ProfileUpdatePacket update, out string reason)
        {
            reason = null;
            if (account == null)
            {
                reason = "No authenticated account.";
                return false;
            }
            if (update == null)
            {
                reason = "Empty profile update.";
                return false;
            }

            string displayName = account.DisplayName;
            string bio = account.Bio;
            string accent = account.AvatarAccent;
            ProfilePrivacy privacy = account.Privacy ?? (account.Privacy = new ProfilePrivacy());

            if (update.DisplayName != null)
            {
                string cleaned = SingleLine(update.DisplayName);
                if (cleaned.Length == 0)
                {
                    reason = "Display name cannot be empty.";
                    return false;
                }
                if (cleaned.Length > MaxDisplayNameLength)
                {
                    reason = $"Display name must be {MaxDisplayNameLength} characters or fewer.";
                    return false;
                }
                displayName = cleaned;
            }

            if (update.Bio != null)
            {
                string cleaned = Block(update.Bio);
                if (cleaned.Length > MaxBioLength)
                {
                    reason = $"Bio must be {MaxBioLength} characters or fewer.";
                    return false;
                }
                bio = cleaned;
            }

            if (update.AvatarAccent != null)
            {
                string cleaned = update.AvatarAccent.Trim();
                if (!AccentPattern.IsMatch(cleaned))
                {
                    reason = "Accent must be a #RRGGBB colour.";
                    return false;
                }
                accent = cleaned.ToUpperInvariant();
            }

            if (update.AllowFriendRequests.HasValue) privacy.AllowFriendRequests = update.AllowFriendRequests.Value;
            if (update.ShowPresence.HasValue) privacy.ShowPresence = update.ShowPresence.Value;

            account.DisplayName = displayName;
            account.Bio = bio;
            account.AvatarAccent = accent;
            account.ProfileUpdatedUtc = DateTime.UtcNow;
            _accounts.Save(account);
            return true;
        }

        private static string SingleLine(string value) =>
            Strip(value).Replace('\r', ' ').Replace('\n', ' ').Trim();

        private static string Block(string value) => Strip(value).Trim();

        private static string Strip(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var builder = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (c == '<' || c == '>') continue;
                if (c >= '\u200B' && c <= '\u200F') continue;
                if (c >= '\u202A' && c <= '\u202E') continue;
                if (c >= '\u2066' && c <= '\u2069') continue;
                if (c == '\uFEFF') continue;
                if (!char.IsControl(c) || c == '\n' || c == '\r') builder.Append(c);
            }
            return builder.ToString();
        }
    }
}
