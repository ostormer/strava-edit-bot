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
    /// Provides methods to interact with Strava's Gear API.
    /// </summary>
    /// <remarks>
    /// This static class offers functionality for retrieving information about gear associated with an athlete's Strava account.
    /// 
    /// <para><b>Features:</b></para>
    /// <list type="bullet">
    /// <item><description>Retrieve detailed information about a specific gear item (bike or shoes) by its ID.</description></item>
    /// </list>
    /// 
    /// <para><b>Usage:</b></para>
    /// <code>
    /// var gear = await Gears.GetGearByIdAsync(accessToken, "b1234567898765509876");
    /// Console.WriteLine(gear["name"]);
    /// </code>
    /// 
    /// <para><b>API Documentation:</b></para>
    /// See the official Strava API reference:
    /// <see href="https://developers.strava.com/docs/reference/">Strava API Reference</see>.
    /// </remarks>
    static public class Gears
    {
        /// <summary>
        /// Get a gear by its ID.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="gearId">The ID of the gear.</param>
        /// <returns>A <see cref="JsonObject"/> containing the gear details.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> or <paramref name="gearId"/> is null or empty.
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
        public static async Task<JsonObject> GetGearByIdAsync(string accessToken, string gearId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (string.IsNullOrWhiteSpace(gearId))
                throw new ArgumentException("Gear ID cannot be null or empty.", nameof(gearId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://www.strava.com/api/v3/gears/{gearId}");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve gear. Status Code: {response.StatusCode}, Response: {content}");

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Failed to retrieve gear. The response was empty.");

            return JsonNode.Parse(content) as JsonObject
                   ?? throw new JsonException("Failed to parse gear JSON.");
        }

    }
}
