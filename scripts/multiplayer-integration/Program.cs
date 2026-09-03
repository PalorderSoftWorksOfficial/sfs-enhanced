using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using SFSEnhanced.Shared.Protocol;

static async Task SendAsync(TcpClient client, PacketType type, object? payload)
{
    await NetMessage.WriteAsync(client.GetStream(), type, payload);
}

static async Task<(PacketType Type, string Json)> ReceiveAsync(TcpClient client, int timeoutMs = 3000)
{
    var task = NetMessage.ReadRawAsync(client.GetStream());
    var completed = await Task.WhenAny(task, Task.Delay(timeoutMs));
    if (completed != task) throw new TimeoutException("Timed out waiting for packet.");
    return await task;
}

static async Task<T> ReceiveTypeAsync<T>(TcpClient client, PacketType expected)
{
    var packet = await ReceiveAsync(client);
    if (packet.Type != expected) throw new InvalidOperationException($"Expected {expected}, got {packet.Type}: {packet.Json}");
    return JsonConvert.DeserializeObject<T>(packet.Json) ?? throw new InvalidOperationException($"Invalid {expected} payload.");
}

static async Task<(TcpClient Client, HelloAckPacket Ack)> ConnectAsync(string name)
{
    var client = new TcpClient();
    await client.ConnectAsync(IPAddress.Loopback, 18777);
    await SendAsync(client, PacketType.Hello, new HelloPacket
    {
        PlayerName = name,
        ClientModVersion = "0.1.0"
    });
    var ack = await ReceiveTypeAsync<HelloAckPacket>(client, PacketType.HelloAck);
    if (!ack.Accepted) throw new InvalidOperationException(ack.RejectReason);
    return (client, ack);
}

var (alice, aliceAck) = await ConnectAsync("Alice");
var (bob, bobAck) = await ConnectAsync("Bob");

await SendAsync(alice, PacketType.WorldCreate, new WorldCreatePacket { Name = "Integration World", IsPublic = true });
var created = await ReceiveTypeAsync<WorldJoinAckPacket>(alice, PacketType.WorldJoinAck);
if (!created.Accepted) throw new InvalidOperationException(created.RejectReason);
var worldId = created.WorldId;

await SendAsync(bob, PacketType.WorldListRequest, new { });
var worlds = await ReceiveTypeAsync<WorldListResponsePacket>(bob, PacketType.WorldListResponse);
if (!worlds.Worlds.Any(x => x.WorldId == worldId)) throw new InvalidOperationException("Created world was not listed.");

await SendAsync(bob, PacketType.WorldJoin, new WorldJoinPacket { WorldId = worldId });
var joined = await ReceiveTypeAsync<WorldJoinAckPacket>(bob, PacketType.WorldJoinAck);
if (!joined.Accepted) throw new InvalidOperationException(joined.RejectReason);
var joinBroadcast = await ReceiveTypeAsync<ChatMessagePacket>(bob, PacketType.ChatMessage);
var joinBroadcastOwner = await ReceiveTypeAsync<ChatMessagePacket>(alice, PacketType.ChatMessage);
if (joinBroadcast.Message != "Bob joined the world." || joinBroadcastOwner.Message != "Bob joined the world.") throw new InvalidOperationException("Join broadcast mismatch.");

await SendAsync(alice, PacketType.BuildSpawn, new BuildSnapshot
{
    BuildId = "build-1",
    DisplayName = "Alice Rocket",
    Kind = BuildKind.Rocket,
    PartsBlueprintJson = "{}"
});
var spawn = await ReceiveTypeAsync<BuildSnapshot>(bob, PacketType.BuildSpawn);
if (spawn.BuildId != "build-1") throw new InvalidOperationException("Build spawn mismatch.");

await SendAsync(alice, PacketType.ClaimCreate, new ClaimCreatePacket
{
    WorldId = worldId,
    Shape = ClaimShape.Build,
    BuildId = "build-1"
});
var claim = await ReceiveTypeAsync<ClaimInfo>(bob, PacketType.ClaimCreate);
var claimOwner = await ReceiveTypeAsync<ClaimInfo>(alice, PacketType.ClaimCreate);
if (string.IsNullOrEmpty(claim.ClaimId) || claimOwner.ClaimId != claim.ClaimId) throw new InvalidOperationException("Claim broadcast mismatch.");

await SendAsync(alice, PacketType.ClaimRemove, new ClaimRemovePacket
{
    WorldId = worldId,
    ClaimId = claim.ClaimId
});
var removed = await ReceiveTypeAsync<ClaimRemovePacket>(bob, PacketType.ClaimRemove);
var removedOwner = await ReceiveTypeAsync<ClaimRemovePacket>(alice, PacketType.ClaimRemove);
if (removed.ClaimId != claim.ClaimId || removedOwner.ClaimId != claim.ClaimId) throw new InvalidOperationException("Claim removal mismatch.");

await SendAsync(alice, PacketType.FriendRequest, new FriendRequestPacket { TargetPlayerName = "Bob" });
var incoming = await ReceiveTypeAsync<FriendListResponsePacket>(bob, PacketType.FriendListResponse);
var outgoing = await ReceiveTypeAsync<FriendListResponsePacket>(alice, PacketType.FriendListResponse);
if (!incoming.PendingIncoming.Any(x => x.PlayerName == "Alice") || !outgoing.PendingOutgoing.Any(x => x.PlayerName == "Bob")) throw new InvalidOperationException("Friend request was not delivered.");

await SendAsync(bob, PacketType.FriendRequestResponse, new FriendRequestResponsePacket
{
    FromPlayerId = aliceAck.PlayerId,
    Accepted = true
});
var bobFriends = await ReceiveTypeAsync<FriendListResponsePacket>(bob, PacketType.FriendListResponse);
var aliceFriends = await ReceiveTypeAsync<FriendListResponsePacket>(alice, PacketType.FriendListResponse);
if (!bobFriends.Friends.Any(x => x.PlayerName == "Alice") || !aliceFriends.Friends.Any(x => x.PlayerName == "Bob")) throw new InvalidOperationException("Friend acceptance was not persisted.");

await SendAsync(alice, PacketType.ChatMessage, new ChatMessagePacket
{
    WorldId = worldId,
    Message = "integration chat"
});
var chat = await ReceiveTypeAsync<ChatMessagePacket>(bob, PacketType.ChatMessage);
var chatOwner = await ReceiveTypeAsync<ChatMessagePacket>(alice, PacketType.ChatMessage);
if (chat.Message != "integration chat" || chat.FromPlayerName != "Alice" || chatOwner.Message != "integration chat") throw new InvalidOperationException("Chat payload mismatch.");

await SendAsync(bob, PacketType.WorldLeave, new { });
await Task.Delay(100);
await SendAsync(alice, PacketType.WorldListRequest, new { });
var afterLeave = await ReceiveTypeAsync<WorldListResponsePacket>(alice, PacketType.WorldListResponse);
var listed = afterLeave.Worlds.FirstOrDefault(x => x.WorldId == worldId);
if (listed == null || listed.PlayersOnline != 1) throw new InvalidOperationException("World leave did not update player count.");

await SendAsync(alice, PacketType.Disconnect, new DisconnectPacket { Reason = "integration complete" });
await SendAsync(bob, PacketType.Disconnect, new DisconnectPacket { Reason = "integration complete" });
alice.Close();
bob.Close();
Console.WriteLine("MULTI_CLIENT_INTEGRATION_PASSED");
