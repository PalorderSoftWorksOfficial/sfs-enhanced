using System.Net.Http.Json;
using System.Text.Json;

namespace SFSEnhanced.Auth;

public sealed class SteamTicketVerifier
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _appId;
    private readonly string _identity;

    public SteamTicketVerifier(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _apiKey = configuration["Steam:WebApiKey"] ?? string.Empty;
        _appId = configuration["Steam:AppId"] ?? string.Empty;
        _identity = configuration["Steam:Identity"] ?? "sfs-enhanced-auth";
    }

    public async Task<string?> VerifyAsync(string ticket, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ticket) || string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_appId))
            return null;

        var url = "https://partner.steam-api.com/ISteamUserAuth/AuthenticateUserTicket/v1/" +
                  "?key=" + Uri.EscapeDataString(_apiKey) +
                  "&appid=" + Uri.EscapeDataString(_appId) +
                  "&ticket=" + Uri.EscapeDataString(ticket) +
                  "&identity=" + Uri.EscapeDataString(_identity);
        using var response = await _http.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("response", out var result)) return null;
        if (!result.TryGetProperty("params", out var parameters)) return null;
        if (!parameters.TryGetProperty("result", out var status) || !string.Equals(status.GetString(), "OK", StringComparison.OrdinalIgnoreCase)) return null;
        if (!parameters.TryGetProperty("steamid", out var steamId)) return null;
        return steamId.GetString();
    }
}
