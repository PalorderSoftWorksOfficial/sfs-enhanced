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
        private int _connectionGeneration;
        private volatile bool _connected;

        public string PlayerId { get; private set; }
        public string AuthToken { get; private set; }
        public string CurrentWorldId { get; private set; }
        public bool IsConnected => _connected;

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
                var generation = Interlocked.Increment(ref _connectionGeneration);
                var tcp = new TcpClient();
                await tcp.ConnectAsync(host, port);
                var stream = tcp.GetStream();
                var cts = new CancellationTokenSource();
                _tcp = tcp;
                _stream = stream;
                _cts = cts;
                _connected = true;

                await SendAsync(PacketType.Hello, new HelloPacket
                {
                    PlayerName = playerName,
                    AuthToken = AuthToken,
                    ClientModVersion = "0.1.0",
                });

                _ = ReadLoopAsync(stream, cts.Token, generation);
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
            Interlocked.Increment(ref _connectionGeneration);
            _connected = false;
            CurrentWorldId = null;
            _cts?.Cancel();
            try { _stream?.Dispose(); } catch { }
            try { _tcp?.Close(); } catch { }
            _stream = null;
            _tcp = null;
            _cts = null;
            while (_incoming.TryDequeue(out _)) { }
        }

        public async Task SendAsync(PacketType type, object payload)
        {
            var stream = _stream;
            if (stream == null || !IsConnected) return;
            await _writeLock.WaitAsync();
            try
            {
                if (!IsConnected || !ReferenceEquals(stream, _stream)) return;
                await NetMessage.WriteAsync(stream, type, payload);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[SFSEnhanced] Send failed ({type}): {e.Message}");
                Disconnect();
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
                try
                {
                    HandlePacket(item.type, item.json);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[SFSEnhanced] Packet handling failed ({item.type}): {e}");
                }

                try
                {
                    OnPacket?.Invoke(item.type, item.json);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[SFSEnhanced] Packet listener failed ({item.type}): {e}");
                }
            }
        }

        private void HandlePacket(PacketType type, string json)
        {
            switch (type)
            {
                case PacketType.HelloAck:
                    var ack = Newtonsoft.Json.JsonConvert.DeserializeObject<HelloAckPacket>(json);
                    if (ack == null) throw new InvalidOperationException("Missing HelloAck payload.");
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
                    break;
                case PacketType.WorldJoinAck:
                    var worldAck = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldJoinAckPacket>(json);
                    if (worldAck == null) throw new InvalidOperationException("Missing WorldJoinAck payload.");
                    if (worldAck.Accepted) CurrentWorldId = worldAck.WorldId;
                    break;
            }
        }

        private async Task ReadLoopAsync(NetworkStream stream, CancellationToken ct, int generation)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var (type, json) = await NetMessage.ReadRawAsync(stream);
                    if (json == null || generation != Volatile.Read(ref _connectionGeneration)) break;
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
                if (!ct.IsCancellationRequested && generation == Volatile.Read(ref _connectionGeneration))
                {
                    _connected = false;
                    CurrentWorldId = null;
                    try { stream.Dispose(); } catch { }
                    try { _tcp?.Close(); } catch { }
                    _stream = null;
                    _tcp = null;
                }
            }
        }
    }
}
