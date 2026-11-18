#nullable disable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using Newtonsoft.Json;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Networking;
using SFSEnhanced.Shared.Protocol;

internal static class TestSettings
{
    public static readonly int Port = int.TryParse(Environment.GetEnvironmentVariable("SFS_INTEGRATION_PORT"), out var p) && p > 0 ? p : 18777;
    public static readonly int PasswordPort = int.TryParse(Environment.GetEnvironmentVariable("SFS_INTEGRATION_PASSWORD_PORT"), out var pp) && pp > 0 ? pp : 0;
    public static readonly int ClosedPort = int.TryParse(Environment.GetEnvironmentVariable("SFS_INTEGRATION_CLOSED_PORT"), out var cp) && cp > 0 ? cp : 0;
    public const string Password = "integration-server-password";
}

sealed class PeerTransport : ISecureTransport
{
    private readonly NetPeer _peer;
    private readonly NetConnection _connection;
    private readonly BlockingCollection<RawFrame> _frames = new();
    private readonly CancellationTokenSource _cts = new();
    private volatile bool _stopped;
    private volatile string _pumpError;
    private volatile string _lastStatus;

    private readonly Task _pump;

    public PeerTransport(NetPeer peer, NetConnection connection)
    {
        _peer = peer;
        _connection = connection;
        _pump = Task.Run(PumpAsync);
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
        RawFrame frame;
        try
        {
            if (!_frames.TryTake(out frame, Timeout.Infinite, ct)) return Task.FromResult<RawFrame>(null);
        }
        catch (OperationCanceledException)
        {
            return Task.FromResult<RawFrame>(null);
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult<RawFrame>(null);
        }
        return Task.FromResult(frame);
    }

    public async Task StopAsync()
    {
        _stopped = true;
        _cts.Cancel();
        try
        {
            await _pump;
        }
        catch (Exception)
        {
        }
    }

    public List<RawFrame> Drain()
    {
        var frames = new List<RawFrame>();
        RawFrame frame;
        while (_frames.TryTake(out frame)) frames.Add(frame);
        return frames;
    }

    public string PumpError => _pumpError;
    public string LastStatus => _lastStatus;

    private async Task PumpAsync()
    {
        try
        {
            while (!_stopped && !_cts.IsCancellationRequested)
            {
                NetIncomingMessage message = _peer.ReadMessage();
                if (message == null)
                {
                    await Task.Delay(2, _cts.Token);
                    continue;
                }
                try
                {
                    if (message.MessageType == NetIncomingMessageType.Data)
                    {
                        RawFrame frame = SecureFrame.Unwrap(message.ReadBytes(message.LengthBytes));
                        if (frame != null) _frames.TryAdd(frame);
                    }
                    else if (message.MessageType == NetIncomingMessageType.StatusChanged)
                    {
                        var status = (NetConnectionStatus)message.ReadByte();
                        string reason = message.LengthBytes > 1 ? message.ReadString() : null;
                        _lastStatus = status + (reason == null ? string.Empty : " (" + reason + ")");
                        Console.WriteLine($"[peer] status={status} reason={reason ?? "-"}");
                        if (status == NetConnectionStatus.Disconnected)
                        {
                            _pumpError = "peer disconnected during session: " + (reason ?? "no reason");
                            break;
                        }
                    }
                }
                finally { _peer.Recycle(message); }
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (Exception e)
        {
            _pumpError = $"pump failed: {e.GetType().Name}: {e.Message}";
            Console.WriteLine("[peer] " + _pumpError);
        }
        finally
        {
            try
            {
                _frames.CompleteAdding();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    private Task SendFrame(RawFrame frame)
    {
        byte[] data = SecureFrame.Wrap(frame);
        NetOutgoingMessage message = _peer.CreateMessage(data.Length);
        message.Write(data);
        _peer.SendMessage(message, _connection, NetDeliveryMethod.ReliableOrdered, 0);
        return Task.CompletedTask;
    }
}

sealed class TestPeer : IDisposable
{
    private const string AppIdentifier = "SFS-Enhanced-Multiplayer";
    private readonly NetPeer _peer;
    private readonly NetConnection _connection;
    private readonly BlockingCollection<RawFrame> _frames = new();
    private readonly CancellationTokenSource _cts = new();
    private SecureChannel _channel;
    private bool _dropped;

    private TestPeer(NetPeer peer, NetConnection connection)
    {
        _peer = peer;
        _connection = connection;
        _ = PumpAsync();
    }

    private static async Task ClosePeerAsync(NetPeer peer, NetConnection connection)
    {
        try { connection?.Disconnect("test done"); } catch { }
        for (int spin = 0; spin < 20; spin++)
        {
            NetIncomingMessage message = peer.ReadMessage();
            if (message == null)
            {
                await Task.Delay(5);
                continue;
            }
            peer.Recycle(message);
        }
        try { peer.Shutdown("test done"); } catch { }
    }

    public void DropAbruptly()
    {
        _dropped = true;
        _cts.Cancel();
        try { _peer.Shutdown("abrupt"); } catch { }
    }

    public static Task<TestPeer> ConnectAsync(string name, string authToken = null, bool createAccount = false, int port = 0, string registrationPassword = null)
        => ConnectCoreAsync(name, authToken, createAccount, port, registrationPassword);

    private static async Task<TestPeer> ConnectCoreAsync(string name, string authToken, bool createAccount, int port, string registrationPassword)
    {
        (NetPeer peer, NetConnection connection, PeerTransport transport, SecureHandshakeResult result) =
            await OpenAsync(name, authToken, createAccount, port, registrationPassword);
        await transport.StopAsync();
        if (!result.Accepted) throw new InvalidOperationException("Handshake rejected: " + (result.RejectReason ?? "unknown"));
        if (!result.IdentityProofVerified) throw new InvalidOperationException("The live server never proved possession of its identity key.");
        var session = new TestPeer(peer, connection) { PlayerId = result.PlayerId, IssuedToken = result.IssuedToken };
        session._channel = result.Channel;
        foreach (RawFrame frame in transport.Drain()) session.EnqueueFrame(frame);
        return session;
    }

    public static async Task<SecureHandshakeResult> HandshakeAsync(string name, string authToken, bool createAccount, int port = 0, string registrationPassword = null)
    {
        (NetPeer peer, NetConnection connection, PeerTransport transport, SecureHandshakeResult result) =
            await OpenAsync(name, authToken, createAccount, port, registrationPassword);
        await transport.StopAsync();
        await ClosePeerAsync(peer, connection);
        return result;
    }

    private static async Task<(NetPeer peer, NetConnection connection, PeerTransport transport, SecureHandshakeResult result)> OpenAsync(
        string name, string authToken, bool createAccount, int port, string registrationPassword)
    {
        int targetPort = port > 0 ? port : TestSettings.Port;
        string lastFailure = null;

        for (int attempt = 1; attempt <= 3; attempt++)
        {
            var config = new NetPeerConfiguration(AppIdentifier)
            {
                ConnectionTimeout = 15f,
                PingInterval = 4f,
                MaximumConnections = 1
            };
            config.EnableMessageType(NetIncomingMessageType.StatusChanged);
            config.EnableMessageType(NetIncomingMessageType.Data);
            var peer = new NetPeer(config);
            peer.Start();
            NetConnection connection = peer.Connect("127.0.0.1", targetPort);
            var connectedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            string lastStatus = null;
            _ = WatchStatusAsync(peer, connectedTcs, status =>
            {
                lastStatus = status;
                Console.WriteLine($"[connect] {name} status={status}");
            });
            await Task.WhenAny(connectedTcs.Task, Task.Delay(8000));

            if (connection.Status != NetConnectionStatus.Connected)
            {
                lastFailure = $"attempt {attempt}: never connected (status={connection.Status}, last={lastStatus ?? "none"})";
                Console.WriteLine("[connect] " + lastFailure);
                await ClosePeerAsync(peer, connection);
                await Task.Delay(250 * attempt);
                continue;
            }

            var transport = new PeerTransport(peer, connection);
            var handshake = new SecureHandshakeClient(transport);
            SecureHandshakeResult result = await handshake.RunAsync(name, authToken ?? "", "0.1.0", registrationPassword, CancellationToken.None, null, createAccount);
            if (!result.Accepted && transport.PumpError != null)
                result.RejectReason = (result.RejectReason ?? "rejected") + " [" + transport.PumpError + "]";
            return (peer, connection, transport, result);
        }

        throw new InvalidOperationException("Transport connection failed: " + lastFailure);
    }

    public static async Task ExpectHandshakeRejectionAsync(string name, string authToken, string expectedFragment, bool createAccount = false, string expectedCode = null, int port = 0, string registrationPassword = null)
    {
        SecureHandshakeResult result = await HandshakeAsync(name, authToken, createAccount, port, registrationPassword);
        if (result.Accepted) throw new InvalidOperationException("Expected the handshake to be rejected.");
        if (expectedFragment != null && (result.RejectReason ?? string.Empty).IndexOf(expectedFragment, StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException($"Expected rejection containing '{expectedFragment}' but got '{result.RejectReason}'.");
        if (expectedCode != null && !string.Equals(result.ErrorCode, expectedCode, StringComparison.Ordinal))
            throw new InvalidOperationException($"Expected error code '{expectedCode}' but got '{result.ErrorCode}' for '{result.RejectReason}'.");
    }

    public string PlayerId { get; private set; }
    public string IssuedToken { get; private set; }

    public void EnqueueFrame(RawFrame frame) => _frames.TryAdd(frame);

    public async Task SendAsync(PacketType type, object payload)
    {
        byte[] sealedBytes = _channel.Seal(type, payload);
        byte[] data = SecureFrame.Wrap(new RawFrame { IsSealed = true, Data = sealedBytes });
        NetOutgoingMessage message = _peer.CreateMessage(data.Length);
        message.Write(data);
        NetDeliveryMethod method = LidgrenWire.IsHotStream(type) ? NetDeliveryMethod.UnreliableSequenced : NetDeliveryMethod.ReliableOrdered;
        _peer.SendMessage(message, _connection, method, LidgrenWire.GetChannel(type));
        await Task.CompletedTask;
    }

    internal static bool TryTakeFrame(BlockingCollection<RawFrame> frames, int timeoutMs, CancellationToken token, out RawFrame frame)
    {
        try
        {
            return frames.TryTake(out frame, timeoutMs, token);
        }
        catch (OperationCanceledException)
        {
            frame = null;
            return false;
        }
    }

    public Task<(PacketType Type, string Json)> ReceiveAsync(int timeoutMs = 4000)
    {
        while (true)
        {
            if (!TryTakeFrame(_frames, timeoutMs, _cts.Token, out RawFrame frame)) throw new TimeoutException("Timed out waiting for packet.");

            if (!frame.IsSealed) throw new InvalidOperationException("Got a plain frame on an authenticated session.");
            if (_channel.TryOpen(frame.Data, out PacketType type, out string json, out string error, out bool replayed))
                return Task.FromResult((type, json));
            if (!replayed) throw new InvalidOperationException("Sealed packet rejected: " + error);
        }
    }

    public async Task<T> ReceiveTypeAsync<T>(PacketType expected)
    {
        for (int i = 0; i < 64; i++)
        {
            (PacketType type, string json) = await ReceiveAsync();
            if (type == PacketType.WorldTimeState) continue;
            if (type != expected) throw new InvalidOperationException($"Expected {expected}, got {type}: {json}");
            return JsonConvert.DeserializeObject<T>(json) ?? throw new InvalidOperationException($"Invalid {expected} payload.");
        }
        throw new TimeoutException($"Could not receive {expected}.");
    }

    public async Task<WorldTimeStatePacket> ReceiveWorldTimeAsync()
    {
        for (int i = 0; i < 64; i++)
        {
            (PacketType type, string json) = await ReceiveAsync();
            if (type != PacketType.WorldTimeState) continue;
            return JsonConvert.DeserializeObject<WorldTimeStatePacket>(json) ?? throw new InvalidOperationException("Invalid WorldTimeState payload.");
        }
        throw new TimeoutException("Could not receive WorldTimeState.");
    }

    public Task DrainUntilQuietAsync()
    {
        while (true)
        {
            if (!TryTakeFrame(_frames, 400, _cts.Token, out RawFrame frame)) return Task.CompletedTask;
        }
    }

    public async Task SendRawBytesAsync(byte[] raw, bool reliable)
    {
        NetOutgoingMessage message = _peer.CreateMessage(raw.Length);
        message.Write(raw);
        _peer.SendMessage(message, _connection, reliable ? NetDeliveryMethod.ReliableOrdered : NetDeliveryMethod.UnreliableSequenced, reliable ? 0 : 1);
        await Task.CompletedTask;
    }

    public byte[] SealForTamperTest(PacketType type, object payload) => _channel.Seal(type, payload);

    public async Task<bool> ExpectRejectionAsync(byte[] tampered)
    {
        await SendRawBytesAsync(SecureFrame.Wrap(new RawFrame { IsSealed = true, Data = tampered }), true);
        try
        {
            (PacketType type, string json) = await ReceiveAsync(2500);
            return type == PacketType.Error || type == PacketType.AuthError ||
                   (json ?? string.Empty).IndexOf("rejected", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private static async Task WatchStatusAsync(NetPeer peer, TaskCompletionSource<bool> connected, Action<string> report)
    {
        while (!connected.Task.IsCompleted)
        {
            NetIncomingMessage message = peer.ReadMessage();
            if (message == null)
            {
                await Task.Delay(10);
                continue;
            }
            try
            {
                if (message.MessageType == NetIncomingMessageType.StatusChanged)
                {
                    var status = (NetConnectionStatus)message.ReadByte();
                    string reason = message.LengthBytes > 1 ? message.ReadString() : null;
                    report?.Invoke(status + (reason == null ? string.Empty : " (" + reason + ")"));
                    if (status == NetConnectionStatus.Connected) connected.TrySetResult(true);
                    if (status == NetConnectionStatus.Disconnected) connected.TrySetResult(false);
                }
            }
            finally { peer.Recycle(message); }
        }
    }

    private async Task PumpAsync()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                NetIncomingMessage message = _peer.ReadMessage();
                if (message == null)
                {
                    await Task.Delay(4, _cts.Token);
                    continue;
                }
                try
                {
                    if (message.MessageType == NetIncomingMessageType.Data)
                    {
                        RawFrame frame = SecureFrame.Unwrap(message.ReadBytes(message.LengthBytes));
                        if (frame != null) _frames.TryAdd(frame);
                    }
                }
                finally { _peer.Recycle(message); }
            }
        }
        catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        _cts.Cancel();
        if (_dropped)
        {
            try { _peer.Shutdown("integration complete"); } catch { }
            return;
        }
        try { _connection.Disconnect("integration complete"); } catch { }
        try { _peer.Shutdown("integration complete"); } catch { }
        _cts.Dispose();
    }
}

static class Program
{
    public static async Task Main()
    {
        string suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
        string aliceName = "Alice_" + suffix;
        string bobName = "Bob_" + suffix;
        string worldName = "Integration World " + suffix;

        VerifyStagedStorage();
        await VerifyAccountModesAsync(suffix);

        TestPeer alice = await TestPeer.ConnectAsync(aliceName, null, createAccount: true);
        TestPeer bob = await TestPeer.ConnectAsync(bobName, null, createAccount: true);
        if (alice.IssuedToken == null || bob.IssuedToken == null)
            throw new InvalidOperationException("Server did not issue auth tokens for the new accounts.");

        await alice.SendAsync(PacketType.ServerInfoRequest, new { });
        ServerInfoResponsePacket info = await alice.ReceiveTypeAsync<ServerInfoResponsePacket>(PacketType.ServerInfoResponse);
        if (info.OnlinePlayers < 2) throw new InvalidOperationException("Server player count did not update.");

        await TestPeer.ExpectHandshakeRejectionAsync(aliceName, "definitely-not-a-valid-token", "token");

        TestPeer aliceReconnect = await TestPeer.ConnectAsync(aliceName, alice.IssuedToken);
        if (!aliceReconnect.PlayerId.Equals(alice.PlayerId, StringComparison.Ordinal))
            throw new InvalidOperationException("Token reconnect did not restore the same player identity.");
        alice.Dispose();
        alice = aliceReconnect;

        await alice.SendAsync(PacketType.ProfileRequest, new ProfileRequestPacket());
        ProfileResponsePacket selfProfile = await alice.ReceiveTypeAsync<ProfileResponsePacket>(PacketType.ProfileResponse);
        if (!selfProfile.Found || selfProfile.Profile == null || !selfProfile.Profile.IsSelf)
            throw new InvalidOperationException("Server did not return the caller's own profile.");
        if (selfProfile.Profile.PlayerName != aliceName)
            throw new InvalidOperationException("Profile player name mismatch.");
        if (selfProfile.Profile.SessionsCount < 2)
            throw new InvalidOperationException("Profile session counter did not track both connections.");

        await alice.SendAsync(PacketType.ProfileUpdate, new ProfileUpdatePacket { DisplayName = new string('x', 200) });
        ProfileUpdateResultPacket overlong = await alice.ReceiveTypeAsync<ProfileUpdateResultPacket>(PacketType.ProfileUpdateResult);
        if (overlong.Accepted || string.IsNullOrEmpty(overlong.Reason))
            throw new InvalidOperationException("An over-long display name was not rejected with a reason.");

        await alice.SendAsync(PacketType.ProfileUpdate, new ProfileUpdatePacket { DisplayName = "Cmdr " + suffix, Bio = "Profile round trip.", AvatarAccent = "#123456" });
        ProfileUpdateResultPacket applied = await alice.ReceiveTypeAsync<ProfileUpdateResultPacket>(PacketType.ProfileUpdateResult);
        if (!applied.Accepted || applied.Profile == null || applied.Profile.DisplayName != "Cmdr " + suffix)
            throw new InvalidOperationException("A valid profile update was not applied: " + applied.Reason);
        if (applied.Profile.AvatarAccent != "#123456")
            throw new InvalidOperationException("Profile accent was not stored as sent.");

        await bob.SendAsync(PacketType.ProfileRequest, new ProfileRequestPacket { PlayerId = aliceName });
        ProfileResponsePacket publicProfile = await bob.ReceiveTypeAsync<ProfileResponsePacket>(PacketType.ProfileResponse);
        if (!publicProfile.Found || publicProfile.Profile.IsSelf || publicProfile.Profile.DisplayName != "Cmdr " + suffix)
            throw new InvalidOperationException("Another player could not read the updated public profile.");

        await alice.SendAsync(PacketType.ProfileUpdate, new ProfileUpdatePacket { ShowPresence = false });
        ProfileUpdateResultPacket hidden = await alice.ReceiveTypeAsync<ProfileUpdateResultPacket>(PacketType.ProfileUpdateResult);
        if (!hidden.Accepted)
            throw new InvalidOperationException("Privacy update was rejected: " + hidden.Reason);

        await bob.SendAsync(PacketType.ProfileRequest, new ProfileRequestPacket { PlayerId = aliceName });
        ProfileResponsePacket hiddenProfile = await bob.ReceiveTypeAsync<ProfileResponsePacket>(PacketType.ProfileResponse);
        if (hiddenProfile.Profile == null || hiddenProfile.Profile.Online || hiddenProfile.Profile.ShowPresence)
            throw new InvalidOperationException("Presence privacy was not enforced for other viewers.");

        int throttled = 0;
        for (int i = 0; i < 8; i++)
        {
            await alice.SendAsync(PacketType.ProfileUpdate, new ProfileUpdatePacket { Bio = "flood " + i });
            ProfileUpdateResultPacket flood = await alice.ReceiveTypeAsync<ProfileUpdateResultPacket>(PacketType.ProfileUpdateResult);
            if (!flood.Accepted) throttled++;
        }
        if (throttled == 0)
            throw new InvalidOperationException("A profile update flood was not rate limited.");

        await alice.SendAsync(PacketType.WorldCreate, new WorldCreatePacket { Name = worldName, IsPublic = true });
        WorldJoinAckPacket created = await alice.ReceiveTypeAsync<WorldJoinAckPacket>(PacketType.WorldJoinAck);
        if (!created.Accepted) throw new InvalidOperationException(created.RejectReason);
        string worldId = created.WorldId;

        await bob.SendAsync(PacketType.WorldListRequest, new { });
        WorldListResponsePacket worlds = await bob.ReceiveTypeAsync<WorldListResponsePacket>(PacketType.WorldListResponse);
        if (!worlds.Worlds.Any(x => x.WorldId == worldId)) throw new InvalidOperationException("Created world was not listed.");

        await bob.SendAsync(PacketType.WorldJoin, new WorldJoinPacket { WorldId = worldId });
        WorldJoinAckPacket joined = await bob.ReceiveTypeAsync<WorldJoinAckPacket>(PacketType.WorldJoinAck);
        if (!joined.Accepted) throw new InvalidOperationException(joined.RejectReason);
        ChatMessagePacket joinBroadcast = await bob.ReceiveTypeAsync<ChatMessagePacket>(PacketType.ChatMessage);
        ChatMessagePacket joinBroadcastOwner = await alice.ReceiveTypeAsync<ChatMessagePacket>(PacketType.ChatMessage);
        if (joinBroadcast.Message != $"{bobName} joined the world." || joinBroadcastOwner.Message != $"{bobName} joined the world.")
            throw new InvalidOperationException("Join broadcast mismatch.");

        await alice.SendAsync(PacketType.BuildSpawn, new BuildSnapshot { BuildId = "build-1", DisplayName = "Alice Rocket", Kind = BuildKind.Rocket, PartsBlueprintJson = "{}" });
        BuildSnapshot spawn = await bob.ReceiveTypeAsync<BuildSnapshot>(PacketType.BuildSpawn);
        if (spawn.BuildId != "build-1") throw new InvalidOperationException("Build spawn mismatch.");

        await alice.SendAsync(PacketType.RocketPrimaryState, new RocketPrimaryStatePacket
        {
            WorldId = worldId, BuildId = "build-1", PosX = 10, PosY = 20, VelX = 3, VelY = 4,
            RotationDegrees = 15, AngularVelocity = 2, PlanetAddress = "Earth", WorldTime = 12, Tick = 1
        });
        RocketPrimaryStatePacket primary = await bob.ReceiveTypeAsync<RocketPrimaryStatePacket>(PacketType.RocketPrimaryState);
        if (primary.BuildId != "build-1" || primary.PosX != 10 || primary.Tick != 1) throw new InvalidOperationException("Rocket primary state mismatch.");

        await alice.SendAsync(PacketType.RocketSecondaryState, new RocketSecondaryStatePacket
        {
            WorldId = worldId, BuildId = "build-1", ThrottlePercent = 75, RcsEnabled = true, EnginesEnabled = true, Tick = 2
        });
        RocketSecondaryStatePacket secondary = await bob.ReceiveTypeAsync<RocketSecondaryStatePacket>(PacketType.RocketSecondaryState);
        if (secondary.ThrottlePercent != 75 || !secondary.RcsEnabled || secondary.Tick != 2) throw new InvalidOperationException("Rocket secondary state mismatch.");

        await alice.SendAsync(PacketType.PartModuleState, new PartModuleStatePacket
        {
            WorldId = worldId, BuildId = "build-1", PartId = "engine-1", ModuleType = "engine", Enabled = true, Value = 1, Tick = 3
        });
        PartModuleStatePacket module = await bob.ReceiveTypeAsync<PartModuleStatePacket>(PacketType.PartModuleState);
        if (module.PartId != "engine-1" || !module.Enabled) throw new InvalidOperationException("Part module state mismatch.");

        await alice.SendAsync(PacketType.ClaimCreate, new ClaimCreatePacket { WorldId = worldId, Shape = ClaimShape.Build, BuildId = "build-1" });
        ClaimInfo claim = await bob.ReceiveTypeAsync<ClaimInfo>(PacketType.ClaimCreate);
        ClaimInfo claimOwner = await alice.ReceiveTypeAsync<ClaimInfo>(PacketType.ClaimCreate);
        if (string.IsNullOrEmpty(claim.ClaimId) || claimOwner.ClaimId != claim.ClaimId) throw new InvalidOperationException("Claim broadcast mismatch.");

        await alice.SendAsync(PacketType.ClaimRemove, new ClaimRemovePacket { WorldId = worldId, ClaimId = claim.ClaimId });
        ClaimRemovePacket removed = await bob.ReceiveTypeAsync<ClaimRemovePacket>(PacketType.ClaimRemove);
        ClaimRemovePacket removedOwner = await alice.ReceiveTypeAsync<ClaimRemovePacket>(PacketType.ClaimRemove);
        if (removed.ClaimId != claim.ClaimId || removedOwner.ClaimId != claim.ClaimId) throw new InvalidOperationException("Claim removal mismatch.");

        await alice.SendAsync(PacketType.FriendRequest, new FriendRequestPacket { TargetPlayerName = bobName });
        FriendListResponsePacket incoming = await bob.ReceiveTypeAsync<FriendListResponsePacket>(PacketType.FriendListResponse);
        FriendListResponsePacket outgoing = await alice.ReceiveTypeAsync<FriendListResponsePacket>(PacketType.FriendListResponse);
        if (!incoming.PendingIncoming.Any(x => x.PlayerName == aliceName) || !outgoing.PendingOutgoing.Any(x => x.PlayerName == bobName))
            throw new InvalidOperationException("Friend request was not delivered.");

        await bob.SendAsync(PacketType.FriendRequestResponse, new FriendRequestResponsePacket { FromPlayerId = alice.PlayerId, Accepted = true });
        FriendListResponsePacket bobFriends = await bob.ReceiveTypeAsync<FriendListResponsePacket>(PacketType.FriendListResponse);
        FriendListResponsePacket aliceFriends = await alice.ReceiveTypeAsync<FriendListResponsePacket>(PacketType.FriendListResponse);
        if (!bobFriends.Friends.Any(x => x.PlayerName == aliceName) || !aliceFriends.Friends.Any(x => x.PlayerName == bobName))
            throw new InvalidOperationException("Friend acceptance was not persisted.");

        await alice.SendAsync(PacketType.ChatMessage, new ChatMessagePacket { WorldId = worldId, Message = "integration chat" });
        ChatMessagePacket chat = await bob.ReceiveTypeAsync<ChatMessagePacket>(PacketType.ChatMessage);
        ChatMessagePacket chatOwner = await alice.ReceiveTypeAsync<ChatMessagePacket>(PacketType.ChatMessage);
        if (chat.Message != "integration chat" || chat.FromPlayerName != aliceName || chatOwner.Message != "integration chat")
            throw new InvalidOperationException("Chat payload mismatch.");

        await alice.SendAsync(PacketType.ChatMessage, new ChatMessagePacket { WorldId = worldId, Message = "<b>markup</b>" });
        await ExpectErrorCodeAsync(alice, "cannot contain", "chat_rejected");

        await bob.SendAsync(PacketType.ChatMessage, new ChatMessagePacket { WorldId = worldId, Message = "  spacing\u200B\ncheck\ttwo  " });
        ChatMessagePacket cleanedChat = await alice.ReceiveTypeAsync<ChatMessagePacket>(PacketType.ChatMessage);
        ChatMessagePacket cleanedOwner = await bob.ReceiveTypeAsync<ChatMessagePacket>(PacketType.ChatMessage);
        if (cleanedChat.Message != "spacing check two" || cleanedOwner.Message != "spacing check two")
            throw new InvalidOperationException($"Chat text was not filtered server-side: '{cleanedChat.Message}'.");
        if (cleanedChat.FromPlayerName != bobName)
            throw new InvalidOperationException("Chat filter changed the sender identity.");
        Console.WriteLine("[assert] chat text is filtered and validated server-side");

        await alice.SendAsync(PacketType.TimeWarpRequest, new TimeWarpRequestPacket { WorldId = worldId, RequestedMultiplier = 4 });
        TimeWarpVotePacket voteAlice = await alice.ReceiveTypeAsync<TimeWarpVotePacket>(PacketType.TimeWarpVote);
        TimeWarpVotePacket voteBob = await bob.ReceiveTypeAsync<TimeWarpVotePacket>(PacketType.TimeWarpVote);
        if (voteAlice.VoteId != voteBob.VoteId || voteAlice.RequestedMultiplier != 4) throw new InvalidOperationException("Timewarp vote broadcast mismatch.");
        await bob.SendAsync(PacketType.TimeWarpVoteResponse, new TimeWarpVoteResponsePacket { WorldId = worldId, VoteId = voteBob.VoteId, Approved = true });
        TimeWarpResultPacket warpAlice = await alice.ReceiveTypeAsync<TimeWarpResultPacket>(PacketType.TimeWarpResult);
        TimeWarpResultPacket warpBob = await bob.ReceiveTypeAsync<TimeWarpResultPacket>(PacketType.TimeWarpResult);
        if (!warpAlice.Approved || !warpBob.Approved || warpAlice.ActualMultiplier != 4 || warpBob.ActualMultiplier != 4)
            throw new InvalidOperationException("Timewarp result mismatch.");

        WorldTimeStatePacket time = await alice.ReceiveWorldTimeAsync();
        if (time.WorldId != worldId || time.WorldTime <= 0) throw new InvalidOperationException("World time state was invalid.");

        byte[] replayed = alice.SealForTamperTest(PacketType.ChatMessage, new ChatMessagePacket { WorldId = worldId, Message = "replay-probe" });
        byte[] replayFrame = SecureFrame.Wrap(new RawFrame { IsSealed = true, Data = replayed });
        await alice.SendRawBytesAsync(replayFrame, true);
        await alice.SendRawBytesAsync(replayFrame, true);
        await Task.Delay(300);

        byte[] tampered = alice.SealForTamperTest(PacketType.ChatMessage, new ChatMessagePacket { WorldId = worldId, Message = "tamper-probe" });
        tampered[tampered.Length / 2] ^= 0x40;
        await alice.SendRawBytesAsync(SecureFrame.Wrap(new RawFrame { IsSealed = true, Data = tampered }), true);
        await Task.Delay(300);

        await bob.SendAsync(PacketType.WorldLeave, new { });
        await bob.DrainUntilQuietAsync();
        await alice.SendAsync(PacketType.WorldListRequest, new { });
        int replayAccepted = 0;
        WorldListResponsePacket afterLeave = null;
        for (int attempt = 0; attempt < 64; attempt++)
        {
            (PacketType type, string json) = await alice.ReceiveAsync();
            if (type == PacketType.WorldTimeState) continue;
            if (type == PacketType.ChatMessage)
            {
                if ((json ?? string.Empty).IndexOf("tamper-probe", StringComparison.Ordinal) >= 0)
                    throw new InvalidOperationException("A tampered packet was accepted by the server.");
                if ((json ?? string.Empty).IndexOf("replay-probe", StringComparison.Ordinal) >= 0)
                {
                    replayAccepted++;
                    if (replayAccepted > 1) throw new InvalidOperationException("A replayed packet was accepted more than once by the server.");
                }
                continue;
            }
            if (type == PacketType.WorldListResponse)
            {
                afterLeave = JsonConvert.DeserializeObject<WorldListResponsePacket>(json);
                break;
            }
        }
        if (afterLeave == null) throw new TimeoutException("Did not receive the world list after the security probes.");
        if (replayAccepted != 1) throw new InvalidOperationException($"Replay probe echo count was {replayAccepted}, expected exactly 1 (first send accepted, replay rejected).");
        WorldSummary listed = afterLeave.Worlds.FirstOrDefault(x => x.WorldId == worldId);
        if (listed == null || listed.PlayersOnline != 1) throw new InvalidOperationException("World leave did not update player count.");

        await VerifyConnectionLifecycleAsync(suffix);
        await VerifyDataPlaneAsync(suffix);
        await VerifyRoomsAsync(suffix);

        VerifyNoOrphanedTake();
        VerifyChatWindow();
        VerifyEndpointKeys();
        VerifyIdentityPinning();
        VerifyTokenMigration();
        await VerifyIdentityGateBlocksProofAsync();
        await VerifyAccountNameCanonicalizationAsync(bobName, bob.IssuedToken, bob.PlayerId);

        alice.Dispose();
        bob.Dispose();
        Console.WriteLine("MULTI_CLIENT_INTEGRATION_PASSED");
    }

    private static void VerifyNoOrphanedTake()
    {
        using var frames = new BlockingCollection<RawFrame>();
        using var cts = new CancellationTokenSource();

        if (TestPeer.TryTakeFrame(frames, 150, cts.Token, out _))
            throw new InvalidOperationException("An empty queue reported a frame.");

        frames.Add(new RawFrame { IsSealed = false, Data = new byte[] { 7 } });
        if (!TestPeer.TryTakeFrame(frames, 1000, cts.Token, out RawFrame frame))
            throw new InvalidOperationException("A timed-out wait swallowed the next frame.");
        if (frame == null || frame.Data == null || frame.Data.Length != 1 || frame.Data[0] != 7)
            throw new InvalidOperationException("The frame delivered after a timeout was not the enqueued frame.");

        Console.WriteLine("[assert] timed wait does not orphan the next frame");
    }

    private static void VerifyChatWindow()
    {
        if (ChatLogWindow.Start(12, 12) != 0)
            throw new InvalidOperationException("Chat window start for a full page should be 0.");
        if (ChatLogWindow.Start(13, 12) != 1)
            throw new InvalidOperationException("Chat window start for the 13th line should be 1.");
        if (!ChatLogWindow.NeedsRedraw(0, 1, 12, 13, 13, 14))
            throw new InvalidOperationException("Chat window must redraw when the 13th line shifts the window.");
        if (ChatLogWindow.Slot(12, 1) != 11)
            throw new InvalidOperationException("Chat window slot for the 13th line should be 11.");
        for (int count = 13; count <= 60; count++)
        {
            int start = ChatLogWindow.Start(count, 12);
            int last = ChatLogWindow.Slot(count - 1, start);
            if (last != 11) throw new InvalidOperationException($"Chat window last slot at {count} lines was {last}, expected 11.");
            if (ChatLogWindow.NeedsRedraw(start, start, count, count + 1, count, count + 1))
                throw new InvalidOperationException($"Chat window redrew while appending the {count + 1}th line.");
        }

        if (!ChatLogWindow.NeedsRedraw(48, 48, 60, 60, 100, 101))
            throw new InvalidOperationException("Chat window must redraw when the capped log's newest line changes.");
        if (ChatLogWindow.NeedsRedraw(48, 48, 60, 60, 101, 101))
            throw new InvalidOperationException("Chat window redrew without a tail change at a stable capped size.");
        if (ChatLogWindow.Slot(59, ChatLogWindow.Start(60, 12)) != 11)
            throw new InvalidOperationException("Chat window newest slot in a capped log should be 11.");

        if (!ChatLogWindow.NeedsRedraw(20, 0, 30, 5, 30, 5))
            throw new InvalidOperationException("Chat window must redraw after the log shrinks or resets.");
        Console.WriteLine("[assert] chat window sliding math");
    }

    private static void RequireKey(string input, int? port, string expected)
    {
        if (!EndpointKey.TryParseAuthority(input, port, out CanonicalEndpoint endpoint, out string error))
            throw new InvalidOperationException($"Endpoint '{input}' was rejected: {error}");
        if (!string.Equals(endpoint.Key, expected, StringComparison.Ordinal))
            throw new InvalidOperationException($"Endpoint '{input}' produced key '{endpoint.Key}', expected '{expected}'.");
    }

    private static void RejectKey(string input, int? port = null)
    {
        if (EndpointKey.TryParseAuthority(input, port, out CanonicalEndpoint endpoint, out _))
            throw new InvalidOperationException($"Endpoint '{input}' was accepted as '{endpoint.Key}' but must be rejected.");
    }

    private static void VerifyEndpointKeys()
    {
        RequireKey("EXAMPLE.com.", null, "example.com#auto");
        RequireKey("sfs://example.com", null, "example.com#auto");
        RequireKey("example.com", 7777, "example.com:7777");
        RequireKey("example.com:7777", null, "example.com:7777");
        RequireKey("sfs://example.com:7777", null, "example.com:7777");
        RequireKey("[0:0:0:0:0:0:0:1]:7777", null, "[::1]:7777");
        RequireKey("[::1]:7777", null, "[::1]:7777");
        RequireKey("127.0.0.1", 7777, "127.0.0.1:7777");
        RequireKey("bücher.example", null, "xn--bcher-kva.example#auto");

        RejectKey("example.com:0");
        RejectKey("example.com:70000");
        RejectKey("example.com:7777", 7778);
        RejectKey("bad-.example");
        RejectKey("-bad.example");
        RejectKey("a..b");
        RejectKey("exa mple.com");
        RejectKey("example.com/path");
        RejectKey("https://example.com");
        RejectKey("user@example.com");

        if (!EndpointKey.TryParseAuthority("example.com", null, out CanonicalEndpoint auto, out _) ||
            !EndpointKey.TryParseAuthority("example.com:7777", null, out CanonicalEndpoint direct, out _))
            throw new InvalidOperationException("Both a host-only and an explicit-port endpoint must parse.");
        if (string.Equals(auto.Key, direct.Key, StringComparison.Ordinal))
            throw new InvalidOperationException("An automatic-port endpoint and an explicit-port endpoint shared a key.");
        if (!string.Equals(auto.DisplayAuthority, "example.com", StringComparison.Ordinal))
            throw new InvalidOperationException("Automatic-port display must not expose the internal key suffix.");

        Console.WriteLine("[assert] endpoint keys canonicalize without aliasing");
    }

    private static void VerifyStagedStorage()
    {
        var backing = new MemoryEndpointStorage();
        var storage = new StagedEndpointStorage(backing);

        if (!storage.Write("pins", "first")) throw new InvalidOperationException("A staged write to a healthy store failed.");
        if (!string.Equals(storage.Read("pins"), "first", StringComparison.Ordinal)) throw new InvalidOperationException("A committed staged write did not read back.");
        if (!string.IsNullOrEmpty(backing.Read("pins" + StagedEndpointStorage.PendingSuffix))) throw new InvalidOperationException("The staged key survived a committed write.");

        var failing = new DroppingEndpointStorage(backing) { DropWrites = true };
        var failingStorage = new StagedEndpointStorage(failing);
        if (failingStorage.Write("pins", "second")) throw new InvalidOperationException("A write the backend dropped was reported as committed.");
        if (!string.Equals(backing.Read("pins"), "first", StringComparison.Ordinal)) throw new InvalidOperationException("A dropped write damaged the previous value.");

        failing.DropWrites = false;
        if (!failingStorage.Write("pins", "third")) throw new InvalidOperationException("A staged write failed to recover after the backend returned.");
        if (!string.Equals(backing.Read("pins"), "third", StringComparison.Ordinal)) throw new InvalidOperationException("A recovered staged write did not commit.");

        failing.ThrowOnRead = true;
        bool propagated = false;
        try
        {
            failingStorage.Read("pins");
        }
        catch (InvalidOperationException)
        {
            propagated = true;
        }
        if (!propagated) throw new InvalidOperationException("A storage read failure was swallowed instead of reaching the caller.");

        var unreadablePins = new ServerIdentityPinStore(failingStorage);
        PinLookup unreadable = unreadablePins.Lookup("example.com:7777", new string('a', 64));
        if (!unreadable.StoreUnreadable) throw new InvalidOperationException("An unreadable pin store was reported as merely empty, so a missing pin would be unsafe.");
        if (unreadable.Match != PinMatch.Missing) throw new InvalidOperationException("An unreadable pin store reported a match.");

        failing.ThrowOnRead = false;
        storage.Remove("pins");
        if (!string.IsNullOrEmpty(backing.Read("pins"))) throw new InvalidOperationException("Remove did not clear the stored value.");

        Console.WriteLine("[assert] staged storage never promotes a dropped write");
    }

    private static async Task VerifyConnectionLifecycleAsync(string suffix)
    {
        for (int index = 0; index < 50; index++)
        {
            await TestPeer.ExpectHandshakeRejectionAsync("Ghost_" + suffix + "_" + index, null, null, createAccount: false, expectedCode: AuthErrorCode.UnknownAccount);
            using TestPeer registered = await TestPeer.ConnectAsync("Life_" + suffix + "_" + index, null, createAccount: true);
            if (string.IsNullOrEmpty(registered.IssuedToken))
                throw new InvalidOperationException($"Lifecycle registration {index} did not issue a token.");
            await SyncAsync(registered);
            if (index % 10 == 0) registered.DropAbruptly();
        }

        using TestPeer survivor = await TestPeer.ConnectAsync("Survivor_" + suffix, null, createAccount: true);
        await SyncAsync(survivor);
        Console.WriteLine("[assert] peer lifecycle survives 100 alternating handshakes and abrupt drops");
    }

    private static async Task VerifyDataPlaneAsync(string suffix)
    {
        using TestPeer owner = await TestPeer.ConnectAsync("DpOwner_" + suffix, null, createAccount: true);
        using TestPeer stranger = await TestPeer.ConnectAsync("DpOther_" + suffix, null, createAccount: true);

        await owner.SendAsync(PacketType.WorldCreate, new WorldCreatePacket { Name = "Private DP " + suffix, IsPublic = false });
        WorldJoinAckPacket privateWorld = await owner.ReceiveTypeAsync<WorldJoinAckPacket>(PacketType.WorldJoinAck);
        if (!privateWorld.Accepted) throw new InvalidOperationException("Could not create a private world: " + privateWorld.RejectReason);

        await stranger.SendAsync(PacketType.WorldJoin, new WorldJoinPacket { WorldId = privateWorld.WorldId });
        WorldJoinAckPacket deniedJoin = await stranger.ReceiveTypeAsync<WorldJoinAckPacket>(PacketType.WorldJoinAck);
        if (deniedJoin.Accepted) throw new InvalidOperationException("A stranger joined a private world.");

        await stranger.SendAsync(PacketType.WorldDownloadRequest, new WorldJoinPacket { WorldId = privateWorld.WorldId });
        await ExpectErrorAsync(stranger, "private");

        await owner.SendAsync(PacketType.WorldDownloadRequest, new WorldJoinPacket { WorldId = privateWorld.WorldId });
        await ExpectDownloadAsync(owner, privateWorld.WorldId);

        await owner.SendAsync(PacketType.WorldCreate, new WorldCreatePacket { Name = "Public DP " + suffix, IsPublic = true });
        WorldJoinAckPacket publicWorld = await owner.ReceiveTypeAsync<WorldJoinAckPacket>(PacketType.WorldJoinAck);
        if (!publicWorld.Accepted) throw new InvalidOperationException("Could not create a public world: " + publicWorld.RejectReason);

        await stranger.SendAsync(PacketType.WorldDownloadRequest, new WorldJoinPacket { WorldId = publicWorld.WorldId });
        await ExpectDownloadAsync(stranger, publicWorld.WorldId);

        await owner.SendAsync(PacketType.FriendRequest, new FriendRequestPacket { TargetPlayerName = "DpOther_" + suffix });
        await owner.ReceiveTypeAsync<FriendListResponsePacket>(PacketType.FriendListResponse);
        await stranger.ReceiveTypeAsync<FriendListResponsePacket>(PacketType.FriendListResponse);
        await stranger.SendAsync(PacketType.FriendRequestResponse, new FriendRequestResponsePacket { FromPlayerId = owner.PlayerId, Accepted = true });
        await stranger.ReceiveTypeAsync<FriendListResponsePacket>(PacketType.FriendListResponse);
        await owner.ReceiveTypeAsync<FriendListResponsePacket>(PacketType.FriendListResponse);

        await stranger.SendAsync(PacketType.WorldJoin, new WorldJoinPacket { WorldId = privateWorld.WorldId });
        WorldJoinAckPacket friendJoin = await stranger.ReceiveTypeAsync<WorldJoinAckPacket>(PacketType.WorldJoinAck);
        if (!friendJoin.Accepted) throw new InvalidOperationException("A friend could not join a private world: " + friendJoin.RejectReason);

        await stranger.SendAsync(PacketType.WorldLeave, new { });

        string uploadId = "up-" + suffix;
        await SendUploadAsync(stranger, uploadId, 1, 0, "!!!not-base64!!!");
        await ExpectErrorAsync(stranger, "base64");
        await ExpectUploadSucceedsAsync(stranger, uploadId + "-a", "Retry base64 " + suffix);

        await SendUploadAsync(stranger, uploadId, 1, 0, Convert.ToBase64String(Encoding.UTF8.GetBytes("this is not json")));
        await ExpectErrorAsync(stranger, "not valid JSON");
        await ExpectUploadSucceedsAsync(stranger, uploadId + "-b", "Retry json " + suffix);

        await SendUploadAsync(stranger, uploadId, 1, 0, new string('A', 100 * 1024));
        await ExpectErrorAsync(stranger, "larger than");
        await ExpectUploadSucceedsAsync(stranger, uploadId + "-c", "Retry size " + suffix);

        await SendUploadAsync(stranger, uploadId, 2, 1, "QUJD");
        await ExpectErrorAsync(stranger, "in order");
        await ExpectUploadSucceedsAsync(stranger, uploadId + "-d", "Retry order " + suffix);

        await SendUploadAsync(stranger, uploadId, 2, 0, "QUJD");
        await SendUploadAsync(stranger, uploadId, 2, 0, "QUJD");
        await ExpectErrorAsync(stranger, "in order");
        await ExpectUploadSucceedsAsync(stranger, uploadId + "-e", "Retry duplicate " + suffix);

        await SendUploadAsync(stranger, uploadId, 257, 0, "QUJD");
        await ExpectErrorAsync(stranger, "Invalid upload chunk");
        await ExpectUploadSucceedsAsync(stranger, uploadId + "-f", "Retry count " + suffix);

        var duplicated = new WorldRecord
        {
            Name = "Dup " + suffix,
            Builds = new List<BuildSnapshot>
            {
                new BuildSnapshot { BuildId = "dup-build", PosX = 1, PosY = 2 },
                new BuildSnapshot { BuildId = "dup-build", PosX = 3, PosY = 4 }
            }
        };
        await SendUploadJsonAsync(stranger, uploadId, duplicated);
        await ExpectErrorAsync(stranger, "duplicate build ids");
        await ExpectUploadSucceedsAsync(stranger, uploadId + "-g", "Retry duplicate builds " + suffix);

        var smuggled = new WorldRecord
        {
            Name = "Imported " + suffix,
            Builds = new List<BuildSnapshot>
            {
                new BuildSnapshot
                {
                    BuildId = "imported-build",
                    DisplayName = "Imported",
                    PosX = 5,
                    PosY = 6,
                    OwnerPlayerId = "someone-else",
                    ControllingPlayerId = "someone-else",
                    PlanetAddress = "Earth"
                }
            },
            Claims = new List<ClaimInfo>
            {
                new ClaimInfo
                {
                    Shape = ClaimShape.Region,
                    CenterX = 0,
                    CenterY = 0,
                    RadiusMeters = 100,
                    OwnerPlayerId = "someone-else",
                    TrustedPlayerIds = new List<string> { "someone-else" }
                }
            }
        };
        await SendUploadJsonAsync(stranger, uploadId, smuggled);
        WorldJoinAckPacket imported = await stranger.ReceiveTypeAsync<WorldJoinAckPacket>(PacketType.WorldJoinAck);
        if (!imported.Accepted) throw new InvalidOperationException("A valid upload was rejected: " + imported.RejectReason);
        if (imported.Builds.Count != 1 || imported.Builds[0].OwnerPlayerId != stranger.PlayerId)
            throw new InvalidOperationException("An imported build kept a foreign owner id.");
        if (!string.IsNullOrEmpty(imported.Builds[0].ControllingPlayerId))
            throw new InvalidOperationException("An imported build kept a controller from the save file.");
        if (imported.Claims.Count != 1 || imported.Claims[0].OwnerPlayerId != stranger.PlayerId)
            throw new InvalidOperationException("An imported claim kept a foreign owner id.");
        if (imported.Claims[0].TrustedPlayerIds.Count != 0)
            throw new InvalidOperationException("An imported claim smuggled trusted player ids.");

        await owner.SendAsync(PacketType.ClaimCreate, new ClaimCreatePacket { WorldId = publicWorld.WorldId, Shape = ClaimShape.Region, CenterX = 0, CenterY = 0, RadiusMeters = 0 });
        await ExpectErrorAsync(owner, "radius");

        await owner.SendAsync(PacketType.ClaimCreate, new ClaimCreatePacket { WorldId = publicWorld.WorldId, Shape = ClaimShape.Region, CenterX = 0, CenterY = 0, RadiusMeters = 400000 });
        await ExpectErrorAsync(owner, "radius");

        await owner.SendAsync(PacketType.ClaimCreate, new ClaimCreatePacket { WorldId = publicWorld.WorldId, Shape = (ClaimShape)99 });
        await ExpectErrorAsync(owner, "Unknown claim type");

        await owner.SendAsync(PacketType.ClaimCreate, new ClaimCreatePacket { WorldId = publicWorld.WorldId, Shape = ClaimShape.Region, CenterX = 10, CenterY = 20, RadiusMeters = 25 });
        ClaimInfo regionClaim = await owner.ReceiveTypeAsync<ClaimInfo>(PacketType.ClaimCreate);
        if (regionClaim.RadiusMeters != 25) throw new InvalidOperationException("A valid region claim was not stored as sent.");

        string probeBuildId = "ctl-" + suffix;
        await owner.SendAsync(PacketType.BuildSpawn, new BuildSnapshot { BuildId = probeBuildId, DisplayName = "Probe", PosX = 1, PosY = 1, PlanetAddress = "Earth" });
        await Task.Delay(400);
        await owner.SendAsync(PacketType.BuildControlRequest, new BuildControlRequestPacket { WorldId = publicWorld.WorldId, BuildId = probeBuildId });
        BuildControlGrantPacket grant = await owner.ReceiveTypeAsync<BuildControlGrantPacket>(PacketType.BuildControlGrant);
        if (!grant.Granted) throw new InvalidOperationException("The owner could not take control of its own build: " + grant.DenyReason);

        await owner.SendAsync(PacketType.WorldLeave, new { });
        await owner.SendAsync(PacketType.WorldJoin, new WorldJoinPacket { WorldId = publicWorld.WorldId });
        WorldJoinAckPacket rejoined = await owner.ReceiveTypeAsync<WorldJoinAckPacket>(PacketType.WorldJoinAck);
        BuildSnapshot released = rejoined.Builds.FirstOrDefault(b => b.BuildId == probeBuildId);
        if (released == null) throw new InvalidOperationException("The spawned build disappeared from the world.");
        if (!string.IsNullOrEmpty(released.ControllingPlayerId))
            throw new InvalidOperationException("Leaving the world did not release build control.");
        await owner.ReceiveTypeAsync<ChatMessagePacket>(PacketType.ChatMessage);

        await stranger.SendAsync(PacketType.WorldJoin, new WorldJoinPacket { WorldId = publicWorld.WorldId });
        WorldJoinAckPacket strangerInPublic = await stranger.ReceiveTypeAsync<WorldJoinAckPacket>(PacketType.WorldJoinAck);
        if (!strangerInPublic.Accepted) throw new InvalidOperationException("The stranger could not join the public world: " + strangerInPublic.RejectReason);
        await stranger.ReceiveTypeAsync<ChatMessagePacket>(PacketType.ChatMessage);
        await owner.ReceiveTypeAsync<ChatMessagePacket>(PacketType.ChatMessage);

        await stranger.SendAsync(PacketType.ClaimCreate, new ClaimCreatePacket { WorldId = publicWorld.WorldId, Shape = ClaimShape.Region, CenterX = 30, CenterY = 20, RadiusMeters = 25 });
        await ExpectErrorCodeAsync(stranger, "overlaps a region claimed by", "claim_rejected");

        await stranger.SendAsync(PacketType.ClaimCreate, new ClaimCreatePacket { WorldId = publicWorld.WorldId, Shape = ClaimShape.Region, CenterX = 400, CenterY = 400, RadiusMeters = 10 });
        ClaimInfo farClaim = await stranger.ReceiveTypeAsync<ClaimInfo>(PacketType.ClaimCreate);
        ClaimInfo farClaimOwner = await owner.ReceiveTypeAsync<ClaimInfo>(PacketType.ClaimCreate);
        if (farClaim.ClaimId != farClaimOwner.ClaimId) throw new InvalidOperationException("A non-overlapping region claim was not broadcast consistently.");

        await owner.SendAsync(PacketType.BuildSpawn, new BuildSnapshot { BuildId = "far-build-" + suffix, DisplayName = "Far", PosX = 500, PosY = 500, PlanetAddress = "Earth" });
        BuildSnapshot farBuild = await stranger.ReceiveTypeAsync<BuildSnapshot>(PacketType.BuildSpawn);
        if (farBuild.BuildId != "far-build-" + suffix) throw new InvalidOperationException("The far build did not reach the other player.");

        await stranger.SendAsync(PacketType.ClaimCreate, new ClaimCreatePacket { WorldId = publicWorld.WorldId, Shape = ClaimShape.Region, CenterX = 500, CenterY = 500, RadiusMeters = 10 });
        await ExpectErrorCodeAsync(stranger, "contains a build owned by", "claim_rejected");

        await owner.SendAsync(PacketType.ClaimCreate, new ClaimCreatePacket { WorldId = publicWorld.WorldId, Shape = ClaimShape.Region, CenterX = 10, CenterY = 20, RadiusMeters = 25 });
        ClaimInfo repeated = await owner.ReceiveTypeAsync<ClaimInfo>(PacketType.ClaimCreate);
        ClaimInfo repeatedSeenByOther = await stranger.ReceiveTypeAsync<ClaimInfo>(PacketType.ClaimCreate);
        if (repeated.ClaimId != regionClaim.ClaimId || repeatedSeenByOther.ClaimId != regionClaim.ClaimId)
            throw new InvalidOperationException("Re-claiming the same region created a duplicate claim instead of returning the existing one.");
        Console.WriteLine("[assert] region claims cannot overlap another player's region or build");

        if (TestSettings.PasswordPort > 0)
        {
            using TestPeer uploadA = await TestPeer.ConnectAsync("UpA_" + suffix, null, createAccount: true, port: TestSettings.PasswordPort, registrationPassword: TestSettings.Password);
            using TestPeer uploadB = await TestPeer.ConnectAsync("UpB_" + suffix, null, createAccount: true, port: TestSettings.PasswordPort, registrationPassword: TestSettings.Password);

            await SendUploadAsync(uploadA, "cap-a-" + suffix, 2, 0, "QUJD");
            await SyncAsync(uploadA);
            await SendUploadAsync(uploadB, "cap-b-" + suffix, 2, 0, "QUJD");
            await ExpectErrorAsync(uploadB, "Too many uploads");

            await Task.Delay(3200);
            await SendUploadAsync(uploadA, "cap-a-" + suffix, 2, 1, "QUJD");
            await ExpectErrorCodeAsync(uploadA, "expired", "upload_expired");

            var fresh = new WorldRecord { Name = "Fresh " + suffix };
            await SendUploadJsonAsync(uploadB, "cap-c-" + suffix, fresh);
            WorldJoinAckPacket freshAck = await uploadB.ReceiveTypeAsync<WorldJoinAckPacket>(PacketType.WorldJoinAck);
            if (!freshAck.Accepted) throw new InvalidOperationException("A fresh upload did not succeed after an expired one: " + freshAck.RejectReason);
        }

        Console.WriteLine("[assert] world data plane enforces access, upload bounds and controller release");
    }

    private static async Task VerifyRoomsAsync(string suffix)
    {
        using TestPeer host = await TestPeer.ConnectAsync("RoomHost_" + suffix, null, createAccount: true);
        using TestPeer guest = await TestPeer.ConnectAsync("RoomGuest_" + suffix, null, createAccount: true);
        using TestPeer other = await TestPeer.ConnectAsync("RoomOther_" + suffix, null, createAccount: true);

        await host.SendAsync(PacketType.RoomCreate, new RoomCreatePacket { Name = "Tiny " + suffix, Visibility = RoomVisibility.Public, Capacity = 1 });
        RoomStatePacket tiny = await host.ReceiveTypeAsync<RoomStatePacket>(PacketType.RoomState);
        if (tiny.Accepted || (tiny.Reason ?? string.Empty).IndexOf("capacity", StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException("A one-player room was not refused with a capacity reason.");

        await host.SendAsync(PacketType.RoomCreate, new RoomCreatePacket { Name = "Room " + suffix, Description = "Integration room.", Visibility = RoomVisibility.Public, Capacity = 2 });
        RoomStatePacket opened = await host.ReceiveTypeAsync<RoomStatePacket>(PacketType.RoomState);
        if (!opened.Accepted || opened.Room == null || opened.World == null || !opened.World.Accepted)
            throw new InvalidOperationException("A public room was not opened: " + opened.Reason);
        if (opened.Room.PlayersOnline != 1 || opened.Room.Capacity != 2 || opened.Room.Visibility != RoomVisibility.Public || opened.Room.Description != "Integration room.")
            throw new InvalidOperationException("The opened room summary was wrong.");
        string roomId = opened.Room.RoomId;
        string worldId = opened.Room.WorldId;
        if (string.IsNullOrEmpty(worldId)) throw new InvalidOperationException("A room was opened without a world.");

        await host.SendAsync(PacketType.RoomCreate, new RoomCreatePacket { Name = "Second " + suffix, Visibility = RoomVisibility.Public, Capacity = 4, WorldId = worldId });
        RoomStatePacket duplicateWorld = await host.ReceiveTypeAsync<RoomStatePacket>(PacketType.RoomState);
        if (duplicateWorld.Accepted || (duplicateWorld.Reason ?? string.Empty).IndexOf("already hosted", StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException("A world hosted by one room was accepted by a second room.");

        await guest.SendAsync(PacketType.RoomListRequest, new { });
        RoomListResponsePacket listed = await guest.ReceiveTypeAsync<RoomListResponsePacket>(PacketType.RoomListResponse);
        RoomSummary row = listed.Rooms.FirstOrDefault(r => r.RoomId == roomId);
        if (row == null || row.PlayersOnline != 1 || row.HostPlayerName != "RoomHost_" + suffix)
            throw new InvalidOperationException("A public room was not listed with its live population.");

        await guest.SendAsync(PacketType.RoomJoin, new RoomJoinPacket { RoomId = roomId });
        RoomStatePacket joined = await guest.ReceiveTypeAsync<RoomStatePacket>(PacketType.RoomState);
        if (!joined.Accepted || joined.Room == null || joined.Room.PlayersOnline != 2 || joined.World == null || joined.World.WorldId != worldId)
            throw new InvalidOperationException("A guest could not join the room: " + joined.Reason);
        await guest.ReceiveTypeAsync<ChatMessagePacket>(PacketType.ChatMessage);
        await host.ReceiveTypeAsync<ChatMessagePacket>(PacketType.ChatMessage);

        await other.SendAsync(PacketType.RoomJoin, new RoomJoinPacket { RoomId = roomId });
        RoomStatePacket full = await other.ReceiveTypeAsync<RoomStatePacket>(PacketType.RoomState);
        if (full.Accepted || (full.Reason ?? string.Empty).IndexOf("full", StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException("The room capacity was not enforced.");

        await guest.SendAsync(PacketType.RoomClose, new RoomClosePacket { RoomId = roomId });
        RoomStatePacket closeDenied = await guest.ReceiveTypeAsync<RoomStatePacket>(PacketType.RoomState);
        if (closeDenied.Accepted || (closeDenied.Reason ?? string.Empty).IndexOf("host", StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException("A guest was allowed to close someone else's room.");

        await guest.SendAsync(PacketType.RoomLeave, new { });
        RoomStatePacket guestLeft = await guest.ReceiveTypeAsync<RoomStatePacket>(PacketType.RoomState);
        if (!guestLeft.Accepted || guestLeft.Room != null) throw new InvalidOperationException("Leaving the room did not clear it.");

        await other.SendAsync(PacketType.RoomJoin, new RoomJoinPacket { RoomId = roomId });
        RoomStatePacket otherJoined = await other.ReceiveTypeAsync<RoomStatePacket>(PacketType.RoomState);
        if (!otherJoined.Accepted) throw new InvalidOperationException("The freed room slot could not be used: " + otherJoined.Reason);
        await other.ReceiveTypeAsync<ChatMessagePacket>(PacketType.ChatMessage);
        await host.ReceiveTypeAsync<ChatMessagePacket>(PacketType.ChatMessage);

        await host.SendAsync(PacketType.RoomClose, new RoomClosePacket { RoomId = roomId });
        RoomClosedPacket hostClosed = await host.ReceiveTypeAsync<RoomClosedPacket>(PacketType.RoomClosed);
        RoomClosedPacket otherClosed = await other.ReceiveTypeAsync<RoomClosedPacket>(PacketType.RoomClosed);
        if (hostClosed.RoomId != roomId || otherClosed.RoomId != roomId)
            throw new InvalidOperationException("Room closure was not delivered to every member.");

        await host.SendAsync(PacketType.RoomListRequest, new { });
        RoomListResponsePacket afterClose = await host.ReceiveTypeAsync<RoomListResponsePacket>(PacketType.RoomListResponse);
        if (afterClose.Rooms.Any(r => r.RoomId == roomId)) throw new InvalidOperationException("A closed room was still listed.");

        await host.SendAsync(PacketType.RoomCreate, new RoomCreatePacket { Name = "Friends " + suffix, Visibility = RoomVisibility.Friends, Capacity = 4 });
        RoomStatePacket friendsRoom = await host.ReceiveTypeAsync<RoomStatePacket>(PacketType.RoomState);
        if (!friendsRoom.Accepted) throw new InvalidOperationException("A friends-only room was not opened: " + friendsRoom.Reason);
        string friendsRoomId = friendsRoom.Room.RoomId;

        await other.SendAsync(PacketType.RoomJoin, new RoomJoinPacket { RoomId = friendsRoomId });
        RoomStatePacket refused = await other.ReceiveTypeAsync<RoomStatePacket>(PacketType.RoomState);
        if (refused.Accepted || (refused.Reason ?? string.Empty).IndexOf("friends-only", StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException("A stranger joined a friends-only room.");

        await host.SendAsync(PacketType.FriendRequest, new FriendRequestPacket { TargetPlayerName = "RoomGuest_" + suffix });
        await guest.ReceiveTypeAsync<FriendListResponsePacket>(PacketType.FriendListResponse);
        await host.ReceiveTypeAsync<FriendListResponsePacket>(PacketType.FriendListResponse);
        await guest.SendAsync(PacketType.FriendRequestResponse, new FriendRequestResponsePacket { FromPlayerId = host.PlayerId, Accepted = true });
        await guest.ReceiveTypeAsync<FriendListResponsePacket>(PacketType.FriendListResponse);
        await host.ReceiveTypeAsync<FriendListResponsePacket>(PacketType.FriendListResponse);

        await guest.SendAsync(PacketType.RoomListRequest, new { });
        RoomListResponsePacket friendListing = await guest.ReceiveTypeAsync<RoomListResponsePacket>(PacketType.RoomListResponse);
        if (!friendListing.Rooms.Any(r => r.RoomId == friendsRoomId))
            throw new InvalidOperationException("A friends-only room was not listed to the host's friend.");

        await other.SendAsync(PacketType.WorldJoin, new WorldJoinPacket { WorldId = friendsRoom.Room.WorldId });
        RoomStatePacket worldRouteRefused = await other.ReceiveTypeAsync<RoomStatePacket>(PacketType.RoomState);
        if (worldRouteRefused.Accepted || (worldRouteRefused.Reason ?? string.Empty).IndexOf("friends-only", StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException("Joining a room-hosted world directly bypassed the room's visibility rule.");

        await guest.SendAsync(PacketType.WorldJoin, new WorldJoinPacket { WorldId = friendsRoom.Room.WorldId });
        RoomStatePacket friendJoined = await guest.ReceiveTypeAsync<RoomStatePacket>(PacketType.RoomState);
        if (!friendJoined.Accepted || friendJoined.Room == null || friendJoined.Room.RoomId != friendsRoomId)
            throw new InvalidOperationException("A friend joining the hosted world was not routed into the room: " + friendJoined.Reason);
        await guest.ReceiveTypeAsync<ChatMessagePacket>(PacketType.ChatMessage);
        await host.ReceiveTypeAsync<ChatMessagePacket>(PacketType.ChatMessage);

        await guest.SendAsync(PacketType.RoomLeave, new { });
        await guest.ReceiveTypeAsync<RoomStatePacket>(PacketType.RoomState);
        await host.SendAsync(PacketType.RoomClose, new RoomClosePacket { RoomId = friendsRoomId });
        await host.ReceiveTypeAsync<RoomClosedPacket>(PacketType.RoomClosed);

        await host.SendAsync(PacketType.RoomCreate, new RoomCreatePacket { Name = "Empty " + suffix, Visibility = RoomVisibility.Public, Capacity = 4 });
        RoomStatePacket emptyRoom = await host.ReceiveTypeAsync<RoomStatePacket>(PacketType.RoomState);
        if (!emptyRoom.Accepted) throw new InvalidOperationException("Could not open the room used to test auto-close: " + emptyRoom.Reason);
        await host.SendAsync(PacketType.RoomLeave, new { });
        await host.ReceiveTypeAsync<RoomStatePacket>(PacketType.RoomState);
        await host.SendAsync(PacketType.RoomListRequest, new { });
        RoomListResponsePacket afterEmpty = await host.ReceiveTypeAsync<RoomListResponsePacket>(PacketType.RoomListResponse);
        if (afterEmpty.Rooms.Any(r => r.RoomId == emptyRoom.Room.RoomId))
            throw new InvalidOperationException("A room that became empty stayed listed.");

        await host.SendAsync(PacketType.WorldCreate, new WorldCreatePacket { Name = "Private Hosted " + suffix, IsPublic = false });
        WorldJoinAckPacket privateHosted = await host.ReceiveTypeAsync<WorldJoinAckPacket>(PacketType.WorldJoinAck);
        if (!privateHosted.Accepted) throw new InvalidOperationException("Could not create the world used for the visibility check.");
        await host.SendAsync(PacketType.RoomCreate, new RoomCreatePacket { Name = "Public Over Private " + suffix, Visibility = RoomVisibility.Public, Capacity = 4, WorldId = privateHosted.WorldId });
        RoomStatePacket publicOverPrivate = await host.ReceiveTypeAsync<RoomStatePacket>(PacketType.RoomState);
        if (publicOverPrivate.Accepted || (publicOverPrivate.Reason ?? string.Empty).IndexOf("public world", StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException("A public room was opened over a private world, which makes the room and world policies disagree.");

        Console.WriteLine("[assert] rooms are server-enforced: capacity, visibility, host close and auto-close");
    }

    private static async Task ExpectErrorCodeAsync(TestPeer peer, string fragment, string code)
    {
        for (int attempt = 0; attempt < 64; attempt++)
        {
            (PacketType type, string json) = await peer.ReceiveAsync();
            if (type == PacketType.WorldTimeState || type == PacketType.ChatMessage) continue;
            if (type != PacketType.Error) throw new InvalidOperationException($"Expected error code '{code}' but got {type}: {json}");
            var error = JsonConvert.DeserializeObject<ErrorPacket>(json);
            string message = error?.Message ?? string.Empty;
            if (message.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException($"Expected an error containing '{fragment}' but got '{message}'.");
            if (!string.Equals(error?.Code, code, StringComparison.Ordinal))
                throw new InvalidOperationException($"Expected error code '{code}' but got '{error?.Code ?? "none"}' for '{message}'.");
            return;
        }
        throw new TimeoutException($"Did not receive error code '{code}'.");
    }

    private static async Task ExpectUploadSucceedsAsync(TestPeer peer, string uploadId, string name)
    {
        await SendUploadJsonAsync(peer, uploadId, new WorldRecord { Name = name });
        WorldJoinAckPacket ack = await peer.ReceiveTypeAsync<WorldJoinAckPacket>(PacketType.WorldJoinAck);
        if (!ack.Accepted) throw new InvalidOperationException($"A valid upload did not succeed after a rejected attempt ({name}): " + ack.RejectReason);
    }

    private static async Task SyncAsync(TestPeer peer)
    {
        await peer.SendAsync(PacketType.ServerInfoRequest, new { });
        await peer.ReceiveTypeAsync<ServerInfoResponsePacket>(PacketType.ServerInfoResponse);
    }

    private static Task SendUploadAsync(TestPeer peer, string uploadId, int totalChunks, int index, string base64, string worldName = null)
        => peer.SendAsync(PacketType.WorldUploadChunk, new WorldUploadChunkPacket
        {
            UploadId = uploadId,
            ChunkIndex = index,
            TotalChunks = totalChunks,
            Base64Data = base64,
            WorldName = worldName,
            IsPublic = false
        });

    private static async Task SendUploadJsonAsync(TestPeer peer, string uploadId, WorldRecord world)
    {
        string json = JsonConvert.SerializeObject(world);
        string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        const int chunkSize = 64 * 1024;
        int totalChunks = Math.Max(1, (base64.Length + chunkSize - 1) / chunkSize);
        for (int index = 0; index < totalChunks; index++)
        {
            string piece = base64.Substring(index * chunkSize, Math.Min(chunkSize, base64.Length - index * chunkSize));
            await SendUploadAsync(peer, uploadId, totalChunks, index, piece, index == 0 ? world.Name : null);
        }
    }

    private static async Task ExpectErrorAsync(TestPeer peer, string fragment)
    {
        for (int attempt = 0; attempt < 64; attempt++)
        {
            (PacketType type, string json) = await peer.ReceiveAsync();
            if (type == PacketType.WorldTimeState || type == PacketType.ChatMessage) continue;
            if (type == PacketType.Error)
            {
                var error = JsonConvert.DeserializeObject<ErrorPacket>(json);
                string message = error?.Message ?? string.Empty;
                if (message.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException($"Expected an error containing '{fragment}' but got '{message}'.");
                return;
            }
            throw new InvalidOperationException($"Expected an error containing '{fragment}' but got {type}: {json}");
        }
        throw new TimeoutException($"Did not receive an error containing '{fragment}'.");
    }

    private static async Task ExpectDownloadAsync(TestPeer peer, string worldId)
    {
        int received = 0;
        for (int attempt = 0; attempt < 256; attempt++)
        {
            (PacketType type, string json) = await peer.ReceiveAsync();
            if (type == PacketType.WorldTimeState || type == PacketType.ChatMessage) continue;
            if (type == PacketType.Error)
            {
                var error = JsonConvert.DeserializeObject<ErrorPacket>(json);
                throw new InvalidOperationException("World download was refused: " + error?.Message);
            }
            if (type != PacketType.WorldDownloadChunk)
                throw new InvalidOperationException($"Expected a download chunk but got {type}.");
            var chunk = JsonConvert.DeserializeObject<WorldDownloadChunkPacket>(json);
            if (chunk == null || chunk.WorldId != worldId) throw new InvalidOperationException("Download chunk belongs to another world.");
            if (chunk.ChunkIndex != received) throw new InvalidOperationException($"Download chunks arrived out of order: expected {received}, got {chunk.ChunkIndex}.");
            received++;
            if (chunk.ChunkIndex == chunk.TotalChunks - 1) return;
        }
        throw new TimeoutException("World download did not complete.");
    }

    private static async Task VerifyAccountModesAsync(string suffix)
    {
        string accountName = "Mode_" + suffix;

        await TestPeer.ExpectHandshakeRejectionAsync(accountName, null, "No account exists", createAccount: false, expectedCode: AuthErrorCode.UnknownAccount);

        string token;
        string playerId;
        using (TestPeer registered = await TestPeer.ConnectAsync(accountName, null, createAccount: true))
        {
            if (string.IsNullOrEmpty(registered.IssuedToken))
                throw new InvalidOperationException("Explicit registration did not issue a token.");
            token = registered.IssuedToken;
            playerId = registered.PlayerId;
        }

        using (TestPeer login = await TestPeer.ConnectAsync(accountName, token))
        {
            if (!string.Equals(login.PlayerId, playerId, StringComparison.Ordinal))
                throw new InvalidOperationException("Token login did not restore the registered account.");
        }

        using (TestPeer spaced = await TestPeer.ConnectAsync("  " + accountName.ToUpperInvariant() + "  ", token))
        {
            if (!string.Equals(spaced.PlayerId, playerId, StringComparison.Ordinal))
                throw new InvalidOperationException("A case and whitespace variant resolved to a different account.");
        }

        await TestPeer.ExpectHandshakeRejectionAsync(accountName, null, "already registered", createAccount: true, expectedCode: AuthErrorCode.NameTaken);
        await TestPeer.ExpectHandshakeRejectionAsync(" " + accountName.ToUpperInvariant() + " ", null, "already registered", createAccount: true, expectedCode: AuthErrorCode.NameTaken);
        await TestPeer.ExpectHandshakeRejectionAsync(accountName, "definitely-not-a-valid-token", "not accepted", createAccount: false, expectedCode: AuthErrorCode.BadToken);
        await TestPeer.ExpectHandshakeRejectionAsync("Nobody_" + suffix, "definitely-not-a-valid-token", null, createAccount: false, expectedCode: AuthErrorCode.UnknownAccount);

        if (TestSettings.ClosedPort > 0)
        {
            await TestPeer.ExpectHandshakeRejectionAsync("Closed_" + suffix, null, "not accepting new registrations", createAccount: true, expectedCode: AuthErrorCode.RegistrationDisabled, port: TestSettings.ClosedPort);
            await TestPeer.ExpectHandshakeRejectionAsync("Closed_" + suffix, null, "not accepting new registrations", createAccount: false, expectedCode: AuthErrorCode.UnknownAccount, port: TestSettings.ClosedPort);
        }

        if (TestSettings.PasswordPort > 0)
        {
            string gatedName = "Gated_" + suffix;
            await TestPeer.ExpectHandshakeRejectionAsync(gatedName, null, "requires a registration password", createAccount: true, expectedCode: AuthErrorCode.RegistrationPasswordRequired, port: TestSettings.PasswordPort);
            await TestPeer.ExpectHandshakeRejectionAsync(gatedName, null, "incorrect", createAccount: true, expectedCode: AuthErrorCode.BadRegistrationPassword, port: TestSettings.PasswordPort, registrationPassword: "wrong-" + TestSettings.Password);

            string gatedToken;
            string gatedPlayerId;
            using (TestPeer gated = await TestPeer.ConnectAsync(gatedName, null, createAccount: true, port: TestSettings.PasswordPort, registrationPassword: TestSettings.Password))
            {
                if (string.IsNullOrEmpty(gated.IssuedToken))
                    throw new InvalidOperationException("Password-protected registration did not issue a token, so the refused attempt must have created the account.");
                gatedToken = gated.IssuedToken;
                gatedPlayerId = gated.PlayerId;
            }

            using (TestPeer gatedLogin = await TestPeer.ConnectAsync(gatedName, gatedToken, port: TestSettings.PasswordPort))
            {
                if (!string.Equals(gatedLogin.PlayerId, gatedPlayerId, StringComparison.Ordinal))
                    throw new InvalidOperationException("An existing account could not sign in on a password-protected server without re-entering the registration password.");
            }

            using (TestPeer gatedLoginKeep = await TestPeer.ConnectAsync(gatedName, gatedToken, port: TestSettings.PasswordPort, registrationPassword: "wrong-" + TestSettings.Password))
            {
                if (!string.Equals(gatedLoginKeep.PlayerId, gatedPlayerId, StringComparison.Ordinal))
                    throw new InvalidOperationException("A token sign-in was blocked by the registration password, which must only gate account creation.");
            }

            await TestPeer.ExpectHandshakeRejectionAsync(gatedName, null, "already registered", createAccount: true, expectedCode: AuthErrorCode.NameTaken, port: TestSettings.PasswordPort, registrationPassword: TestSettings.Password);
            await TestPeer.ExpectHandshakeRejectionAsync("Nobody_" + suffix, null, null, createAccount: false, expectedCode: AuthErrorCode.UnknownAccount, port: TestSettings.PasswordPort);
            await TestPeer.ExpectHandshakeRejectionAsync("Nobody_" + suffix, null, "requires a registration password", createAccount: true, expectedCode: AuthErrorCode.RegistrationPasswordRequired, port: TestSettings.PasswordPort);

            var repeated = new List<(string Name, string Token, string PlayerId)>();
            for (int index = 0; index < 20; index++)
            {
                string repeatedName = "Seq" + index + "_" + suffix;
                using TestPeer registered = await TestPeer.ConnectAsync(repeatedName, null, createAccount: true, port: TestSettings.PasswordPort, registrationPassword: TestSettings.Password);
                if (string.IsNullOrEmpty(registered.IssuedToken))
                    throw new InvalidOperationException($"Sequential password registration {index} did not issue a token.");
                await SyncAsync(registered);
                repeated.Add((repeatedName, registered.IssuedToken, registered.PlayerId));
            }

            foreach (var entry in repeated)
            {
                using TestPeer again = await TestPeer.ConnectAsync(entry.Name, entry.Token, port: TestSettings.PasswordPort);
                if (!string.Equals(again.PlayerId, entry.PlayerId, StringComparison.Ordinal))
                    throw new InvalidOperationException("A repeated token sign-in on a password-protected server restored a different account.");
                await SyncAsync(again);
            }
        }

        Console.WriteLine("[assert] account authentication is explicit and failures are distinct");
    }

    private static void AcceptPin(ServerIdentityPinStore pins, string endpointKey, ServerIdentity identity)
    {
        DateTime now = DateTime.UtcNow;
        bool accepted = pins.Accept(new ServerIdentityPin
        {
            EndpointKey = endpointKey,
            Fingerprint = identity.Fingerprint,
            ShortFingerprint = identity.ShortFingerprint,
            Origin = "test",
            FirstSeenUtc = now,
            LastVerifiedUtc = now
        });
        if (!accepted) throw new InvalidOperationException("The pin store rejected a valid pin.");
    }

    private static void VerifyIdentityPinning()
    {
        var storage = new MemoryEndpointStorage();
        var pins = new ServerIdentityPinStore(storage);

        P256.PublicPoint(P256.GeneratePrivateKey(), out byte[] ax, out byte[] ay);
        ServerIdentity a = ServerIdentityCalculator.FromStaticPoint(ax, ay);
        if (!a.Valid) throw new InvalidOperationException("A valid static point was rejected.");
        if (a.Fingerprint.Length != 64 || !string.Equals(a.Fingerprint, a.Fingerprint.ToLowerInvariant(), StringComparison.Ordinal))
            throw new InvalidOperationException("The identity fingerprint must be 64 lowercase hex characters.");
        if (a.ShortFingerprint.Length != 19 || a.ShortFingerprint.Count(c => c == '-') != 3)
            throw new InvalidOperationException("The short fingerprint must be four grouped blocks.");

        P256.PublicPoint(P256.GeneratePrivateKey(), out byte[] bx, out byte[] by);
        ServerIdentity b = ServerIdentityCalculator.FromStaticPoint(bx, by);
        if (string.Equals(a.Fingerprint, b.Fingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("Two different identity keys produced the same fingerprint.");

        const string key = "example.com:7777";
        if (pins.Lookup(key, a.Fingerprint).Match != PinMatch.Missing)
            throw new InvalidOperationException("An unknown endpoint must report no pin.");

        AcceptPin(pins, key, a);
        if (pins.Lookup(key, a.Fingerprint).Match != PinMatch.Match)
            throw new InvalidOperationException("An accepted pin did not match its own fingerprint.");

        PinLookup changed = pins.Lookup(key, b.Fingerprint);
        if (changed.Match != PinMatch.Changed)
            throw new InvalidOperationException("A changed identity must be reported as changed.");
        if (!string.Equals(changed.PinnedFingerprint, a.Fingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("The changed lookup did not report the pinned fingerprint.");
        if (!string.Equals(pins.All().Single().Fingerprint, a.Fingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("A refused identity changed the stored pin.");
        if (pins.Lookup("example.com#auto", a.Fingerprint).Match != PinMatch.Missing)
            throw new InvalidOperationException("Two endpoints shared a pin.");

        var reloaded = new ServerIdentityPinStore(storage);
        if (reloaded.Lookup(key, a.Fingerprint).Match != PinMatch.Match)
            throw new InvalidOperationException("A pin did not survive a store reload.");

        AcceptPin(pins, key, b);
        ServerIdentityPin repinned = pins.All().Single();
        if (repinned.RePinCount != 1 || !string.Equals(repinned.PreviousFingerprint, a.Fingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("A deliberate re-pin did not record its previous fingerprint.");

        if (ServerIdentityCalculator.FromStaticPoint(null, ay).Valid)
            throw new InvalidOperationException("A missing identity key was accepted.");
        if (ServerIdentityCalculator.FromStaticPoint(new byte[31], ay).Valid)
            throw new InvalidOperationException("A short identity key was accepted.");
        byte[] offCurve = (byte[])ax.Clone();
        offCurve[0] ^= 0xFF;
        if (ServerIdentityCalculator.FromStaticPoint(offCurve, ay).Valid)
            throw new InvalidOperationException("An off-curve identity key was accepted.");

        storage.Write(ServerIdentityPinStore.StorageKey, "{ not valid json");
        var corrupt = new ServerIdentityPinStore(storage);
        if (corrupt.Lookup(key, a.Fingerprint).Match != PinMatch.Missing)
            throw new InvalidOperationException("A corrupt pin store produced a match.");
        AcceptPin(corrupt, key, a);
        if (corrupt.Lookup(key, a.Fingerprint).Match != PinMatch.Match)
            throw new InvalidOperationException("An explicit acceptance could not recover a corrupt pin store.");
        if (string.IsNullOrEmpty(storage.Read(ServerIdentityPinStore.CorruptBackupKey)))
            throw new InvalidOperationException("A corrupt pin document was not preserved.");

        Console.WriteLine("[assert] identity pins: first use, match, changed, no overwrite, persistence, corrupt store");
    }

    private static void VerifyTokenMigration()
    {
        var storage = new MemoryEndpointStorage();
        var tokens = new EndpointTokenStore(storage);

        if (tokens.MigrateLegacyToken("example.com:7777", "legacy-token") != TokenMigrationStatus.Migrated)
            throw new InvalidOperationException("A legacy token was not migrated to its endpoint.");
        if (tokens.GetToken("example.com:7777") != "legacy-token")
            throw new InvalidOperationException("The migrated token was not readable.");
        if (tokens.GetToken("example.com#auto") != null)
            throw new InvalidOperationException("A token leaked to a different endpoint.");

        if (tokens.MigrateLegacyToken("example.com:7777", "second") != TokenMigrationStatus.SkippedExisting)
            throw new InvalidOperationException("A migration overwrote an existing endpoint token.");
        if (tokens.GetToken("example.com:7777") != "legacy-token")
            throw new InvalidOperationException("An existing endpoint token was replaced.");

        if (tokens.MigrateLegacyToken(null, "legacy-token") != TokenMigrationStatus.SkippedAmbiguous)
            throw new InvalidOperationException("An ambiguous legacy token was migrated.");
        if (tokens.GetToken("other.example#auto") != null)
            throw new InvalidOperationException("An ambiguous legacy token was attached to an endpoint.");

        if (tokens.MigrateLegacyToken("example.com:7777", null) != TokenMigrationStatus.NoLegacyToken)
            throw new InvalidOperationException("A missing legacy token was reported as migrated.");

        var reloaded = new EndpointTokenStore(storage);
        if (reloaded.GetToken("example.com:7777") != "legacy-token")
            throw new InvalidOperationException("An endpoint token did not survive a store reload.");

        var history = new List<EndpointHistoryCandidate>
        {
            new EndpointHistoryCandidate { Host = "old.example", Port = null, LastConnectedUtc = DateTime.UtcNow.AddHours(-3) },
            new EndpointHistoryCandidate { Host = "EXAMPLE.com.", Port = 7777, LastConnectedUtc = DateTime.UtcNow.AddHours(-1) },
            new EndpointHistoryCandidate { Host = "newer-but-failed.example", Port = 7777, LastConnectedUtc = DateTime.UtcNow.AddHours(-2) }
        };
        if (!string.Equals(EndpointKey.LastSuccessfulKey(history), "example.com:7777", StringComparison.Ordinal))
            throw new InvalidOperationException("Legacy token migration did not select the most recent successful endpoint.");
        if (EndpointKey.LastSuccessfulKey(new List<EndpointHistoryCandidate>()) != null)
            throw new InvalidOperationException("Legacy token migration produced a key without history.");
        if (EndpointKey.LastSuccessfulKey(new List<EndpointHistoryCandidate> { new EndpointHistoryCandidate { Host = null, Port = 7777, LastConnectedUtc = DateTime.UtcNow } }) != null)
            throw new InvalidOperationException("Legacy token migration produced a key from an invalid history entry.");

        Console.WriteLine("[assert] endpoint tokens migrate once and never cross endpoints");
    }

    private static async Task VerifyIdentityGateBlocksProofAsync()
    {
        byte[] serverPrivate = P256.GeneratePrivateKey();
        P256.PublicPoint(serverPrivate, out byte[] sx, out byte[] sy);
        ServerIdentity identity = ServerIdentityCalculator.FromStaticPoint(sx, sy);

        var firstUseTransport = new ScriptedHandshakeTransport(serverPrivate);
        var firstUse = await new SecureHandshakeClient(firstUseTransport)
            .RunAsync("Pilot", string.Empty, "0.1.0", null, CancellationToken.None, new StubIdentityGate("example.com:7777", ServerIdentityDecision.FirstUse));
        if (firstUse.Accepted) throw new InvalidOperationException("An untrusted identity completed the handshake.");
        if (firstUse.IdentityDecision != ServerIdentityDecision.FirstUse)
            throw new InvalidOperationException("An untrusted identity was not reported as first use.");
        if (!string.Equals(firstUse.PresentedFingerprint, identity.Fingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("The client did not recompute the fingerprint from the static key.");
        if (firstUseTransport.ProofSent)
            throw new InvalidOperationException("The client sent an auth proof to a server whose identity was not trusted.");

        var changedTransport = new ScriptedHandshakeTransport(serverPrivate);
        var changed = await new SecureHandshakeClient(changedTransport)
            .RunAsync("Pilot", string.Empty, "0.1.0", null, CancellationToken.None, new StubIdentityGate("example.com:7777", ServerIdentityDecision.Changed));
        if (changed.Accepted || changed.IdentityDecision != ServerIdentityDecision.Changed)
            throw new InvalidOperationException("A changed identity was not refused.");
        if (changedTransport.ProofSent)
            throw new InvalidOperationException("The client sent an auth proof to a server with a changed identity.");

        var trustedTransport = new ScriptedHandshakeTransport(serverPrivate);
        SecureHandshakeResult trusted = await new SecureHandshakeClient(trustedTransport)
            .RunAsync("Pilot", string.Empty, "0.1.0", null, CancellationToken.None, new StubIdentityGate("example.com:7777", ServerIdentityDecision.Trusted));
        if (!trusted.Accepted || !trusted.IdentityProofVerified)
            throw new InvalidOperationException("A trusted server that proved its identity was not accepted: " + (trusted.RejectReason ?? "no reason"));
        if (!trustedTransport.IdentityProofSent)
            throw new InvalidOperationException("The scripted server never sent an identity proof.");
        if (!trustedTransport.ProofSent)
            throw new InvalidOperationException("A trusted server did not receive the auth proof.");

        var silentTransport = new ScriptedHandshakeTransport(serverPrivate, ScriptedProofMode.Omit);
        SecureHandshakeResult silent = await new SecureHandshakeClient(silentTransport)
            .RunAsync("Pilot", string.Empty, "0.1.0", null, CancellationToken.None, new StubIdentityGate("example.com:7777", ServerIdentityDecision.Trusted));
        if (silent.Accepted || silent.ErrorCode != AuthErrorCode.IdentityProofMissing)
            throw new InvalidOperationException($"A server that never proved its identity key was not refused with {AuthErrorCode.IdentityProofMissing} (got {silent.ErrorCode ?? "acceptance"}).");
        if (silentTransport.ProofSent)
            throw new InvalidOperationException("The client sent a token proof to a server that never proved its identity key.");

        var forgedTransport = new ScriptedHandshakeTransport(serverPrivate, ScriptedProofMode.WrongMac);
        SecureHandshakeResult forged = await new SecureHandshakeClient(forgedTransport)
            .RunAsync("Pilot", string.Empty, "0.1.0", null, CancellationToken.None, new StubIdentityGate("example.com:7777", ServerIdentityDecision.Trusted));
        if (forged.Accepted || forged.ErrorCode != AuthErrorCode.IdentityProofInvalid)
            throw new InvalidOperationException($"A server with a forged identity proof was not refused with {AuthErrorCode.IdentityProofInvalid} (got {forged.ErrorCode ?? "acceptance"}).");
        if (forgedTransport.ProofSent)
            throw new InvalidOperationException("The client sent a token proof to a server that failed the identity proof.");

        Console.WriteLine("[assert] identity is decided and proven before the auth proof is sent");
    }

    private static async Task VerifyAccountNameCanonicalizationAsync(string name, string token, string playerId)
    {
        TestPeer padded = await TestPeer.ConnectAsync(name + " ", token);
        if (!string.Equals(padded.PlayerId, playerId, StringComparison.Ordinal))
            throw new InvalidOperationException("A whitespace-padded player name created or matched a different account.");
        padded.Dispose();

        TestPeer lower = await TestPeer.ConnectAsync(" " + name.ToLowerInvariant(), token);
        if (!string.Equals(lower.PlayerId, playerId, StringComparison.Ordinal))
            throw new InvalidOperationException("A differently-cased player name created or matched a different account.");
        lower.Dispose();

        Console.WriteLine("[assert] player names are canonicalized before lookup");
    }
}

sealed class MemoryEndpointStorage : IEndpointStorage
{
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public string Read(string key) => _values.TryGetValue(key, out string value) ? value : null;

    public bool Write(string key, string value)
    {
        _values[key] = value;
        return true;
    }

    public void Remove(string key) => _values.Remove(key);
}

sealed class DroppingEndpointStorage : IEndpointStorage
{
    private readonly IEndpointStorage _inner;

    public DroppingEndpointStorage(IEndpointStorage inner) => _inner = inner;

    public bool DropWrites { get; set; }
    public bool ThrowOnRead { get; set; }

    public string Read(string key)
    {
        if (ThrowOnRead) throw new InvalidOperationException("storage read failed");
        return _inner.Read(key);
    }

    public bool Write(string key, string value)
    {
        if (DropWrites) return true;
        return _inner.Write(key, value);
    }

    public void Remove(string key) => _inner.Remove(key);
}

sealed class StubIdentityGate : IServerIdentityGate
{
    private readonly ServerIdentityDecision _decision;

    public string EndpointKey { get; }

    public StubIdentityGate(string endpointKey, ServerIdentityDecision decision)
    {
        EndpointKey = endpointKey;
        _decision = decision;
    }

    public ServerIdentityDecision Evaluate(ServerIdentity identity, out string pinnedFingerprint)
    {
        pinnedFingerprint = _decision == ServerIdentityDecision.Changed ? new string('b', 64) : null;
        return _decision;
    }
}

enum ScriptedProofMode
{
    Correct,
    Omit,
    WrongMac
}

sealed class ScriptedHandshakeTransport : ISecureTransport
{
    private readonly BlockingCollection<RawFrame> _frames = new();
    private readonly byte[] _staticPrivate;
    private readonly byte[] _staticX;
    private readonly byte[] _staticY;
    private readonly ScriptedProofMode _mode;
    private byte[] _exchangeIdentifier;
    private byte[] _serverChallenge;
    private byte[] _clientNonce;
    private string _playerName;
    private bool _keyExchangeSent;
    private SecureChannel _serverChannel;
    private byte[] _sessionId;

    public bool ProofSent;
    public bool IdentityProofSent;

    public ScriptedHandshakeTransport(byte[] staticPrivate, ScriptedProofMode mode = ScriptedProofMode.Correct)
    {
        _staticPrivate = staticPrivate;
        _mode = mode;
        P256.PublicPoint(staticPrivate, out _staticX, out _staticY);
    }

    public Task SendPlainAsync(PacketType type, object payload, CancellationToken ct)
    {
        if (type == PacketType.Hello)
        {
            var hello = (HelloPacket)payload;
            _clientNonce = hello.ClientNonce;
            _playerName = hello.PlayerName;
            _serverChallenge = SessionAuth.RandomBytes(SessionAuth.ChallengeBytes);
            _exchangeIdentifier = SessionAuth.RandomBytes(SessionAuth.SessionIdBytes);
            Enqueue(PacketType.HelloAck, new HelloAckPacket
            {
                Accepted = true,
                ServerChallenge = _serverChallenge,
                ClientNonce = hello.ClientNonce,
                ExchangeIdentifier = _exchangeIdentifier
            });
        }
        else if (type == PacketType.AuthResponse)
        {
            var response = (AuthResponsePacket)payload;
            if (response.Proof != null || response.RegistrationSecret != null)
            {
                ProofSent = true;
                if (_serverChannel != null)
                {
                    EnqueueSealed(_serverChannel, PacketType.AuthResult, new AuthResultPacket
                    {
                        Accepted = true,
                        PlayerId = "scripted-player",
                        SessionId = _sessionId
                    });
                }
            }
            else if (!_keyExchangeSent)
            {
                _keyExchangeSent = true;
                byte[] serverEphemeralPrivate = P256.GeneratePrivateKey();
                P256.PublicPoint(serverEphemeralPrivate, out byte[] serverX, out byte[] serverY);
                Enqueue(PacketType.AuthChallenge, new AuthChallengePacket
                {
                    EphemeralX = serverX,
                    EphemeralY = serverY,
                    StaticX = _staticX,
                    StaticY = _staticY,
                    IdentityFingerprint = "untrusted-server-supplied-value",
                    ExchangeIdentifier = _exchangeIdentifier,
                    FinalFlag = true
                });

                byte[] ephemeralSecret = P256.DeriveSharedSecret(serverEphemeralPrivate, response.EphemeralX, response.EphemeralY);
                byte[] staticSecret = P256.DeriveSharedSecret(_staticPrivate, response.EphemeralX, response.EphemeralY);
                byte[] transcript = SessionAuth.ComputeTranscriptHash(_clientNonce, _serverChallenge, response.EphemeralX, response.EphemeralY, serverX, serverY, _staticX, _staticY, _playerName);
                byte[] preMaster = SessionAuth.ComputePreMaster(ephemeralSecret, staticSecret, transcript, _exchangeIdentifier, _clientNonce);
                _sessionId = SessionAuth.DeriveSessionId(preMaster);
                SecureChannel channel = SecureChannel.FromPreMaster(preMaster, _sessionId, isServer: true);
                _serverChannel = channel;
                if (_mode != ScriptedProofMode.Omit)
                {
                    byte[] mac = SessionAuth.ComputeIdentityProof(staticSecret, transcript);
                    if (_mode == ScriptedProofMode.WrongMac) mac[0] ^= 0xFF;
                    EnqueueSealed(channel, PacketType.IdentityProof, new IdentityProofPacket { Mac = mac });
                    IdentityProofSent = true;
                }
            }
        }
        return Task.CompletedTask;
    }

    public Task SendSealedAsync(byte[] sealedBytes, CancellationToken ct) => Task.CompletedTask;

    public Task<RawFrame> ReceiveRawAsync(CancellationToken ct)
    {
        if (_frames.TryTake(out RawFrame frame, 500, ct)) return Task.FromResult(frame);
        return Task.FromResult<RawFrame>(null);
    }

    private void Enqueue(PacketType type, object payload) =>
        _frames.Add(new RawFrame { IsSealed = false, Data = LidgrenWire.Encode(type, payload) });

    private void EnqueueSealed(SecureChannel channel, PacketType type, object payload) =>
        _frames.Add(new RawFrame { IsSealed = true, Data = channel.Seal(type, payload) });
}
