using System.Collections.Concurrent;
using System.Security.Cryptography;
using SFSEnhanced.Shared.Models;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var servers = new ConcurrentDictionary<string, RegisteredServer>();

app.MapGet("/api/v1/servers", () =>
{
    var cutoff = DateTime.UtcNow.AddSeconds(-45);
    var active = servers.Values
        .Where(x => x.Listing.LastHeartbeatUtc >= cutoff)
        .Select(x => x.Listing)
        .OrderByDescending(x => x.OnlinePlayers)
        .ThenBy(x => x.Name)
        .ToList();
    return Results.Ok(new ServerDirectoryResponse { Servers = active });
});

app.MapPost("/api/v1/servers/register", (ServerRegisterRequest request) =>
{
    if (request == null || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Host) || request.Port < 1 || request.Port > 65535)
        return Results.BadRequest();

    var id = Guid.NewGuid().ToString("N");
    var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    var listing = new ServerListing
    {
        ServerId = id,
        Name = request.Name.Trim(),
        Host = request.Host.Trim(),
        Port = request.Port,
        Region = request.Region ?? "unknown",
        GameVersion = request.GameVersion ?? "unknown",
        ModVersion = request.ModVersion ?? "unknown",
        Motd = request.Motd ?? string.Empty,
        MaxPlayers = Math.Max(1, request.MaxPlayers),
        PasswordProtected = request.PasswordProtected,
        LastHeartbeatUtc = DateTime.UtcNow
    };
    servers[id] = new RegisteredServer(listing, token);
    return Results.Ok(new ServerRegisterResponse { ServerId = id, HeartbeatToken = token });
});

app.MapPost("/api/v1/servers/heartbeat", (ServerHeartbeatRequest request) =>
{
    if (request == null || string.IsNullOrWhiteSpace(request.ServerId) || !servers.TryGetValue(request.ServerId, out var existing) || existing.Token != request.HeartbeatToken)
        return Results.Unauthorized();

    existing.Listing.OnlinePlayers = Math.Max(0, request.OnlinePlayers);
    existing.Listing.MaxPlayers = Math.Max(1, request.MaxPlayers);
    if (request.Motd != null) existing.Listing.Motd = request.Motd;
    existing.Listing.LastHeartbeatUtc = DateTime.UtcNow;
    return Results.Ok();
});

app.MapPost("/api/v1/servers/unregister", (ServerHeartbeatRequest request) =>
{
    if (request == null || string.IsNullOrWhiteSpace(request.ServerId) || !servers.TryGetValue(request.ServerId, out var existing) || existing.Token != request.HeartbeatToken)
        return Results.Unauthorized();
    servers.TryRemove(request.ServerId, out _);
    return Results.Ok();
});

app.MapGet("/", () => Results.Text("SFS Enhanced Server Directory"));

app.Run();

sealed record RegisteredServer(ServerListing Listing, string Token);
