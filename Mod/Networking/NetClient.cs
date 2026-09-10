using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using SFSEnhanced.Shared.Protocol;
using LidgrenClient = Lidgren.Network.NetClient;

namespace SFSEnhanced.Mod.Networking
{
    public class NetClient
    {
        private const string AppIdentifier = "SFS-Enhanced-Multiplayer";
        private LidgrenClient _peer;
        private NetConnection _connection;
        private CancellationTokenSource _cts;
        private TaskCompletionSource<bool> _connectTcs;
        private int _connectionGeneration;
        private volatile bool _connected;
        private SecureChannel _channel;
        private BlockingCollection<RawFrame> _pendingFrames = new BlockingCollection<RawFrame>();
        private readonly ConcurrentQueue<(PacketType type, string json)> _incoming = new();

        public string PlayerId { get; private set; }
        public string AuthToken { get; private set; }
        public string CurrentWorldId { get; private set; }
        public bool IsConnected => _connected;
        public event Action<PacketType, string> OnPacket;

        public NetClient()
        {
            AuthToken = SFSEnhanced.Mod.ModSettings.AuthToken;
        }

        public Task<bool> ConnectAsync(string host, int port, string playerName) => ConnectAsync(host, (int?)port, playerName);

        public async Task<bool> ConnectAsync(string host, int? port, string playerName, string registrationPassword = null)
        {
            Disconnect();
            try
            {
                int generation = Interlocked.Increment(ref _connectionGeneration);
                ServerEndpoint endpoint = await ServerEndpointResolver.ResolveAsync(host, port).ConfigureAwait(false);
                var config = new NetPeerConfiguration(AppIdentifier)
                {
                    ConnectionTimeout = 15f,
                    PingInterval = 4f,
                    MaximumConnections = 1
                };
                config.EnableMessageType(NetIncomingMessageType.StatusChanged);
                config.EnableMessageType(NetIncomingMessageType.Data);
                _peer = new LidgrenClient(config);
                _peer.Start();
                _cts = new CancellationTokenSource();
                _pendingFrames = new BlockingCollection<RawFrame>();
                _connectTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _connection = _peer.Connect(endpoint.Host, endpoint.Port);
                var pump = PumpAsync(generation, _cts.Token);
                if (await Task.WhenAny(_connectTcs.Task, Task.Delay(TimeSpan.FromSeconds(12))).ConfigureAwait(false) != _connectTcs.Task)
                    throw new TimeoutException("Timed out connecting to the multiplayer server.");
                if (!await _connectTcs.Task.ConfigureAwait(false)) throw new InvalidOperationException("The multiplayer transport rejected the connection.");

                var handshake = new SecureHandshakeClient(new LidgrenSecureTransport(this));
                SecureHandshakeResult result = await handshake.RunAsync(playerName, AuthToken, "0.1.0", registrationPassword, _cts.Token).ConfigureAwait(false);
                if (!result.Accepted) throw new InvalidOperationException(result.RejectReason ?? "Authentication failed.");

                _channel = result.Channel;
                PlayerId = result.PlayerId;
                if (!string.IsNullOrEmpty(result.IssuedToken))
                {
                    AuthToken = result.IssuedToken;
                    SFSEnhanced.Mod.ModSettings.AuthToken = result.IssuedToken;
                }
                _connected = true;
                _ = ConsumeFramesAsync(generation, _cts.Token);
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
            PlayerId = null;
            _channel = null;
            _connectTcs?.TrySetResult(false);
            try { _connection?.Disconnect("client disconnect"); } catch { }
            try { _peer?.Shutdown("client disconnect"); } catch { }
            _connection = null;
            _peer = null;
            _cts?.Cancel();
            _cts = null;
            _pendingFrames.CompleteAdding();
            while (_incoming.TryDequeue(out _)) { }
            while (_pendingFrames.TryTake(out _)) { }
        }

        public async Task LeaveWorldAsync()
        {
            if (!string.IsNullOrEmpty(CurrentWorldId) && IsConnected)
                await SendAsync(PacketType.WorldLeave, new { });
            CurrentWorldId = null;
        }

        public Task SendAsync(PacketType type, object payload)
        {
            LidgrenClient peer = _peer;
            NetConnection connection = _connection;
            SecureChannel channel = _channel;
            if (peer == null || connection == null || !_connected || channel == null) return Task.CompletedTask;
            try
            {
                byte[] sealedBytes = channel.Seal(type, payload);
                byte[] framed = SecureFrame.Wrap(new RawFrame { IsSealed = true, Data = sealedBytes });
                var message = peer.CreateMessage(framed.Length);
                message.Write(framed);
                NetDeliveryMethod method = LidgrenWire.IsHotStream(type) ? NetDeliveryMethod.UnreliableSequenced : NetDeliveryMethod.ReliableOrdered;
                peer.SendMessage(message, connection, method, LidgrenWire.GetChannel(type));
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[SFSEnhanced] Send failed ({type}): {e.Message}");
            }
            return Task.CompletedTask;
        }

        public void PumpIncoming()
        {
            while (_incoming.TryDequeue(out var item))
            {
                try { HandlePacket(item.type, item.json); }
                catch (Exception e) { UnityEngine.Debug.LogError($"[SFSEnhanced] Packet handling failed ({item.type}): {e}"); }
                try { OnPacket?.Invoke(item.type, item.json); }
                catch (Exception e) { UnityEngine.Debug.LogError($"[SFSEnhanced] Packet listener failed ({item.type}): {e}"); }
            }
        }

        private void HandlePacket(PacketType type, string json)
        {
            switch (type)
            {
                case PacketType.WorldJoinAck:
                    var worldAck = Newtonsoft.Json.JsonConvert.DeserializeObject<WorldJoinAckPacket>(json);
                    if (worldAck == null) throw new InvalidOperationException("Missing WorldJoinAck payload.");
                    if (worldAck.Accepted) CurrentWorldId = worldAck.WorldId;
                    break;
                case PacketType.AuthError:
                    var authError = Newtonsoft.Json.JsonConvert.DeserializeObject<AuthErrorPacket>(json);
                    UnityEngine.Debug.LogError($"[SFSEnhanced] Authentication failed: {authError?.Message}");
                    Disconnect();
                    break;
            }
        }

        private void EnqueueSealed(RawFrame frame)
        {
            SecureChannel channel = _channel;
            if (channel == null) return;
            if (channel.TryOpen(frame.Data, out PacketType type, out string json, out string error, out bool replayed))
            {
                if (type != PacketType.Ping) _incoming.Enqueue((type, json));
                return;
            }
            if (!replayed) UnityEngine.Debug.LogWarning($"[SFSEnhanced] Sealed packet rejected: {error}");
        }

        private async Task ConsumeFramesAsync(int generation, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && generation == Volatile.Read(ref _connectionGeneration))
                {
                    RawFrame frame = _pendingFrames.Take(ct);
                    if (frame.IsSealed) EnqueueSealed(frame);
                }
            }
            catch (OperationCanceledException) { }
            catch (InvalidOperationException) { }
        }

        private async Task PumpAsync(int generation, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    NetIncomingMessage message = _peer?.ReadMessage();
                    if (message == null)
                    {
                        await Task.Delay(1, ct).ConfigureAwait(false);
                        continue;
                    }
                    try
                    {
                        if (message.MessageType == NetIncomingMessageType.StatusChanged)
                        {
                            var status = (NetConnectionStatus)message.ReadByte();
                            if (status == NetConnectionStatus.Connected) _connectTcs?.TrySetResult(true);
                            if (status == NetConnectionStatus.Disconnected) _connectTcs?.TrySetResult(false);
                        }
                        else if (message.MessageType == NetIncomingMessageType.Data)
                        {
                            RawFrame frame = SecureFrame.Unwrap(message.ReadBytes(message.LengthBytes));
                            if (frame != null) _pendingFrames.TryAdd(frame);
                        }
                    }
                    finally
                    {
                        _peer?.Recycle(message);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                if (!ct.IsCancellationRequested) UnityEngine.Debug.LogWarning($"[SFSEnhanced] Connection lost: {e.Message}");
            }
            finally
            {
                if (!ct.IsCancellationRequested && generation == Volatile.Read(ref _connectionGeneration))
                {
                    _connected = false;
                    CurrentWorldId = null;
                    _connectTcs?.TrySetResult(false);
                }
            }
        }

        private sealed class LidgrenSecureTransport : ISecureTransport
        {
            private readonly NetClient _owner;

            public LidgrenSecureTransport(NetClient owner)
            {
                _owner = owner;
            }

            public Task SendPlainAsync(PacketType type, object payload, CancellationToken ct)
            {
                return SendFrame(new RawFrame { IsSealed = false, Data = LidgrenWire.Encode(type, payload) });
            }

            public Task SendSealedAsync(byte[] sealedBytes, CancellationToken ct)
            {
                return SendFrame(new RawFrame { IsSealed = true, Data = sealedBytes });
            }

            public Task<RawFrame> ReceiveRawAsync(CancellationToken ct)
            {
                try
                {
                    return Task.FromResult(_owner._pendingFrames.Take(ct));
                }
                catch (OperationCanceledException)
                {
                    return Task.FromResult<RawFrame>(null);
                }
                catch (InvalidOperationException)
                {
                    return Task.FromResult<RawFrame>(null);
                }
            }

            private Task SendFrame(RawFrame frame)
            {
                LidgrenClient peer = _owner._peer;
                NetConnection connection = _owner._connection;
                if (peer == null || connection == null) throw new InvalidOperationException("The transport is not connected.");
                byte[] data = SecureFrame.Wrap(frame);
                NetOutgoingMessage message = peer.CreateMessage(data.Length);
                message.Write(data);
                peer.SendMessage(message, connection, NetDeliveryMethod.ReliableOrdered, 0);
                return Task.CompletedTask;
            }
        }
    }
}
