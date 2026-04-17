using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using StravaAPILibrary.Models.Athletes;

namespace StravaEditBotApi.Services.Auth;

public class StravaAuthService(HttpClient httpClient, IConfiguration configuration) : IStravaAuthService
{
    private const string TokenEndpoint = "https://www.strava.com/oauth/token";

    public async Task<StravaTokenData> ExchangeCodeAsync(string code)
    {
        string clientId = configuration["Strava:ClientId"]
            ?? throw new InvalidOperationException("Strava:ClientId is not configured.");
        string clientSecret = configuration["Strava:ClientSecret"]
            ?? throw new InvalidOperationException("Strava:ClientSecret is not configured.");

        var formData = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
        };

        var response = await httpClient.PostAsync(TokenEndpoint, new FormUrlEncodedContent(formData));
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Strava token exchange failed. Status: {response.StatusCode}, Body: {body}");
        }

        var json = JsonNode.Parse(body) as JsonObject
            ?? throw new JsonException("Failed to parse Strava token response.");

        string accessToken = json["access_token"]?.GetValue<string>()
            ?? throw new JsonException("access_token missing in Strava response.");
        string refreshToken = json["refresh_token"]?.GetValue<string>()
            ?? throw new JsonException("refresh_token missing in Strava response.");
        long expiresAtUnix = json["expires_at"]?.GetValue<long>()
            ?? throw new JsonException("expires_at missing in Strava response.");
        DateTime expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix).UtcDateTime;

        var athleteNode = json["athlete"]
            ?? throw new JsonException("athlete missing in Strava response.");
        var athlete = athleteNode.Deserialize<SummaryAthlete>()
            ?? throw new JsonException("Failed to deserialize athlete from Strava response.");

        if (athlete.Id == 0)
        {
            throw new JsonException("athlete.id missing in Strava response.");
        }

        return new StravaTokenData(
            athlete.Id,
            accessToken,
            refreshToken,
            expiresAt,
            athlete.Firstname,
            athlete.Lastname,
            athlete.ProfileMedium,
            athlete.Profile
        );
    }
}
