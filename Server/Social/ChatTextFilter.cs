using System;
using System.Text;

namespace SFSEnhanced.Server.Social
{
    public static class ChatTextFilter
    {
        public const int MaxLength = 512;
        public const int MaxConsecutiveWhitespace = 1;

        public static bool TryClean(string input, out string cleaned, out string reason)
        {
            cleaned = null;
            reason = null;
            if (string.IsNullOrWhiteSpace(input))
            {
                reason = "A chat message needs some text.";
                return false;
            }

            if (input.IndexOf('<') >= 0 || input.IndexOf('>') >= 0)
            {
                reason = "Chat messages cannot contain < or >. Remove the markup and send again.";
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
                if (char.IsControl(c) || c == '\n' || c == '\r' || c == '\t')
                {
                    if (!lastWasSpace && builder.Length > 0)
                    {
                        builder.Append(' ');
                        lastWasSpace = true;
                    }
                    continue;
                }
                if (char.IsWhiteSpace(c))
                {
                    if (lastWasSpace || builder.Length == 0) continue;
                    builder.Append(' ');
                    lastWasSpace = true;
                    continue;
                }
                builder.Append(c);
                lastWasSpace = false;
            }

            string result = builder.ToString().Trim();
            if (result.Length == 0)
            {
                reason = "That message is empty after removing unsupported characters.";
                return false;
            }
            if (result.Length > MaxLength)
            {
                reason = $"Chat messages are limited to {MaxLength} characters.";
                return false;
            }

            cleaned = result;
            return true;
        }
    }
}
