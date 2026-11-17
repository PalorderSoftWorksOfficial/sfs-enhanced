using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SFSEnhanced.Shared.Networking;

namespace SFSEnhanced.Mod.Networking
{
    internal static class ServerEndpointResolver
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        public static async Task<CanonicalEndpoint> ResolveAsync(string input, int? portOverride)
        {
            if (!EndpointKey.TryParseAuthority(input, portOverride, out CanonicalEndpoint endpoint, out string error))
                throw new ArgumentException(error);

            if (endpoint.PortExplicit) return endpoint;
            if (endpoint.Host.StartsWith("[", StringComparison.Ordinal)) return WithDefaultPort(endpoint);

            string service = "_sfs._tcp." + endpoint.Host;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://cloudflare-dns.com/dns-query?name=" + Uri.EscapeDataString(service) + "&type=SRV");
                request.Headers.Accept.ParseAdd("application/dns-json");
                using var response = await Http.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var json = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                var answer = json["Answer"]?.OfType<JObject>().FirstOrDefault(x => string.Equals(x.Value<string>("type"), "33", StringComparison.Ordinal));
                if (answer != null)
                {
                    var parts = (answer.Value<string>("data") ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 4 &&
                        int.TryParse(parts[2], out int srvPort) && srvPort >= 1 && srvPort <= 65535 &&
                        !string.IsNullOrWhiteSpace(parts[3]) && parts[3] != "." &&
                        EndpointKey.TryParseAuthority(parts[3], srvPort, out CanonicalEndpoint target, out _))
                    {
                        endpoint.Port = srvPort;
                        endpoint.ResolvedHost = target.Host;
                        endpoint.ResolvedPort = srvPort;
                        endpoint.TransportHost = target.TransportHost;
                        return endpoint;
                    }
                }
            }
            catch
            {
            }

            return WithDefaultPort(endpoint);
        }

        private static CanonicalEndpoint WithDefaultPort(CanonicalEndpoint endpoint)
        {
            endpoint.Port = 7777;
            endpoint.ResolvedHost = endpoint.Host;
            endpoint.ResolvedPort = 7777;
            return endpoint;
        }
    }
}
