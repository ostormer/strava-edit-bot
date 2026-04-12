using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;


namespace StravaAPILibary.API
{
    /// <summary>
    /// Provides methods to interact with Strava's Athletes API.
    /// </summary>
    /// <remarks>
    /// This static class contains methods for retrieving and updating information about the authenticated athlete 
    /// and other related athlete-specific data. It communicates with Strava's REST API and returns JSON responses.
    ///
    /// <para><b>Features:</b></para>
    /// <list type="bullet">
    /// <item><description>Retrieve the authenticated athlete's profile details.</description></item>
    /// <item><description>Fetch athlete statistics (e.g., totals, ride/run stats).</description></item>
    /// <item><description>Retrieve heart rate and power zones.</description></item>
    /// <item><description>Update athlete information such as weight (requires <c>profile:write</c> scope).</description></item>
    /// </list>
    ///
    /// <para><b>Usage:</b></para>
    /// All methods require a valid <c>accessToken</c> (OAuth token) with the necessary scopes granted.
    /// 
    /// <para><b>API Documentation:</b></para>
    /// See the official Strava API reference:
    /// <see href="https://developers.strava.com/docs/reference/">Strava API Reference</see>.
    /// </remarks>
    /// <example>
    /// Example: Retrieve the authenticated athlete's profile:
    /// <code>
    /// var profile = await Athletes.GetAuthenticatedAthleteProfileAsync(accessToken);
    /// </code>
    ///
    /// Example: Update athlete weight:
    /// <code>
    /// var updatedProfile = await Athletes.UpdateAuthenticatedAthleteAsync(accessToken, 72.5f);
    /// </code>
    /// </example>
    static public class Athletes
    {
        /// <summary>
        /// Get the authenticated Athlete's profile.
        /// </summary>
        /// <param name="accessToken">The access token for authentication.</param>
        /// <returns>A <see cref="JsonObject"/> containing the athlete's profile information.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when the request to the Strava API fails or returns a non-success status code.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the API response is empty.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the JSON response cannot be parsed as a <see cref="JsonObject"/>.
        /// </exception>
        public static async Task<JsonObject> GetAuthenticatedAthleteProfileAsync(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync("https://www.strava.com/api/v3/athlete");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve athlete profile. Status: {response.StatusCode}, Response: {content}");

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Failed to retrieve athlete profile. The response was empty.");

            return JsonNode.Parse(content) as JsonObject
                   ?? throw new JsonException("Failed to parse athlete profile JSON.");
        }


        /// <summary>
        /// Get Athlete's stats for the authenticated athlete.
        /// This method retrieves the statistics of a specific athlete using their athlete ID.
        /// </summary>
        /// <param name="accessToken">The access token for authentication.</param>
        /// <param name="athleteId">The ID of the athlete whose stats are to be retrieved.</param>
        /// <returns>A <see cref="JsonObject"/> containing the athlete's statistics.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> or <paramref name="athleteId"/> is null or empty.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when the request to the Strava API fails or returns a non-success status code.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the API response is empty.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the JSON response cannot be parsed as a <see cref="JsonObject"/>.
        /// </exception>
        public static async Task<JsonObject> GetAuthenticatedAthleteStatsAsync(string accessToken, string athleteId)
        {
            if (string.IsNullOrWhiteSpace(accessToken)) {
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            }
            if (string.IsNullOrWhiteSpace(athleteId))
            {
                throw new ArgumentException("Athlete ID cannot be null or empty.", nameof(athleteId));
            }

            string url = $"https://www.strava.com/api/v3/athletes/{athleteId}/stats";
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to retrieve athlete stats. Status Code: {response.StatusCode}");
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                throw new InvalidOperationException("Failed to retrieve athlete stats. The response was empty.");
            }
            return JsonNode.Parse(jsonResponse) as JsonObject ?? throw new JsonException("Failed to parse athlete stats JSON");
        }

        /// <summary>
        /// Get Athlete's heart rate and power zones for the authenticated athlete.
        /// </summary>
        /// <param name="accessToken">The access token for authentication.</param>
        /// <returns>A <see cref="JsonObject"/> containing the athlete's heart rate and power zones.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when the request to the Strava API fails or returns a non-success status code.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the API response is empty.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the JSON response cannot be parsed as a <see cref="JsonObject"/>.
        /// </exception>
        public static async Task<JsonObject> GetAuthenticatedAthleteZonesAsync(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            }

            string url = "https://www.strava.com/api/v3/athlete/zones";
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to retrieve athlete zones. Status Code: {response.StatusCode}");
            }
            string jsonResponse = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                throw new InvalidOperationException("Failed to retrieve athlete zones. The response was empty.");
            }

            return JsonNode.Parse(jsonResponse) as JsonObject ?? throw new JsonException("Failed to parse athlete zones JSON.");
        }

        /// <summary>
        /// Updates the authenticated athlete's weight.
        /// Requires scope: profile:write.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="weight">The new weight of the athlete in kilograms.</param>
        /// <returns>A <see cref="JsonObject"/> containing the updated athlete profile.</returns>
        /// <exception cref="ArgumentException">Thrown when the access token is null or empty.</exception>
        /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the response is empty.</exception>
        /// <exception cref="JsonException">Thrown when the response cannot be parsed as JSON.</exception>
        public static async Task<JsonObject> UpdateAuthenticatedAthleteAsync(string accessToken, float weight)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            const string url = "https://www.strava.com/api/v3/athlete";
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var payload = new JsonObject { ["weight"] = weight };
            var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");

            var response = await client.PutAsync(url, content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to update athlete profile. Status Code: {response.StatusCode}, Response: {jsonResponse}");

            if (string.IsNullOrWhiteSpace(jsonResponse))
                throw new InvalidOperationException("Failed to update athlete profile. The response was empty.");

            return JsonNode.Parse(jsonResponse) as JsonObject
                   ?? throw new JsonException("Failed to parse updated athlete profile JSON.");
        }

    }
}
