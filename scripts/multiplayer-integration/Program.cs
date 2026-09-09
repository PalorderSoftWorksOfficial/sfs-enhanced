using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using Newtonsoft.Json;
using SFSEnhanced.Shared.Protocol;

static async Task SendAsync(SslStream stream, PacketType type, object? payload)
{
    await NetMessage.WriteAsync(stream, type, payload);
}

static async Task<(PacketType Type, string Json)> ReceiveAsync(SslStream stream, int timeoutMs = 3000)
{
    var task = NetMessage.ReadRawAsync(stream);
    var completed = await Task.WhenAny(task, Task.Delay(timeoutMs));
    if (completed != task) throw new TimeoutException("Timed out waiting for packet.");
    return await task;
}

static async Task<T> ReceiveTypeAsync<T>(SslStream stream, PacketType expected)
{
    var packet = await ReceiveAsync(stream);
    if (packet.Type != expected) throw new InvalidOperationException($"Expected {expected}, got {packet.Type}: {packet.Json}");
    return JsonConvert.DeserializeObject<T>(packet.Json) ?? throw new InvalidOperationException($"Invalid {expected} payload.");
}

static async Task TestUnauthenticatedAccessAsync()
{
    using var client = new TcpClient();
    await client.ConnectAsync(IPAddress.Loopback, 18777);
    using var stream = new SslStream(client.GetStream(), false, (sender, certificate, chain, errors) => certificate != null);
    await stream.AuthenticateAsClientAsync("localhost", null, SslProtocols.Tls12, false);
    await SendAsync(stream, PacketType.ServerInfoRequest, new { });
    var packet = await ReceiveAsync(stream);
    if (packet.Type != PacketType.Error || !packet.Json.Contains("Authentication is required"))
        throw new InvalidOperationException("Unauthenticated access was not rejected.");
}

static async Task<(TcpClient Client, SslStream Stream, HelloAckPacket Ack)> ConnectAsync(string name)
{
    var client = new TcpClient();
    await client.ConnectAsync(IPAddress.Loopback, 18777);
    var stream = new SslStream(client.GetStream(), false, (sender, certificate, chain, errors) => certificate != null);
    await stream.AuthenticateAsClientAsync("localhost", null, SslProtocols.Tls12, false);
    await SendAsync(stream, PacketType.Hello, new HelloPacket
    {
        PlayerName = name,
        ClientModVersion = "0.1.0"
    });
    var ack = await ReceiveTypeAsync<HelloAckPacket>(stream, PacketType.HelloAck);
    if (!ack.Accepted) throw new InvalidOperationException(ack.RejectReason);
    return (client, stream, ack);
}

var aliceName = "Alice_" + Guid.NewGuid().ToString("N").Substring(0, 8);
var bobName = "Bob_" + Guid.NewGuid().ToString("N").Substring(0, 8);
var worldName = "Integration World " + Guid.NewGuid().ToString("N").Substring(0, 8);

await TestUnauthenticatedAccessAsync();

var (alice, aliceStream, aliceAck) = await ConnectAsync(aliceName);
var (bob, bobStream, bobAck) = await ConnectAsync(bobName);

await SendAsync(aliceStream, PacketType.WorldCreate, new WorldCreatePacket { Name = worldName, IsPublic = true });
var created = await ReceiveTypeAsync<WorldJoinAckPacket>(aliceStream, PacketType.WorldJoinAck);
if (!created.Accepted) throw new InvalidOperationException(created.RejectReason);
var worldId = created.WorldId;

await SendAsync(bobStream, PacketType.WorldListRequest, new { });
var worlds = await ReceiveTypeAsync<WorldListResponsePacket>(bobStream, PacketType.WorldListResponse);
if (!worlds.Worlds.Any(x => x.WorldId == worldId)) throw new InvalidOperationException("Created world was not listed.");

await SendAsync(bobStream, PacketType.WorldJoin, new WorldJoinPacket { WorldId = worldId });
var joined = await ReceiveTypeAsync<WorldJoinAckPacket>(bobStream, PacketType.WorldJoinAck);
if (!joined.Accepted) throw new InvalidOperationException(joined.RejectReason);
var joinBroadcast = await ReceiveTypeAsync<ChatMessagePacket>(bobStream, PacketType.ChatMessage);
var joinBroadcastOwner = await ReceiveTypeAsync<ChatMessagePacket>(aliceStream, PacketType.ChatMessage);
if (joinBroadcast.Message != $"{bobName} joined the world." || joinBroadcastOwner.Message != $"{bobName} joined the world.") throw new InvalidOperationException("Join broadcast mismatch.");

await SendAsync(aliceStream, PacketType.BuildSpawn, new BuildSnapshot
{
    BuildId = "build-1",
    DisplayName = "Alice Rocket",
    Kind = BuildKind.Rocket,
    PartsBlueprintJson = "{}"
});
var spawn = await ReceiveTypeAsync<BuildSnapshot>(bobStream, PacketType.BuildSpawn);
if (spawn.BuildId != "build-1") throw new InvalidOperationException("Build spawn mismatch.");

await SendAsync(aliceStream, PacketType.ClaimCreate, new ClaimCreatePacket
{
    WorldId = worldId,
    Shape = ClaimShape.Build,
    BuildId = "build-1"
});
var claim = await ReceiveTypeAsync<ClaimInfo>(bobStream, PacketType.ClaimCreate);
var claimOwner = await ReceiveTypeAsync<ClaimInfo>(aliceStream, PacketType.ClaimCreate);
if (string.IsNullOrEmpty(claim.ClaimId) || claimOwner.ClaimId != claim.ClaimId) throw new InvalidOperationException("Claim broadcast mismatch.");

await SendAsync(aliceStream, PacketType.ClaimRemove, new ClaimRemovePacket
{
    WorldId = worldId,
    ClaimId = claim.ClaimId
});
var removed = await ReceiveTypeAsync<ClaimRemovePacket>(bobStream, PacketType.ClaimRemove);
var removedOwner = await ReceiveTypeAsync<ClaimRemovePacket>(aliceStream, PacketType.ClaimRemove);
if (removed.ClaimId != claim.ClaimId || removedOwner.ClaimId != claim.ClaimId) throw new InvalidOperationException("Claim removal mismatch.");

await SendAsync(aliceStream, PacketType.FriendRequest, new FriendRequestPacket { TargetPlayerName = $"{bobName}" });
var incoming = await ReceiveTypeAsync<FriendListResponsePacket>(bobStream, PacketType.FriendListResponse);
var outgoing = await ReceiveTypeAsync<FriendListResponsePacket>(aliceStream, PacketType.FriendListResponse);
if (!incoming.PendingIncoming.Any(x => x.PlayerName == $"{aliceName}") || !outgoing.PendingOutgoing.Any(x => x.PlayerName == $"{bobName}")) throw new InvalidOperationException("Friend request was not delivered.");

await SendAsync(bobStream, PacketType.FriendRequestResponse, new FriendRequestResponsePacket
{
    FromPlayerId = aliceAck.PlayerId,
    Accepted = true
});
var bobFriends = await ReceiveTypeAsync<FriendListResponsePacket>(bobStream, PacketType.FriendListResponse);
var aliceFriends = await ReceiveTypeAsync<FriendListResponsePacket>(aliceStream, PacketType.FriendListResponse);
if (!bobFriends.Friends.Any(x => x.PlayerName == $"{aliceName}") || !aliceFriends.Friends.Any(x => x.PlayerName == $"{bobName}")) throw new InvalidOperationException("Friend acceptance was not persisted.");

await SendAsync(aliceStream, PacketType.ChatMessage, new ChatMessagePacket
{
    WorldId = worldId,
    Message = "integration chat"
});
var chat = await ReceiveTypeAsync<ChatMessagePacket>(bobStream, PacketType.ChatMessage);
var chatOwner = await ReceiveTypeAsync<ChatMessagePacket>(aliceStream, PacketType.ChatMessage);
if (chat.Message != "integration chat" || chat.FromPlayerName != $"{aliceName}" || chatOwner.Message != "integration chat") throw new InvalidOperationException("Chat payload mismatch.");

await SendAsync(bobStream, PacketType.WorldLeave, new { });
await Task.Delay(100);
await SendAsync(aliceStream, PacketType.WorldListRequest, new { });
var afterLeave = await ReceiveTypeAsync<WorldListResponsePacket>(aliceStream, PacketType.WorldListResponse);
var listed = afterLeave.Worlds.FirstOrDefault(x => x.WorldId == worldId);
if (listed == null || listed.PlayersOnline != 1) throw new InvalidOperationException("World leave did not update player count.");

await SendAsync(aliceStream, PacketType.Disconnect, new DisconnectPacket { Reason = "integration complete" });
await SendAsync(bobStream, PacketType.Disconnect, new DisconnectPacket { Reason = "integration complete" });
aliceStream.Dispose();
bobStream.Dispose();
alice.Close();
bob.Close();
Console.WriteLine("MULTI_CLIENT_INTEGRATION_PASSED");
