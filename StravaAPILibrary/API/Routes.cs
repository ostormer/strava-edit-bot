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
    /// Provides methods to interact with Strava's Routes API.
    /// </summary>
    /// <remarks>
    /// This static class contains methods for retrieving and exporting route data associated with Strava athletes.
    /// 
    /// <para><b>Features:</b></para>
    /// <list type="bullet">
    /// <item><description>Retrieve detailed information about a specific route by its ID.</description></item>
    /// <item><description>Retrieve a list of routes associated with a specific athlete.</description></item>
    /// <item><description>Export a route in GPX or TCX format for external usage.</description></item>
    /// </list>
    /// 
    /// <para><b>Usage:</b></para>
    /// <code>
    /// var route = await Routes.GetRouteByIdAsync(accessToken, 123456);
    /// Console.WriteLine(route["name"]);
    ///
    /// var gpxData = await Routes.GetRouteGpxExportAsync(accessToken, 123456);
    /// File.WriteAllText("route.gpx", gpxData);
    /// </code>
    /// 
    /// <para><b>API Documentation:</b></para>
    /// See the official Strava API reference:
    /// <see href="https://developers.strava.com/docs/reference/#api-Routes">Strava Routes API Reference</see>.
    /// </remarks>
    static public class Routes
    {
        /// <summary>
        /// Get a route by its ID.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="routeId">The ID of the route to retrieve.</param>
        /// <returns>A <see cref="JsonObject"/> containing the route data.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty, or when <paramref name="routeId"/> is invalid.
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
        public static async Task<JsonObject> GetRouteByIdAsync(string accessToken, long routeId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (routeId <= 0)
                throw new ArgumentException("Route ID must be greater than zero.", nameof(routeId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://www.strava.com/api/v3/routes/{routeId}");

            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve route. Status Code: {response.StatusCode}, Response: {content}");

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Failed to retrieve route. The response was empty.");

            return JsonNode.Parse(content) as JsonObject
                   ?? throw new JsonException("Failed to parse route JSON.");
        }


        /// <summary>
        /// Get a list of routes from a specific athlete.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="athleteId">The ID of the athlete.</param>
        /// <param name="page">Page number for pagination (default: 1).</param>
        /// <param name="perPage">Number of items per page (default: 30).</param>
        /// <returns>A <see cref="JsonArray"/> containing the athlete's routes.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty or <paramref name="athleteId"/> is invalid.
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
        public static async Task<JsonArray> GetRoutesByAthleteIdAsync(string accessToken, long athleteId, int page = 1, int perPage = 30)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (athleteId <= 0)
                throw new ArgumentException("Athlete ID must be greater than zero.", nameof(athleteId));

            if (page <= 0)
                throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");

            if (perPage <= 0)
                throw new ArgumentOutOfRangeException(nameof(perPage), "PerPage must be greater than zero.");

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://www.strava.com/api/v3/athletes/{athleteId}/routes?page={page}&per_page={perPage}");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve routes. Status Code: {response.StatusCode}, Response: {content}");

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Failed to retrieve routes. The response was empty.");

            return JsonNode.Parse(content) as JsonArray
                   ?? throw new JsonException("Failed to parse routes JSON.");
        }


        /// <summary>
        /// Get a GPX export of a route by its ID.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="routeId">The ID of the route to export.</param>
        /// <returns>A string containing the GPX data of the route.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty, or when <paramref name="routeId"/> is invalid.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when the request to the Strava API fails or returns a non-success status code.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the GPX response is empty.
        /// </exception>
        public static async Task<string> GetRouteGpxExportAsync(string accessToken, long routeId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (routeId <= 0)
                throw new ArgumentException("Route ID must be greater than zero.", nameof(routeId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://www.strava.com/api/v3/routes/{routeId}/export_gpx");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve GPX export. Status Code: {response.StatusCode}, Response: {content}");

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Failed to retrieve GPX export. The response was empty.");

            return content;
        }


        /// <summary>
        /// Get a TCX export of a route by its ID.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="routeId">The ID of the route to export.</param>
        /// <returns>A string containing the TCX data of the route.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty, or when <paramref name="routeId"/> is invalid.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when the request to the Strava API fails or returns a non-success status code.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the TCX response is empty.
        /// </exception>
        public static async Task<string> GetRouteTcxExportAsync(string accessToken, long routeId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (routeId <= 0)
                throw new ArgumentException("Route ID must be greater than zero.", nameof(routeId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://www.strava.com/api/v3/routes/{routeId}/export_tcx");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve TCX export. Status Code: {response.StatusCode}, Response: {content}");

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Failed to retrieve TCX export. The response was empty.");

            return content;
        }


    }
}
