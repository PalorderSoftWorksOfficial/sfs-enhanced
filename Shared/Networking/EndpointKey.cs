using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace SFSEnhanced.Shared.Networking
{
    public sealed class CanonicalEndpoint
    {
        public string Key;
        public string Host;
        public string TransportHost;
        public int Port;
        public bool PortExplicit;
        public string ResolvedHost;
        public int? ResolvedPort;

        public string DisplayAuthority => PortExplicit ? Key : Host;
    }

    public sealed class EndpointHistoryCandidate
    {
        public string Host;
        public int? Port;
        public DateTime LastConnectedUtc;
    }

    public static class EndpointKey
    {
        public const string AutoSuffix = "#auto";

        public static string LastSuccessfulKey(IEnumerable<EndpointHistoryCandidate> candidates)
        {
            if (candidates == null) return null;
            EndpointHistoryCandidate latest = null;
            foreach (EndpointHistoryCandidate candidate in candidates)
            {
                if (candidate == null || string.IsNullOrWhiteSpace(candidate.Host)) continue;
                if (latest == null || candidate.LastConnectedUtc > latest.LastConnectedUtc) latest = candidate;
            }
            if (latest == null) return null;
            return TryParseAuthority(latest.Host, latest.Port, out CanonicalEndpoint endpoint, out _) ? endpoint.Key : null;
        }

        public static string ForAutoPort(string canonicalHost) => canonicalHost + AutoSuffix;

        public static string ForExplicitPort(string canonicalHost, int port) => canonicalHost + ":" + port.ToString(CultureInfo.InvariantCulture);

        public static bool TryParseAuthority(string input, int? portOverride, out CanonicalEndpoint endpoint, out string error)
        {
            endpoint = null;
            error = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                error = "A server address is required.";
                return false;
            }

            string raw = input.Trim();
            foreach (char c in raw)
            {
                if (char.IsWhiteSpace(c) || c < 0x20 || c == 0x7F)
                {
                    error = "The server address must not contain spaces or control characters.";
                    return false;
                }
                if (c == '@' || c == '?' || c == '#' || c == '\\' || c == '"' || c == '<' || c == '>' || c == '|')
                {
                    error = "The server address contains a character that is not allowed in a host name.";
                    return false;
                }
            }

            int schemeSplit = raw.IndexOf("://", StringComparison.Ordinal);
            if (schemeSplit >= 0)
            {
                string scheme = raw.Substring(0, schemeSplit);
                if (!string.Equals(scheme, "sfs", StringComparison.OrdinalIgnoreCase))
                {
                    error = "Unsupported address scheme '" + scheme + "'. Use a plain host name or sfs://.";
                    return false;
                }
                raw = raw.Substring(schemeSplit + 3);
            }

            raw = raw.TrimEnd('/');
            if (raw.Length == 0)
            {
                error = "A server host is required.";
                return false;
            }
            if (raw.IndexOf('/') >= 0)
            {
                error = "The server address must not contain a path.";
                return false;
            }

            string hostPart;
            int? addressPort = null;

            if (raw[0] == '[')
            {
                int close = raw.IndexOf(']');
                if (close < 0)
                {
                    error = "The IPv6 address is missing a closing bracket.";
                    return false;
                }
                string literal = raw.Substring(1, close - 1);
                string remainder = raw.Substring(close + 1);
                if (remainder.Length > 0)
                {
                    if (remainder[0] != ':' || remainder.Length == 1)
                    {
                        error = "The IPv6 address is followed by invalid text. Use [address]:port.";
                        return false;
                    }
                    if (!TryParsePort(remainder.Substring(1), out int parsedPort))
                    {
                        error = "The port must be between 1 and 65535.";
                        return false;
                    }
                    addressPort = parsedPort;
                }
                if (!TryParseIPv6(literal, out string canonical6))
                {
                    error = "The IPv6 address is invalid.";
                    return false;
                }
                hostPart = "[" + canonical6 + "]";
            }
            else
            {
                int colonCount = 0;
                foreach (char c in raw) if (c == ':') colonCount++;

                if (colonCount > 1)
                {
                    if (!TryParseIPv6(raw, out string canonical6))
                    {
                        error = "The address contains more than one colon but is not a valid IPv6 address.";
                        return false;
                    }
                    hostPart = "[" + canonical6 + "]";
                }
                else if (colonCount == 1)
                {
                    int separator = raw.IndexOf(':');
                    hostPart = raw.Substring(0, separator);
                    if (!TryParsePort(raw.Substring(separator + 1), out int parsedPort))
                    {
                        error = "The port must be between 1 and 65535.";
                        return false;
                    }
                    addressPort = parsedPort;
                }
                else
                {
                    hostPart = raw;
                }
            }

            if (portOverride.HasValue && (portOverride.Value < 1 || portOverride.Value > 65535))
            {
                error = "The port must be between 1 and 65535.";
                return false;
            }
            if (addressPort.HasValue && portOverride.HasValue && addressPort.Value != portOverride.Value)
            {
                error = "The address specifies port " + addressPort.Value + " but the port field says " + portOverride.Value + ". Keep only one of them.";
                return false;
            }

            int? effectivePort = addressPort ?? portOverride;

            string canonicalHost;
            string transportHost;
            if (hostPart.StartsWith("[", StringComparison.Ordinal))
            {
                canonicalHost = hostPart;
                transportHost = hostPart.Substring(1, hostPart.Length - 2);
            }
            else
            {
                string host = hostPart;
                if (host.EndsWith(".", StringComparison.Ordinal)) host = host.Substring(0, host.Length - 1);
                if (host.Length == 0)
                {
                    error = "A server host is required.";
                    return false;
                }

                if (IPAddress.TryParse(host, out IPAddress address) && address.AddressFamily == AddressFamily.InterNetwork)
                {
                    canonicalHost = address.ToString();
                    transportHost = canonicalHost;
                }
                else
                {
                    host = host.ToLowerInvariant();
                    try
                    {
                        host = new IdnMapping { UseStd3AsciiRules = true }.GetAscii(host);
                    }
                    catch (ArgumentException)
                    {
                        error = "The server host name is not a valid internationalized domain name.";
                        return false;
                    }
                    if (host.Length > 253)
                    {
                        error = "The server host name is too long.";
                        return false;
                    }
                    foreach (string label in host.Split('.'))
                    {
                        if (!IsValidHostLabel(label))
                        {
                            error = "The server host name has an invalid label. Use letters, digits and hyphens only.";
                            return false;
                        }
                    }
                    canonicalHost = host;
                    transportHost = host;
                }
            }

            endpoint = new CanonicalEndpoint
            {
                Host = canonicalHost,
                TransportHost = transportHost,
                Port = effectivePort ?? 0,
                PortExplicit = effectivePort.HasValue,
                Key = effectivePort.HasValue
                    ? ForExplicitPort(canonicalHost, effectivePort.Value)
                    : ForAutoPort(canonicalHost)
            };
            return true;
        }

        private static bool IsValidHostLabel(string label)
        {
            if (string.IsNullOrEmpty(label) || label.Length > 63) return false;
            if (label[0] == '-' || label[label.Length - 1] == '-') return false;
            foreach (char c in label)
            {
                bool allowed = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-';
                if (!allowed) return false;
            }
            return true;
        }

        private static bool TryParsePort(string text, out int port)
        {
            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out port) && port >= 1 && port <= 65535;
        }

        private static bool TryParseIPv6(string text, out string canonical)
        {
            canonical = null;
            if (!IPAddress.TryParse(text, out IPAddress address) || address.AddressFamily != AddressFamily.InterNetworkV6) return false;
            canonical = address.ToString().ToLowerInvariant();
            return true;
        }
    }
}
