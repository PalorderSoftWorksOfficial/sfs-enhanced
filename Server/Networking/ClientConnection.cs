using System;
using System.Threading;
using System.Threading.Tasks;
using SFSEnhanced.Server.Persistence;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Server.Networking
{
    public class ClientConnection
    {
        private readonly LidgrenServerConnection _transport;
        private SecureChannel _channel;
        private PlayerAccount _account;
        private string _currentWorldId;
        private readonly object _stateLock = new object();

        public ClientConnection(LidgrenServerConnection transport)
        {
            _transport = transport;
        }

        public PlayerAccount Account
        {
            get { lock (_stateLock) return _account; }
            private set { lock (_stateLock) _account = value; }
        }

        public string CurrentWorldId
        {
            get { lock (_stateLock) return _currentWorldId; }
            set { lock (_stateLock) _currentWorldId = value; }
        }

        public bool IsSecure => _channel != null;

        public void EstablishSecureChannel(SecureChannel channel, PlayerAccount account)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            Account = account ?? throw new ArgumentNullException(nameof(account));
        }

        public Task SendPlainAsync(PacketType type, object payload, CancellationToken ct)
        {
            return _transport.SendFrameAsync(SecureFrame.Wrap(new RawFrame { IsSealed = false, Data = LidgrenWire.Encode(type, payload) }));
        }

        public Task SendSealedAsync(PacketType type, object payload, CancellationToken ct)
        {
            SecureChannel channel = _channel;
            if (channel == null) throw new InvalidOperationException("The secure channel is not established.");
            return _transport.SendFrameAsync(SecureFrame.Wrap(new RawFrame { IsSealed = true, Data = channel.Seal(type, payload) }));
        }

        public async Task<RawFrame> ReceiveRawAsync(CancellationToken ct)
        {
            byte[] framed = await _transport.ReceiveAsync(ct).ConfigureAwait(false);
            return framed == null ? null : SecureFrame.Unwrap(framed);
        }

        public Task SendAsync(PacketType type, object payload)
        {
            SecureChannel channel = _channel;
            if (channel == null) throw new InvalidOperationException("The secure channel is not established.");
            return _transport.SendSealedAsync(type, channel.Seal(type, payload));
        }

        public async Task<(bool ok, PacketType type, string json)> ReceiveSecureAsync(CancellationToken ct)
        {
            int consecutiveInvalid = 0;
            while (true)
            {
                RawFrame frame = await ReceiveRawAsync(ct).ConfigureAwait(false);
                if (frame == null) return (false, 0, null);
                if (!frame.IsSealed) return (true, PacketType.Error, "{\"Message\":\"A plain packet was rejected on an authenticated connection.\"}");
                if (_channel.TryOpen(frame.Data, out PacketType type, out string json, out string error, out bool replayed))
                {
                    consecutiveInvalid = 0;
                    return (true, type, json);
                }
                if (replayed) continue;
                consecutiveInvalid++;
                if (consecutiveInvalid >= 32) throw new InvalidOperationException("Too many consecutive invalid packets.");
            }
        }

        public void Close() => _transport.Close();
    }
}
