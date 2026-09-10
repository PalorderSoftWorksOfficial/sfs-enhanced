using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using Newtonsoft.Json;
using SFSEnhanced.Shared.Protocol;

sealed class PeerTransport : ISecureTransport
{
    private readonly NetPeer _peer;
    private readonly NetConnection _connection;

    public PeerTransport(NetPeer peer, NetConnection connection)
    {
        _peer = peer;
        _connection = connection;
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
        var buffer = new System.Collections.Concurrent.BlockingCollection<RawFrame>();
        Task pump = Task.Run(() =>
        {
            while (!ct.IsCancellationRequested && !buffer.IsAddingCompleted)
            {
                NetIncomingMessage message = _peer.ReadMessage();
                if (message == null)
                {
                    Thread.Sleep(2);
                    continue;
                }
                try
                {
                    if (message.MessageType == NetIncomingMessageType.Data)
                    {
                        RawFrame frame = SecureFrame.Unwrap(message.ReadBytes(message.LengthBytes));
                        if (frame != null && !buffer.TryAdd(frame)) break;
                    }
                    else if (message.MessageType == NetIncomingMessageType.StatusChanged)
                    {
                        if ((NetConnectionStatus)message.ReadByte() == NetConnectionStatus.Disconnected) break;
                    }
                }
                finally { _peer.Recycle(message); }
            }
        }, ct);
        try
        {
            RawFrame frame = buffer.Take(ct);
            return Task.FromResult(frame);
        }
        catch (OperationCanceledException)
        {
            return Task.FromResult<RawFrame>(null);
        }
        finally
        {
            buffer.CompleteAdding();
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

    private TestPeer(NetPeer peer, NetConnection connection)
    {
        _peer = peer;
        _connection = connection;
        _ = PumpAsync();
    }

    public static async Task<TestPeer> ConnectAsync(string name, string authToken = null)
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
        NetConnection connection = peer.Connect("127.0.0.1", 18777);
        var connectedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = WatchStatusAsync(peer, connectedTcs);
        await Task.WhenAny(connectedTcs.Task, Task.Delay(8000));
        if (connection.Status != NetConnectionStatus.Connected) throw new InvalidOperationException("Transport connection failed.");

        var handshake = new SecureHandshakeClient(new PeerTransport(peer, connection));
        SecureHandshakeResult result = await handshake.RunAsync(name, authToken ?? "", "0.1.0", null, CancellationToken.None);
        if (!result.Accepted) throw new InvalidOperationException("Handshake rejected: " + (result.RejectReason ?? "unknown"));
        var session = new TestPeer(peer, connection) { PlayerId = result.PlayerId, IssuedToken = result.IssuedToken };
        session._channel = result.Channel;
        return session;
    }

    public static async Task ExpectHandshakeRejectionAsync(string name, string authToken, string expectedFragment)
    {
        var config = new NetPeerConfiguration(AppIdentifier);
        config.EnableMessageType(NetIncomingMessageType.Data);
        var peer = new NetPeer(config);
        peer.Start();
        NetConnection connection = peer.Connect("127.0.0.1", 18777);
        var connectedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = WatchStatusAsync(peer, connectedTcs);
        await Task.WhenAny(connectedTcs.Task, Task.Delay(8000));
        if (connection.Status != NetConnectionStatus.Connected) throw new InvalidOperationException("Transport connection failed.");
        var handshake = new SecureHandshakeClient(new PeerTransport(peer, connection));
        SecureHandshakeResult result = await handshake.RunAsync(name, authToken, "0.1.0", null, CancellationToken.None);
        peer.Shutdown("test done");
        if (result.Accepted) throw new InvalidOperationException("Expected the handshake to be rejected.");
        if (expectedFragment != null && (result.RejectReason ?? string.Empty).IndexOf(expectedFragment, StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException($"Expected rejection containing '{expectedFragment}' but got '{result.RejectReason}'.");
    }

    public string PlayerId { get; private set; }
    public string IssuedToken { get; private set; }

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

    public async Task<(PacketType Type, string Json)> ReceiveAsync(int timeoutMs = 4000)
    {
        Task<RawFrame> take = Task.Run(() => _frames.Take(_cts.Token));
        if (await Task.WhenAny(take, Task.Delay(timeoutMs)) != take) throw new TimeoutException("Timed out waiting for packet.");
        RawFrame frame = await take;
        if (!frame.IsSealed) throw new InvalidOperationException("Got a plain frame on an authenticated session.");
        if (_channel.TryOpen(frame.Data, out PacketType type, out string json, out string error, out bool replayed))
            return (type, json);
        if (replayed) return await ReceiveAsync(timeoutMs);
        throw new InvalidOperationException("Sealed packet rejected: " + error);
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

    public async Task DrainUntilQuietAsync()
    {
        while (true)
        {
            Task<RawFrame> take = Task.Run(() => _frames.Take(_cts.Token));
            if (await Task.WhenAny(take, Task.Delay(400)) == take) continue;
            return;
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

    private static async Task WatchStatusAsync(NetPeer peer, TaskCompletionSource<bool> connected)
    {
        while (!connected.Task.IsCompleted)
        {
            NetIncomingMessage message = peer.ReadMessage();
            if (message == null)
            {
                await Task.Delay(5);
                continue;
            }
            try
            {
                if (message.MessageType == NetIncomingMessageType.StatusChanged)
                {
                    var status = (NetConnectionStatus)message.ReadByte();
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
                    await Task.Delay(1, _cts.Token);
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

        TestPeer alice = await TestPeer.ConnectAsync(aliceName);
        TestPeer bob = await TestPeer.ConnectAsync(bobName);
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

        alice.Dispose();
        bob.Dispose();
        Console.WriteLine("MULTI_CLIENT_INTEGRATION_PASSED");
    }
}
