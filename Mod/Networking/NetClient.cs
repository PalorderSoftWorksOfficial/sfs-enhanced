using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using SFSEnhanced.Shared.Networking;
using SFSEnhanced.Shared.Protocol;
using LidgrenClient = Lidgren.Network.NetClient;

namespace SFSEnhanced.Mod.Networking
{
    public sealed class ServerIdentityPrompt
    {
        public ServerIdentityDecision Decision;
        public ServerIdentityStatus Status;
        public ServerIdentity Identity;
        public string EndpointKey;
        public string DisplayAuthority;
        public string HostInput;
        public string ResolvedHost;
        public int? ResolvedPort;
        public string PresentedFingerprint;
        public string PresentedShortFingerprint;
        public string PinnedFingerprint;
        public string Reason;

        public bool IsTrustPrompt => Decision == ServerIdentityDecision.FirstUse || Decision == ServerIdentityDecision.Changed;
        public bool IsChanged => Decision == ServerIdentityDecision.Changed;
    }

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
        private readonly StagedEndpointStorage _storage;
        private readonly EndpointTokenStore _tokens;
        private readonly ServerIdentityPinStore _pins;
        private string _lastHost;
        private int? _lastPort;
        private string _lastPlayerName;
        private string _lastRegistrationPassword;
        private bool _lastCreateAccount;
        private ServerIdentityGate _lastGate;
        private PendingIdentityApproval _approval;

        public string PlayerId { get; private set; }
        public string AuthToken { get; private set; }
        public string LastConnectError { get; private set; }
        public string LastConnectErrorCode { get; private set; }
        public ServerIdentityPrompt PendingIdentity { get; private set; }
        public string MigrationNotice { get; private set; }
        public ServerIdentityPinStore Pins => _pins;
        public event Action IdentityPromptChanged;
        public string CurrentWorldId { get; private set; }
        public bool IsConnected => _connected;
        public PlayerProfile SelfProfile { get; private set; }
        public event Action<PacketType, string> OnPacket;
        public event Action ProfileChanged;
        public event Action Disconnected;

        public NetClient()
        {
            _storage = new StagedEndpointStorage(new PlayerPrefsEndpointStorage());
            _tokens = new EndpointTokenStore(_storage);
            _pins = new ServerIdentityPinStore(_storage);
            MigrateLegacyToken();
        }

        public Task<bool> ConnectAsync(string host, int port, string playerName) => ConnectAsync(host, (int?)port, playerName);

        public Task<bool> ConnectAsync(string host, int? port, string playerName, string registrationPassword = null)
            => ConnectAsync(host, port, playerName, registrationPassword, false);

        public async Task<bool> ConnectAsync(string host, int? port, string playerName, string registrationPassword, bool createAccount)
        {
            Disconnect();
            PendingIdentity = null;
            LastConnectError = null;
            LastConnectErrorCode = null;
            try
            {
                int generation = Interlocked.Increment(ref _connectionGeneration);
                CanonicalEndpoint endpoint = await ServerEndpointResolver.ResolveAsync(host, port).ConfigureAwait(false);
                _lastHost = host;
                _lastPort = port;
                _lastPlayerName = playerName;
                _lastRegistrationPassword = registrationPassword;
                _lastCreateAccount = createAccount;
                _lastGate = new ServerIdentityGate(_pins, endpoint.Key, host, port, endpoint.ResolvedHost, endpoint.ResolvedPort, _approval);

                string token = _tokens.GetToken(endpoint.Key) ?? string.Empty;
                AuthToken = token;

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
                _connection = _peer.Connect(endpoint.TransportHost, endpoint.Port);
                var pump = PumpAsync(generation, _cts.Token);
                if (await Task.WhenAny(_connectTcs.Task, Task.Delay(TimeSpan.FromSeconds(12))).ConfigureAwait(false) != _connectTcs.Task)
                    throw new TimeoutException("Timed out connecting to the multiplayer server.");
                if (!await _connectTcs.Task.ConfigureAwait(false)) throw new InvalidOperationException("The multiplayer transport rejected the connection.");

                var handshake = new SecureHandshakeClient(new LidgrenSecureTransport(this));
                SecureHandshakeResult result = await handshake.RunAsync(playerName, token, "0.1.0", registrationPassword, _cts.Token, _lastGate, createAccount).ConfigureAwait(false);
                if (!result.Accepted)
                {
                    LastConnectError = result.RejectReason ?? "Authentication failed.";
                    LastConnectErrorCode = result.ErrorCode;
                    _approval = null;
                    if (result.IdentityStatus != ServerIdentityStatus.Valid || result.IdentityDecision != ServerIdentityDecision.Trusted)
                    {
                        PendingIdentity = BuildPrompt(result, endpoint, host, port);
                        Action promptHandler = IdentityPromptChanged;
                        if (promptHandler != null) promptHandler();
                    }
                    throw new InvalidOperationException(result.RejectReason ?? "Authentication failed.");
                }

                if (_lastGate.ApprovalUsed)
                {
                    ServerIdentityPin pin = _lastGate.BuildPin(result.Identity, null, DateTime.UtcNow);
                    if (pin == null || !_pins.Accept(pin))
                    {
                        LastConnectError = "The server identity was accepted, but it could not be saved on this device, so this server is not trusted. Check that the game can write its settings, then try again.";
                        LastConnectErrorCode = AuthErrorCode.PinStoreWriteFailed;
                        _approval = null;
                        Disconnect();
                        return false;
                    }
                }
                else
                {
                    _lastGate.Verify(result.Identity, null);
                }

                _approval = null;
                _channel = result.Channel;
                PlayerId = result.PlayerId;
                if (!string.IsNullOrEmpty(result.IssuedToken))
                {
                    AuthToken = result.IssuedToken;
                    _tokens.SetToken(endpoint.Key, result.IssuedToken);
                }
                PendingIdentity = null;
                _connected = true;
                _ = Task.Run(() => ConsumeFrames(generation, _cts.Token), _cts.Token);
                await SendAsync(PacketType.ProfileRequest, new ProfileRequestPacket()).ConfigureAwait(false);
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[SFSEnhanced] Connect failed: {e.Message}");
                if (string.IsNullOrEmpty(LastConnectError)) LastConnectError = e.Message;
                _approval = null;
                Disconnect();
                return false;
            }
        }

        public bool TrustPendingIdentity()
        {
            ServerIdentityPrompt prompt = PendingIdentity;
            if (prompt == null || prompt.Identity == null || !prompt.IsTrustPrompt) return false;
            _approval = new PendingIdentityApproval
            {
                EndpointKey = prompt.EndpointKey,
                Fingerprint = prompt.Identity.Fingerprint,
                Identity = prompt.Identity,
                Origin = prompt.IsChanged ? "re_pin" : "user_verified",
                IsReplacement = prompt.IsChanged
            };
            PendingIdentity = null;
            Action handler = IdentityPromptChanged;
            if (handler != null) handler();
            return true;
        }

        public void CancelPendingIdentity()
        {
            PendingIdentity = null;
            _approval = null;
            Action handler = IdentityPromptChanged;
            if (handler != null) handler();
        }

        public bool ForgetPin(string endpointKey) => _pins.Forget(endpointKey);

        public bool RetryLastConnection(bool createAccount)
        {
            if (string.IsNullOrEmpty(_lastHost)) return false;
            _ = ConnectAsync(_lastHost, _lastPort, _lastPlayerName, _lastRegistrationPassword, createAccount);
            return true;
        }

        public bool RetryLastConnectionWithPassword(string registrationPassword)
        {
            if (string.IsNullOrEmpty(_lastHost)) return false;
            _ = ConnectAsync(_lastHost, _lastPort, _lastPlayerName, registrationPassword, _lastCreateAccount);
            return true;
        }

        private static ServerIdentityPrompt BuildPrompt(SecureHandshakeResult result, CanonicalEndpoint endpoint, string host, int? port)
        {
            return new ServerIdentityPrompt
            {
                Decision = result.IdentityDecision,
                Status = result.IdentityStatus,
                Identity = result.Identity,
                EndpointKey = result.CanonicalEndpointKey ?? endpoint.Key,
                DisplayAuthority = endpoint.DisplayAuthority,
                HostInput = host,
                ResolvedHost = endpoint.ResolvedHost,
                ResolvedPort = endpoint.ResolvedPort,
                PresentedFingerprint = result.PresentedFingerprint,
                PresentedShortFingerprint = result.PresentedShortFingerprint,
                PinnedFingerprint = result.PinnedFingerprint,
                Reason = result.RejectReason
            };
        }

        private void MigrateLegacyToken()
        {
            string legacy = SFSEnhanced.Mod.ModSettings.AuthToken;
            if (string.IsNullOrEmpty(legacy)) return;
            TokenMigrationStatus status = _tokens.MigrateLegacyToken(TokenMigration.LastSuccessfulEndpointKey(), legacy);
            if (status == TokenMigrationStatus.Migrated)
            {
                SFSEnhanced.Mod.ModSettings.AuthToken = string.Empty;
                return;
            }
            if (status == TokenMigrationStatus.SkippedExisting)
            {
                MigrationNotice = "A saved login token is already stored for that server, so the older shared token was left untouched and will not be sent to any server.";
                return;
            }
            if (status == TokenMigrationStatus.SkippedAmbiguous || status == TokenMigrationStatus.WriteFailed)
                MigrationNotice = "A saved login token exists, but the server it belongs to could not be determined, so it was kept and will not be sent to any server. Connect to that server again to reclaim the account.";
        }

        public void Disconnect()
        {
            Interlocked.Increment(ref _connectionGeneration);
            _connected = false;
            CurrentWorldId = null;
            PlayerId = null;
            SelfProfile = null;
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

        public Task UpdateProfileAsync(ProfileUpdatePacket update) => SendAsync(PacketType.ProfileUpdate, update);

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
                case PacketType.ProfileResponse:
                    var profileResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<ProfileResponsePacket>(json);
                    if (profileResponse != null && profileResponse.Found && profileResponse.Profile != null && profileResponse.Profile.IsSelf)
                    {
                        SelfProfile = profileResponse.Profile;
                        ProfileChanged?.Invoke();
                    }
                    break;
                case PacketType.ProfileUpdateResult:
                    var updateResult = Newtonsoft.Json.JsonConvert.DeserializeObject<ProfileUpdateResultPacket>(json);
                    if (updateResult == null) break;
                    if (!updateResult.Accepted) UnityEngine.Debug.LogWarning($"[SFSEnhanced] Profile update rejected: {updateResult.Reason}");
                    if (updateResult.Profile != null)
                    {
                        SelfProfile = updateResult.Profile;
                        ProfileChanged?.Invoke();
                    }
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

        private void ConsumeFrames(int generation, CancellationToken ct)
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
                    bool wasConnected = _connected;
                    _connected = false;
                    CurrentWorldId = null;
                    SelfProfile = null;
                    _connectTcs?.TrySetResult(false);
                    if (wasConnected)
                    {
                        Action handler = Disconnected;
                        if (handler != null) handler();
                    }
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
