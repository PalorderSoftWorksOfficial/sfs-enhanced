using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
        public bool Advertise;
        public string DirectoryUrl;
        public string PublicHost;
        public string Region = "unknown";
        public string GameVersion = "1.5+";
        public string ModVersion = "0.1.0";
        public bool PasswordProtected;

        public static ServerConfig Load(string path)
        {
            var config = new ServerConfig();
            if (!File.Exists(path)) return config;

            try
            {
                var loaded = JsonConvert.DeserializeObject<ServerConfig>(File.ReadAllText(path));
                if (loaded == null) return config;
                return loaded;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not read server config '{path}': {ex.Message}", ex);
            }
        }
}

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var configPath = GetConfigPath(args);
            var config = ServerConfig.Load(configPath);
            ParseArgs(config, args);

            if (string.IsNullOrWhiteSpace(config.ServerName))
                config.ServerName = "SFS Enhanced Server";
            if (config.Port < 1 || config.Port > 65535)
                throw new ArgumentOutOfRangeException(nameof(config.Port), "Port must be between 1 and 65535.");
            if (config.MaxPlayers < 1)
                config.MaxPlayers = 1;

            Directory.CreateDirectory(config.DataDir);
            Console.WriteLine("=== SFS Enhanced Server ===");
            Console.WriteLine($"Name: {config.ServerName}");
            Console.WriteLine($"Port: {config.Port}");
            Console.WriteLine($"Data: {Path.GetFullPath(config.DataDir)}");
            Console.WriteLine($"Advertised: {config.Advertise}");

            var store = new FileStore(config.DataDir);
            var accounts = new AccountService(store);
            var worlds = new WorldManager(store);
            var friends = new FriendsService(accounts);
            var claims = new ClaimsService(worlds);
            var server = new NetServer(config, accounts, worlds, friends, claims);
            var publisher = new ServerDirectoryPublisher(config);

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            var serverTask = server.RunAsync(cts.Token);
            var directoryTask = publisher.RunAsync(server, cts.Token);
            Console.WriteLine("Server running. Commands: 'list', 'worlds', 'status', 'stop'");
            _ = Task.Run(() => ConsoleCommandLoop(server, config, cts));
            await Task.WhenAll(serverTask, directoryTask);
            Console.WriteLine("Server stopped.");
        }

        private static void ConsoleCommandLoop(NetServer server, ServerConfig config, CancellationTokenSource cts)
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
                    case "status":
                        Console.WriteLine($"  {config.ServerName} on :{config.Port}");
                        Console.WriteLine($"  players={server.ConnectedPlayerCount}/{config.MaxPlayers}");
                        Console.WriteLine($"  advertise={config.Advertise}");
                        break;
                    default:
                        Console.WriteLine("Unknown command. Try: list, worlds, status, stop");
                        break;
                }
            }
        }

        private static string GetConfigPath(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--config") return args[i + 1];
            }
            return "server.json";
        }

        private static void ParseArgs(ServerConfig config, string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--config": i++; break;
                    case "--port": config.Port = int.Parse(args[++i]); break;
                    case "--name": config.ServerName = args[++i]; break;
                    case "--motd": config.Motd = args[++i]; break;
                    case "--max-players": config.MaxPlayers = int.Parse(args[++i]); break;
                    case "--data":
                    case "--worlds": config.DataDir = args[++i]; break;
                    case "--advertise": config.Advertise = true; break;
                    case "--no-advertise": config.Advertise = false; break;
                    case "--directory": config.DirectoryUrl = args[++i]; break;
                    case "--public-host": config.PublicHost = args[++i]; break;
                    case "--region": config.Region = args[++i]; break;
                    case "--game-version": config.GameVersion = args[++i]; break;
                    case "--mod-version": config.ModVersion = args[++i]; break;
                    case "--password-protected": config.PasswordProtected = true; break;
                }
            }
        }
    }
}
