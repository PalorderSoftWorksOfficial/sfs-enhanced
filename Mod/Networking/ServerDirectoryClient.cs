using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SFSEnhanced.Shared.Models;

namespace SFSEnhanced.Mod.Networking
{
    public sealed class ServerDirectoryResult
    {
        public List<ServerListing> Servers = new List<ServerListing>();
        public string Error;
        public bool Succeeded => Error == null;
    }

    public sealed class ServerDirectoryClient
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        public async Task<ServerDirectoryResult> ListAsync(string directoryUrl)
        {
            var result = new ServerDirectoryResult();
            if (string.IsNullOrWhiteSpace(directoryUrl))
            {
                result.Error = "No server directory URL is configured.";
                return result;
            }

            try
            {
                string url = directoryUrl.TrimEnd('/') + "/api/v1/servers";
                using HttpResponseMessage response = await Http.GetAsync(url).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    result.Error = $"The directory answered HTTP {(int)response.StatusCode} ({response.ReasonPhrase}). It may be down or misconfigured.";
                    return result;
                }

                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var parsed = JsonConvert.DeserializeObject<ServerDirectoryResponse>(json);
                if (parsed == null)
                {
                    result.Error = "The directory returned a response SFS Enhanced could not read.";
                    return result;
                }
                result.Servers = parsed.Servers ?? new List<ServerListing>();
                return result;
            }
            catch (TaskCanceledException)
            {
                result.Error = "The directory did not respond in time. It may be offline or unreachable.";
                return result;
            }
            catch (HttpRequestException e)
            {
                result.Error = "Could not reach the directory: " + e.Message;
                return result;
            }
            catch (JsonException)
            {
                result.Error = "The directory returned a response SFS Enhanced could not read. Check that the URL points at an SFS Enhanced directory.";
                return result;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[SFSEnhanced] Server directory failed: {e.Message}");
                result.Error = "The directory request failed: " + e.Message;
                return result;
            }
        }
    }
}
