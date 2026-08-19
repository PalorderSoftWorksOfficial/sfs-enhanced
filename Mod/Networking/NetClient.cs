using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Mod.Networking
{
    public class NetClient
    {
        private TcpClient _tcp;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

        public string PlayerId { get; private set; }
        public string AuthToken { get; private set; }
        public string CurrentWorldId { get; private set; }
        public bool IsConnected => _tcp?.Connected == true;

        private readonly ConcurrentQueue<(PacketType type, string json)> _incoming = new ConcurrentQueue<(PacketType type, string json)>();

        public event Action<PacketType, string> OnPacket;

        public NetClient()
        {
            AuthToken = SFSEnhanced.Mod.ModSettings.AuthToken;
        }

        public async Task<bool> ConnectAsync(string host, int port, string playerName)
        {
            Disconnect();

            try
            {
                _tcp = new TcpClient();
                await _tcp.ConnectAsync(host, port);
                _stream = _tcp.GetStream();
                _cts = new CancellationTokenSource();

                await SendAsync(PacketType.Hello, new HelloPacket
                {
                    PlayerName = playerName,
                    AuthToken = AuthToken,
                    ClientModVersion = "0.1.0",
                });

                _ = ReadLoopAsync(_cts.Token);
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[SFSEnhanced] Connect failed: {e.Message}");
                Disconnect();
                return false;
            }
        }

        public void Disconnect()
        {
            CurrentWorldId = null;
            _cts?.Cancel();
            try { _stream?.Dispose(); } catch { }
            try { _tcp?.Close(); } catch { }
            _stream = null;
            _tcp = null;
        }

        public async Task SendAsync(PacketType type, object payload)
        {
            if (_stream == null || !IsConnected) return;
            await _writeLock.WaitAsync();
            try
            {
                await NetMessage.WriteAsync(_stream, type, payload);
            }
            finally
            {
                _writeLock.Release();
            }
        }

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
                        if (!string.IsNullOrEmpty(ack.AuthToken))
                        {
                            AuthToken = ack.AuthToken;
                            SFSEnhanced.Mod.ModSettings.AuthToken = ack.AuthToken;
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"[SFSEnhanced] Login rejected: {ack.RejectReason}");
                    }
                }
                else if (item.type == PacketType.WorldJoinAck)
                {
                    var ack = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldJoinAckPacket>(item.json);
                    if (ack.Accepted)
                        CurrentWorldId = ack.WorldId;
                }

                OnPacket?.Invoke(item.type, item.json);
            }
        }

        private async Task ReadLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _stream != null)
                {
                    var (type, json) = await NetMessage.ReadRawAsync(_stream);
                    if (json == null) break;
                    _incoming.Enqueue((type, json));
                }
            }
            catch (Exception e)
            {
                if (!ct.IsCancellationRequested)
                    UnityEngine.Debug.LogWarning($"[SFSEnhanced] Connection lost: {e.Message}");
            }
            finally
            {
                if (!ct.IsCancellationRequested)
                {
                    CurrentWorldId = null;
                    try { _tcp?.Close(); } catch { }
                }
            }
        }
    }
}
