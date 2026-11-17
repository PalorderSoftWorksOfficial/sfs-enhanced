using System.Linq;
using SFSEnhanced.Shared.Networking;

namespace SFSEnhanced.Mod.Networking
{
    internal static class TokenMigration
    {
        public static string LastSuccessfulEndpointKey()
        {
            var candidates = ServerHistory.GetAll()
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Host))
                .Select(x => new EndpointHistoryCandidate
                {
                    Host = x.Host,
                    Port = x.Port,
                    LastConnectedUtc = x.LastConnectedUtc
                })
                .ToList();
            return EndpointKey.LastSuccessfulKey(candidates);
        }

        public static string EndpointKeyFor(string host, int? port)
        {
            if (string.IsNullOrWhiteSpace(host)) return null;
            return EndpointKey.TryParseAuthority(host, port, out CanonicalEndpoint endpoint, out _) ? endpoint.Key : null;
        }
    }
}
