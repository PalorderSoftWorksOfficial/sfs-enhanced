using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SFSEnhanced.Shared.Models;

namespace SFSEnhanced.Server.Networking
{
    public sealed class ServerDirectoryPublisher
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        private readonly ServerConfig _config;
        private string _serverId;
        private string _heartbeatToken;

        public ServerDirectoryPublisher(ServerConfig config)
        {
            _config = config;
        }

        public async Task RunAsync(NetServer server, CancellationToken ct)
        {
            if (!_config.Advertise || string.IsNullOrWhiteSpace(_config.DirectoryUrl) || string.IsNullOrWhiteSpace(_config.PublicHost))
                return;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(_serverId) || string.IsNullOrWhiteSpace(_heartbeatToken))
                        await RegisterAsync().ConfigureAwait(false);

                    await HeartbeatAsync(server).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(20), ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[directory] {ex.Message}. Retrying in 10 seconds.");
                    _serverId = null;
                    _heartbeatToken = null;
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            await UnregisterAsync().ConfigureAwait(false);
        }

        private async Task RegisterAsync()
        {
            var request = new ServerRegisterRequest
            {
                Name = _config.ServerName,
                Host = _config.PublicHost,
                Port = _config.Port,
                Region = _config.Region,
                GameVersion = _config.GameVersion,
                ModVersion = _config.ModVersion,
                Motd = _config.Motd,
                MaxPlayers = _config.MaxPlayers,
                PasswordProtected = _config.PasswordProtected
            };
            var response = await PostAsync<ServerRegisterResponse>("/api/v1/servers/register", request).ConfigureAwait(false);
            if (response == null || string.IsNullOrWhiteSpace(response.ServerId) || string.IsNullOrWhiteSpace(response.HeartbeatToken))
                throw new InvalidOperationException("Directory returned an invalid registration response.");

            _serverId = response.ServerId;
            _heartbeatToken = response.HeartbeatToken;
            Console.WriteLine($"[directory] Registered as {_serverId}");
        }

        private async Task HeartbeatAsync(NetServer server)
        {
            var response = await PostAsync<object>("/api/v1/servers/heartbeat", new ServerHeartbeatRequest
            {
                ServerId = _serverId,
                HeartbeatToken = _heartbeatToken,
                OnlinePlayers = server.ConnectedPlayerCount,
                MaxPlayers = _config.MaxPlayers,
                Motd = _config.Motd
            }).ConfigureAwait(false);
        }

        private async Task UnregisterAsync()
        {
            if (string.IsNullOrWhiteSpace(_serverId) || string.IsNullOrWhiteSpace(_heartbeatToken))
                return;

            try
            {
                await PostAsync<object>("/api/v1/servers/unregister", new ServerHeartbeatRequest
                {
                    ServerId = _serverId,
                    HeartbeatToken = _heartbeatToken,
                    MaxPlayers = _config.MaxPlayers
                }).ConfigureAwait(false);
            }
            catch { }

            _serverId = null;
            _heartbeatToken = null;
        }

        private async Task<T> PostAsync<T>(string path, object payload)
        {
            string json = JsonConvert.SerializeObject(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await Http.PostAsync(_config.DirectoryUrl.TrimEnd('/') + path, content).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (typeof(T) == typeof(object)) return default;
            return JsonConvert.DeserializeObject<T>(body);
        }
    }
}
