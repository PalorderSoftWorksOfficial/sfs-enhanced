using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.TestClient
{
    /// <summary>
    /// A plain console client that speaks the real wire protocol. Not part of
    /// the mod — this exists so you can sanity-check the whole Server/Shared
    /// stack (connect, create a world, spawn a fake build, add a friend...)
    /// without needing the game or a compiled mod DLL at all.
    ///
    /// Usage:
    ///   dotnet run --project TestClient -- 127.0.0.1 7777 Alice
    /// Then type commands: world, spawn, friend <name>, chat <msg>, quit
    /// </summary>
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            string host = args.Length > 0 ? args[0] : "127.0.0.1";
            int port = args.Length > 1 ? int.Parse(args[1]) : 7777;
            string name = args.Length > 2 ? args[2] : "TestPlayer";

            using var tcp = new TcpClient();
            await tcp.ConnectAsync(host, port);
            var stream = tcp.GetStream();
            Console.WriteLine($"Connected to {host}:{port} as {name}");

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var (type, json) = await NetMessage.ReadRawAsync(stream);
                    if (json == null) { Console.WriteLine("[disconnected]"); return; }
                    Console.WriteLine($"<< {type}: {json}");
                }
            });

            await NetMessage.WriteAsync(stream, PacketType.Hello, new HelloPacket { PlayerName = name });

            string currentWorldId = null;

            while (true)
            {
                string line = Console.ReadLine();
                if (line == null) continue;
                var parts = line.Split(' ', 2);

                switch (parts[0])
                {
                    case "quit":
                        return;

                    case "world":
                        await NetMessage.WriteAsync(stream, PacketType.WorldCreate, new WorldCreatePacket
                        {
                            Name = parts.Length > 1 ? parts[1] : $"{name}'s World",
                            IsPublic = true,
                        });
                        break;

                    case "join":
                        currentWorldId = parts.Length > 1 ? parts[1] : currentWorldId;
                        await NetMessage.WriteAsync(stream, PacketType.WorldJoin, new WorldJoinPacket { WorldId = currentWorldId });
                        break;

                    case "spawn":
                        await NetMessage.WriteAsync(stream, PacketType.BuildSpawn, new BuildSnapshot
                        {
                            BuildId = Guid.NewGuid().ToString("N"),
                            DisplayName = "Test Rocket",
                            Kind = BuildKind.Rocket,
                            PosX = 0, PosY = 0,
                            PartsBlueprintJson = "{}",
                        });
                        break;

                    case "friend":
                        await NetMessage.WriteAsync(stream, PacketType.FriendRequest, new FriendRequestPacket
                        {
                            TargetPlayerName = parts.Length > 1 ? parts[1] : "",
                        });
                        break;

                    case "friends":
                        await NetMessage.WriteAsync(stream, PacketType.FriendListRequest, null);
                        break;

                    case "chat":
                        await NetMessage.WriteAsync(stream, PacketType.ChatMessage, new ChatMessagePacket
                        {
                            WorldId = currentWorldId,
                            Message = parts.Length > 1 ? parts[1] : "",
                        });
                        break;

                    default:
                        Console.WriteLine("Commands: world [name] | join [worldId] | spawn | friend <name> | friends | chat <msg> | quit");
                        break;
                }
            }
        }
    }
}
