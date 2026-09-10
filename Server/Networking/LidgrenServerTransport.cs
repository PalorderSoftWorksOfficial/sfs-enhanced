using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using SFSEnhanced.Shared.Protocol;
using LidgrenNetServer = Lidgren.Network.NetServer;

namespace SFSEnhanced.Server.Networking
{
    public sealed class LidgrenServerTransport
    {
        private const string AppIdentifier = "SFS-Enhanced-Multiplayer";
        private readonly LidgrenNetServer _peer;
        private readonly ConcurrentDictionary<NetConnection, LidgrenServerConnection> _connections = new();
        public event Action<LidgrenServerConnection> ConnectionAccepted;

        public LidgrenServerTransport(int port, int maximumConnections)
        {
            var config = new NetPeerConfiguration(AppIdentifier)
            {
                Port = port,
                MaximumConnections = maximumConnections,
                ConnectionTimeout = 15f,
                PingInterval = 4f
            };
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.StatusChanged);
            config.EnableMessageType(NetIncomingMessageType.Data);
            _peer = new LidgrenNetServer(config);
        }

        public Task StartAsync(CancellationToken ct)
        {
            _peer.Start();
            return PumpAsync(ct);
        }

        public void Stop()
        {
            try { _peer.Shutdown("server shutdown"); } catch { }
            foreach (var connection in _connections.Values) connection.Complete();
            _connections.Clear();
        }

        public bool TryGet(NetConnection connection, out LidgrenServerConnection session) => _connections.TryGetValue(connection, out session);

        private async Task PumpAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var message = _peer.ReadMessage();
                    if (message == null)
                    {
                        await Task.Delay(1, ct).ConfigureAwait(false);
                        continue;
                    }
                    try
                    {
                        switch (message.MessageType)
                        {
                            case NetIncomingMessageType.ConnectionApproval:
                                message.SenderConnection.Approve();
                                break;
                            case NetIncomingMessageType.StatusChanged:
                                HandleStatus(message);
                                break;
                            case NetIncomingMessageType.Data:
                                if (_connections.TryGetValue(message.SenderConnection, out var session))
                                    session.Enqueue(message.ReadBytes(message.LengthBytes));
                                break;
                        }
                    }
                    finally
                    {
                        _peer.Recycle(message);
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                Stop();
            }
        }

        private void HandleStatus(NetIncomingMessage message)
        {
            var status = (NetConnectionStatus)message.ReadByte();
            if (status == NetConnectionStatus.Connected)
            {
                var session = new LidgrenServerConnection(_peer, message.SenderConnection);
                if (_connections.TryAdd(message.SenderConnection, session)) ConnectionAccepted?.Invoke(session);
            }
            else if (status == NetConnectionStatus.Disconnected)
            {
                if (_connections.TryRemove(message.SenderConnection, out var session)) session.Complete();
            }
        }
    }

    public sealed class LidgrenServerConnection
    {
        private readonly NetPeer _peer;
        private readonly NetConnection _connection;
        private readonly BlockingCollection<byte[]> _incoming = new();

        public LidgrenServerConnection(NetPeer peer, NetConnection connection)
        {
            _peer = peer;
            _connection = connection;
        }

        public Task<byte[]> ReceiveAsync(CancellationToken ct)
        {
            return Task.Run(() =>
            {
                if (_incoming.IsCompleted) return null;
                try { return _incoming.Take(ct); }
                catch (InvalidOperationException) { return null; }
                catch (OperationCanceledException) { return null; }
            }, ct);
        }

        public Task SendFrameAsync(byte[] framedData)
        {
            var message = _peer.CreateMessage(framedData.Length);
            message.Write(framedData);
            _peer.SendMessage(message, _connection, NetDeliveryMethod.ReliableOrdered, 0);
            return Task.CompletedTask;
        }

        public Task SendSealedAsync(PacketType type, byte[] sealedBytes)
        {
            var data = SecureFrame.Wrap(new RawFrame { IsSealed = true, Data = sealedBytes });
            var message = _peer.CreateMessage(data.Length);
            message.Write(data);
            var method = LidgrenWire.IsHotStream(type) ? NetDeliveryMethod.UnreliableSequenced : NetDeliveryMethod.ReliableOrdered;
            _peer.SendMessage(message, _connection, method, 0);
            return Task.CompletedTask;
        }

        public void Enqueue(byte[] data) => _incoming.TryAdd(data);
        public void Complete() => _incoming.CompleteAdding();
        public void Close() { try { _connection.Disconnect("closed"); } catch { } }
    }
}
