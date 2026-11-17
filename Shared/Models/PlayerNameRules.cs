using System;

namespace SFSEnhanced.Shared.Models
{
    public static class PlayerNameRules
    {
        public const int MaxLength = 32;

        public static string Canonicalize(string playerName)
        {
            return (playerName ?? string.Empty).Trim();
        }

        public static bool TryCanonicalize(string playerName, out string canonical, out string error)
        {
            canonical = Canonicalize(playerName);
            error = null;

            if (canonical.Length == 0)
            {
                error = "A player name of 1-32 characters is required.";
                return false;
            }
            if (canonical.Length > MaxLength)
            {
                error = "A player name of 1-32 characters is required.";
                return false;
            }
            foreach (char c in canonical)
            {
                if (c < 0x20 || c == 0x7F)
                {
                    error = "A player name must not contain control characters.";
                    return false;
                }
            }
            return true;
        }

        public static bool Matches(string storedName, string candidateName)
        {
            return string.Equals(Canonicalize(storedName), Canonicalize(candidateName), StringComparison.OrdinalIgnoreCase);
        }
    }
}
