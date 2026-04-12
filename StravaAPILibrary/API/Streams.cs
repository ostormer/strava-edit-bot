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
    /// Provides methods for retrieving stream data (detailed time-series data) from Strava.
    /// </summary>
    /// <remarks>
    /// Streams represent detailed data points over time or distance, such as GPS coordinates,
    /// altitude, speed, heart rate, cadence, power, and more.
    ///
    /// <para><b>Features:</b></para>
    /// <list type="bullet">
    /// <item><description>Retrieve streams for a specific activity.</description></item>
    /// <item><description>Retrieve streams for a specific segment.</description></item>
    /// <item><description>Retrieve streams for a specific route.</description></item>
    /// <item><description>Retrieve streams for a specific segment effort.</description></item>
    /// </list>
    ///
    /// <para><b>Usage Example:</b></para>
    /// <code>
    /// // Example: Get lat/lng and altitude streams for an activity
    /// var streams = await Streams.GetActivityStreamsAsync(accessToken, 123456789, "latlng,altitude", true);
    /// Console.WriteLine(streams["latlng"]);
    /// </code>
    ///
    /// <para><b>API Documentation:</b></para>
    /// Refer to the official Strava API documentation for streams:
    /// <see href="https://developers.strava.com/docs/reference/#api-Streams">Strava Streams API Reference</see>.
    /// </remarks>
    static public class Streams
    {
        /// <summary>
        /// Get streams for a specific activity.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="activityId">The ID of the activity.</param>
        /// <param name="keys">Optional: Comma-separated list of stream types to retrieve (e.g., "latlng,altitude").</param>
        /// <param name="keyByType">Optional: If true, streams will be returned as a map keyed by type.</param>
        /// <returns>A <see cref="JsonObject"/> containing the streams data.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty or <paramref name="activityId"/> is invalid.
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
        public static async Task<JsonObject> GetActivityStreamsAsync(string accessToken, long activityId, string? keys = null, bool keyByType = false)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (activityId <= 0)
                throw new ArgumentException("Activity ID must be greater than zero.", nameof(activityId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var url = $"https://www.strava.com/api/v3/activities/{activityId}/streams";
            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(keys))
                queryParams.Add($"keys={Uri.EscapeDataString(keys)}");

            if (keyByType)
                queryParams.Add("key_by_type=true");

            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve activity streams. Status Code: {response.StatusCode}, Response: {content}");

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Failed to retrieve activity streams. The response was empty.");

            return JsonNode.Parse(content) as JsonObject
                   ?? throw new JsonException("Failed to parse activity streams JSON.");
        }


        /// <summary>
        /// Get streams for a specific segment.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="segmentId">The ID of the segment.</param>
        /// <param name="keys">Optional: Comma-separated list of stream keys (e.g., "distance,altitude,latlng").</param>
        /// <param name="keyByType">Optional: If true, streams will be returned as a map keyed by type.</param>
        /// <returns>A <see cref="JsonObject"/> containing the segment streams.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty or <paramref name="segmentId"/> is invalid.
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
        public static async Task<JsonObject> GetSegmentStreamsAsync(string accessToken, long segmentId, string? keys = null, bool keyByType = false)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (segmentId <= 0)
                throw new ArgumentException("Segment ID must be greater than zero.", nameof(segmentId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var url = $"https://www.strava.com/api/v3/segments/{segmentId}/streams";
            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(keys))
                queryParams.Add($"keys={Uri.EscapeDataString(keys)}");

            if (keyByType)
                queryParams.Add("key_by_type=true");

            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve segment streams. Status Code: {response.StatusCode}, Response: {content}");

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Failed to retrieve segment streams. The response was empty.");

            return JsonNode.Parse(content) as JsonObject
                   ?? throw new JsonException("Failed to parse segment streams JSON.");
        }


        /// <summary>
        /// Get streams for a specific route.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="routeId">The ID of the route.</param>
        /// <param name="keys">Optional: Comma-separated list of stream keys (e.g., "distance,altitude,latlng").</param>
        /// <param name="keyByType">Optional: If true, streams will be returned as a map keyed by type.</param>
        /// <returns>A <see cref="JsonObject"/> containing the route streams.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty or <paramref name="routeId"/> is invalid.
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
        public static async Task<JsonObject> GetRouteStreamsAsync(string accessToken, long routeId, string? keys = null, bool keyByType = false)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (routeId <= 0)
                throw new ArgumentException("Route ID must be greater than zero.", nameof(routeId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var url = $"https://www.strava.com/api/v3/routes/{routeId}/streams";
            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(keys))
                queryParams.Add($"keys={Uri.EscapeDataString(keys)}");

            if (keyByType)
                queryParams.Add("key_by_type=true");

            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve route streams. Status Code: {response.StatusCode}, Response: {content}");

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Failed to retrieve route streams. The response was empty.");

            return JsonNode.Parse(content) as JsonObject
                   ?? throw new JsonException("Failed to parse route streams JSON.");
        }


        /// <summary>
        /// Get streams for a specific segment effort.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="segmentEffortId">The ID of the segment effort.</param>
        /// <param name="keys">Optional: Comma-separated list of stream keys (e.g., "distance,time,latlng").</param>
        /// <param name="keyByType">Optional: If true, streams will be returned as a map keyed by type.</param>
        /// <returns>A <see cref="JsonObject"/> containing the segment effort streams.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty or <paramref name="segmentEffortId"/> is invalid.
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
        public static async Task<JsonObject> GetSegmentEffortStreamsAsync(string accessToken, long segmentEffortId, string? keys = null, bool keyByType = false)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (segmentEffortId <= 0)
                throw new ArgumentException("Segment Effort ID must be greater than zero.", nameof(segmentEffortId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var url = $"https://www.strava.com/api/v3/segment_efforts/{segmentEffortId}/streams";
            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(keys))
                queryParams.Add($"keys={Uri.EscapeDataString(keys)}");

            if (keyByType)
                queryParams.Add("key_by_type=true");

            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve segment effort streams. Status Code: {response.StatusCode}, Response: {content}");

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Failed to retrieve segment effort streams. The response was empty.");

            return JsonNode.Parse(content) as JsonObject
                   ?? throw new JsonException("Failed to parse segment effort streams JSON.");
        }


    }
}
