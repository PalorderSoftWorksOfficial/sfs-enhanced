using SFSEnhanced.Auth;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient<SteamTicketVerifier>();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.IncludeFields = true);
var app = builder.Build();

app.MapPost("/api/v1/auth/steam/ticket", async (SteamTicketRequest request, SteamTicketVerifier verifier, CancellationToken cancellationToken) =>
{
    if (request == null || string.IsNullOrWhiteSpace(request.Ticket))
        return Results.BadRequest(new SteamTicketResponse { Authenticated = false, Error = "A Steam ticket is required." });

    var steamId = await verifier.VerifyAsync(request.Ticket.Trim(), cancellationToken);
    if (string.IsNullOrWhiteSpace(steamId))
        return Results.Unauthorized();

    return Results.Ok(new SteamTicketResponse { Authenticated = true, SteamId = steamId });
});

app.MapGet("/", () => Results.Text("SFS Enhanced Authentication Service"));
app.Run();

public sealed class SteamTicketRequest
{
    public string Ticket { get; set; } = string.Empty;
}

public sealed class SteamTicketResponse
{
    public bool Authenticated { get; set; }
    public string? SteamId { get; set; }
    public string? Error { get; set; }
}
