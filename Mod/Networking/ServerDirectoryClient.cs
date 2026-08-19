using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SFSEnhanced.Shared.Models;

namespace SFSEnhanced.Mod.Networking
{
    public sealed class ServerDirectoryClient
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        public async Task<List<ServerListing>> ListAsync(string directoryUrl)
        {
            if (string.IsNullOrWhiteSpace(directoryUrl)) return new List<ServerListing>();
            try
            {
                string url = directoryUrl.TrimEnd('/') + "/api/v1/servers";
                string json = await Http.GetStringAsync(url).ConfigureAwait(false);
                var response = JsonConvert.DeserializeObject<ServerDirectoryResponse>(json);
                return response?.Servers ?? new List<ServerListing>();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[SFSEnhanced] Server directory failed: {e.Message}");
                return new List<ServerListing>();
            }
        }
    }
}
