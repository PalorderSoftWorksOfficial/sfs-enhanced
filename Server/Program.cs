using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SFSEnhanced.Server.Networking;
using SFSEnhanced.Server.Persistence;
using SFSEnhanced.Server.World;
using SFSEnhanced.Server.Social;

namespace SFSEnhanced.Server
{
    public class ServerConfig
    {
        public int Port = 7777;
        public string ServerName = "SFS Enhanced Server";
        public string Motd = "Welcome! Build something cool.";
        public int MaxPlayers = 32;
        public string DataDir = "./data";
    }

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var config = ParseArgs(args);
            Directory.CreateDirectory(config.DataDir);

            Console.WriteLine("=== SFS Enhanced Server ===");
            Console.WriteLine($"Name: {config.ServerName}");
            Console.WriteLine($"Port: {config.Port}");
            Console.WriteLine($"Data: {Path.GetFullPath(config.DataDir)}");
            Console.WriteLine();

            var store = new FileStore(config.DataDir);
            var accounts = new AccountService(store);
            var worlds = new WorldManager(store);
            var friends = new FriendsService(accounts);
            var claims = new ClaimsService(worlds);

            var server = new NetServer(config, accounts, worlds, friends, claims);

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            var serverTask = server.RunAsync(cts.Token);

            Console.WriteLine("Server running. Commands: 'list', 'worlds', 'stop'");
            _ = Task.Run(() => ConsoleCommandLoop(server, cts));

            await serverTask;
            Console.WriteLine("Server stopped.");
        }

        private static void ConsoleCommandLoop(NetServer server, CancellationTokenSource cts)
        {
            while (!cts.IsCancellationRequested)
            {
                string line = Console.ReadLine();
                if (line == null) continue;
                switch (line.Trim().ToLowerInvariant())
                {
                    case "stop":
                        cts.Cancel();
                        break;
                    case "list":
                        server.PrintConnectedPlayers();
                        break;
                    case "worlds":
                        server.PrintWorlds();
                        break;
                    default:
                        Console.WriteLine("Unknown command. Try: list, worlds, stop");
                        break;
                }
            }
        }

        private static ServerConfig ParseArgs(string[] args)
        {
            var config = new ServerConfig();
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--port":
                        config.Port = int.Parse(args[++i]);
                        break;
                    case "--name":
                        config.ServerName = args[++i];
                        break;
                    case "--motd":
                        config.Motd = args[++i];
                        break;
                    case "--max-players":
                        config.MaxPlayers = int.Parse(args[++i]);
                        break;
                    case "--worlds":
                    case "--data":
                        config.DataDir = args[++i];
                        break;
                }
            }
            return config;
        }
    }
}
