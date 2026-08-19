using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Mod.Networking
{
    /// <summary>
    /// The game-side connection to an SFS Enhanced server. Talks the same
    /// Shared/Protocol wire format the dedicated server speaks. Incoming
    /// packets are queued and drained on the main thread via PumpIncoming()
    /// (called from ModMain.Update) since Unity APIs aren't thread-safe.
    /// </summary>
    public class NetClient
    {
        private TcpClient _tcp;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;

        public string PlayerId { get; private set; }
        public string AuthToken { get; private set; }
        public string CurrentWorldId { get; private set; }
        public bool IsConnected => _tcp?.Connected == true;

        private readonly ConcurrentQueue<(PacketType type, string json)> _incoming = new();

        // Subscribe from the mod's other systems (MultiBuildManager, FriendsUI, ...)
        public event Action<PacketType, string> OnPacket;

        public async Task<bool> ConnectAsync(string host, int port, string playerName)
        {
            try
            {
                _tcp = new TcpClient();
                await _tcp.ConnectAsync(host, port);
                _stream = _tcp.GetStream();
                _cts = new CancellationTokenSource();

                _ = ReadLoopAsync(_cts.Token);

                await NetMessage.WriteAsync(_stream, PacketType.Hello, new HelloPacket
                {
                    PlayerName = playerName,
                    AuthToken = AuthToken, // null on first connect; PlayerPrefs-load this in a real build
                    ClientModVersion = "0.1.0",
                });

                return true; // HelloAck arrives asynchronously via OnPacket; caller can await that event
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[SFSEnhanced] Connect failed: {e.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            try { _stream?.Dispose(); } catch { }
            try { _tcp?.Close(); } catch { }
        }

        public async Task SendAsync(PacketType type, object payload)
        {
            if (!IsConnected) return;
            await NetMessage.WriteAsync(_stream, type, payload);
        }

        /// <summary>Drain queued packets on the main/Unity thread. Call once per frame.</summary>
        public void PumpIncoming()
        {
            while (_incoming.TryDequeue(out var item))
            {
                if (item.type == PacketType.HelloAck)
                {
                    var ack = Newtonsoft.Json.JsonConvert.DeserializeObject<HelloAckPacket>(item.json);
                    if (ack.Accepted)
                    {
                        PlayerId = ack.PlayerId;
                        if (!string.IsNullOrEmpty(ack.AuthToken)) AuthToken = ack.AuthToken;
                    }
                }
                else if (item.type == PacketType.WorldJoinAck)
                {
                    var ack = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldJoinAckPacket>(item.json);
                    if (ack.Accepted) CurrentWorldId = ack.WorldId;
                }

                OnPacket?.Invoke(item.type, item.json);
            }
        }

        private async Task ReadLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var (type, json) = await NetMessage.ReadRawAsync(_stream);
                    if (json == null) break; // server closed the connection
                    _incoming.Enqueue((type, json));
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[SFSEnhanced] Connection lost: {e.Message}");
            }
        }
    }
}
