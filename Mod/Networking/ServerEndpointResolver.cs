using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SFSEnhanced.Mod.Networking
{
    internal sealed class ServerEndpoint
    {
        public string Host { get; set; }
        public int Port { get; set; }
    }

    internal static class ServerEndpointResolver
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        public static async Task<ServerEndpoint> ResolveAsync(string input, int? portOverride)
        {
            if (string.IsNullOrWhiteSpace(input)) throw new ArgumentException("Server address is required.");
            string host = input.Trim();
            if (Uri.TryCreate("sfs://" + host, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host))
                host = uri.Host;

            if (portOverride.HasValue)
            {
                if (portOverride.Value < 1 || portOverride.Value > 65535) throw new ArgumentOutOfRangeException(nameof(portOverride));
                return new ServerEndpoint { Host = host, Port = portOverride.Value };
            }

            string service = "_sfs._tcp." + host.TrimEnd('.');
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
                    if (parts.Length == 4 && int.TryParse(parts[2], out var srvPort) && srvPort > 0 && srvPort <= 65535)
                        return new ServerEndpoint { Host = parts[3].TrimEnd('.'), Port = srvPort };
                }
            }
            catch
            {
            }

            return new ServerEndpoint { Host = host, Port = 7777 };
        }
    }
}
