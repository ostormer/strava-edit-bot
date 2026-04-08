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
    /// Provides methods to interact with Strava's Clubs API.
    /// </summary>
    /// <remarks>
    /// This static class offers functionality for retrieving information related to Strava clubs, including club details, members, activities, and admins.
    /// All methods require a valid OAuth <c>accessToken</c> with the necessary scopes.
    /// 
    /// <para><b>Features:</b></para>
    /// <list type="bullet">
    /// <item><description>Retrieve detailed information about a specific club by its ID.</description></item>
    /// <item><description>Fetch the list of clubs the authenticated athlete is a member of.</description></item>
    /// <item><description>Retrieve members, activities, and admins of a specific club.</description></item>
    /// </list>
    /// 
    /// <para><b>Usage:</b></para>
    /// <code>
    /// var club = await Clubs.GetClubByIdAsync(accessToken, 12345);
    /// var members = await Clubs.GetClubMembersAsync(accessToken, 12345);
    /// var activities = await Clubs.GetClubActivitiesAsync(accessToken, 12345);
    /// var admins = await Clubs.GetClubAdminsAsync(accessToken, 12345);
    /// </code>
    /// 
    /// <para><b>API Documentation:</b></para>
    /// See the official Strava API reference:
    /// <see href="https://developers.strava.com/docs/reference/">Strava API Reference</see>.
    /// </remarks>
    static public class Clubs
    {
        /// <summary>
        /// Get a club by its ID.
        /// </summary>
        /// <param name="accessToken">The access token for the API.</param>
        /// <param name="clubId">The ID of the club to retrieve.</param>
        /// <returns>A <see cref="JsonObject"/> containing the club data.</returns>
        /// <exception cref="ArgumentException">Thrown when the access token or club ID is null or empty.</exception>
        /// <exception cref="HttpRequestException">Thrown when the request to the API fails.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the response is empty.</exception>
        /// <exception cref="JsonException">Thrown when the JSON response cannot be parsed.</exception>
        public static async Task<JsonObject> GetClubByIdAsync(string accessToken, long clubId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (clubId <= 0)
                throw new ArgumentException("Club ID must be greater than zero.", nameof(clubId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://www.strava.com/api/v3/clubs/{clubId}");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"Failed to retrieve club. Status Code: {response.StatusCode}, Response: {content}");

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Failed to retrieve club. The response was empty.");

            return JsonNode.Parse(content) as JsonObject
                   ?? throw new JsonException("Failed to parse club JSON.");
        }


        /// <summary>
        /// Get clubs for the authenticated athlete.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="page">Page number for pagination (default: 1).</param>
        /// <param name="perPage">Number of items per page (default: 30).</param>
        /// <returns>A <see cref="JsonArray"/> containing the clubs of the authenticated athlete.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="page"/> or <paramref name="perPage"/> is less than or equal to zero.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when the request to the Strava API fails or returns a non-success status code.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the API response is empty.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the JSON response cannot be parsed as a <see cref="JsonArray"/>.
        /// </exception>
        public static async Task<JsonArray> GetClubsAsync(string accessToken, int page = 1, int perPage = 30)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (page <= 0)
                throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");

            if (perPage <= 0)
                throw new ArgumentOutOfRangeException(nameof(perPage), "PerPage must be greater than zero.");

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://www.strava.com/api/v3/athlete/clubs?page={page}&per_page={perPage}");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve clubs. Status Code: {response.StatusCode}, Response: {content}");

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Failed to retrieve clubs. The response was empty.");

            return JsonNode.Parse(content) as JsonArray
                   ?? throw new JsonException("Failed to parse clubs JSON.");
        }

        /// <summary>
        /// Get the members of a club by its ID.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="clubId">The ID of the club.</param>
        /// <param name="page">Page number for pagination (default: 1).</param>
        /// <param name="perPage">Number of items per page (default: 30).</param>
        /// <returns>A <see cref="JsonArray"/> containing the members of the club.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty or when <paramref name="clubId"/> is invalid.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="page"/> or <paramref name="perPage"/> is less than or equal to zero.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when the request to the Strava API fails or returns a non-success status code.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the API response is empty.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the JSON response cannot be parsed as a <see cref="JsonArray"/>.
        /// </exception>
        public static async Task<JsonArray> GetClubMembersAsync(string accessToken, long clubId, int page = 1, int perPage = 30)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (clubId <= 0)
                throw new ArgumentException("Club ID must be greater than zero.", nameof(clubId));

            if (page <= 0)
                throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");

            if (perPage <= 0)
                throw new ArgumentOutOfRangeException(nameof(perPage), "PerPage must be greater than zero.");

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://www.strava.com/api/v3/clubs/{clubId}/members?page={page}&per_page={perPage}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve club members. Status Code: {response.StatusCode}, Response: {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Failed to retrieve club members. The response was empty.");

            return JsonNode.Parse(content) as JsonArray
                   ?? throw new JsonException("Failed to parse club members JSON.");
        }


        /// <summary>
        /// Get the activities of a club by its ID.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="clubId">The ID of the club.</param>
        /// <param name="page">Page number for pagination (default: 1).</param>
        /// <param name="perPage">Number of items per page (default: 30).</param>
        /// <returns>A <see cref="JsonArray"/> containing the activities of the club.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty or when <paramref name="clubId"/> is invalid.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="page"/> or <paramref name="perPage"/> is less than or equal to zero.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when the request to the Strava API fails or returns a non-success status code.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the API response is empty.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the JSON response cannot be parsed as a <see cref="JsonArray"/>.
        /// </exception>
        public static async Task<JsonArray> GetClubActivitiesAsync(string accessToken, long clubId, int page = 1, int perPage = 30)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (clubId <= 0)
                throw new ArgumentException("Club ID must be greater than zero.", nameof(clubId));

            if (page <= 0)
                throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");

            if (perPage <= 0)
                throw new ArgumentOutOfRangeException(nameof(perPage), "PerPage must be greater than zero.");

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://www.strava.com/api/v3/clubs/{clubId}/activities?page={page}&per_page={perPage}");

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve club activities. Status Code: {response.StatusCode}, Response: {content}");

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Failed to retrieve club activities. The response was empty.");

            return JsonNode.Parse(content) as JsonArray
                   ?? throw new JsonException("Failed to parse club activities JSON.");
        }


        /// <summary>
        /// Get the admins of a club by its ID.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="clubId">The ID of the club.</param>
        /// <param name="page">Page number for pagination (default: 1).</param>
        /// <param name="perPage">Number of items per page (default: 30).</param>
        /// <returns>A <see cref="JsonArray"/> containing the admins of the club.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty or when <paramref name="clubId"/> is invalid.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="page"/> or <paramref name="perPage"/> is less than or equal to zero.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when the request to the Strava API fails or returns a non-success status code.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the API response is empty.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the JSON response cannot be parsed as a <see cref="JsonArray"/>.
        /// </exception>
        public static async Task<JsonArray> GetClubAdminsAsync(string accessToken, long clubId, int page = 1, int perPage = 30)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (clubId <= 0)
                throw new ArgumentException("Club ID must be greater than zero.", nameof(clubId));

            if (page <= 0)
                throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");

            if (perPage <= 0)
                throw new ArgumentOutOfRangeException(nameof(perPage), "PerPage must be greater than zero.");

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://www.strava.com/api/v3/clubs/{clubId}/admins?page={page}&per_page={perPage}");

            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve club admins. Status Code: {response.StatusCode}, Response: {content}");

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Failed to retrieve club admins. The response was empty.");

            return JsonNode.Parse(content) as JsonArray
                   ?? throw new JsonException("Failed to parse club admins JSON.");
        }

    }
}
