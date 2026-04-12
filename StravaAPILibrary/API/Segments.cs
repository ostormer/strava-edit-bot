using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;


namespace StravaAPILibary.API
{
    /// <summary>
    /// Provides methods for interacting with Strava's Segments API.
    /// </summary>
    /// <remarks>
    /// This static class includes methods to retrieve, explore, and manage segments for the authenticated athlete.
    /// 
    /// <para><b>Features:</b></para>
    /// <list type="bullet">
    /// <item><description>Retrieve detailed information about a specific segment by its ID.</description></item>
    /// <item><description>Get a list of starred segments for the authenticated athlete.</description></item>
    /// <item><description>Explore top 10 segments within a defined geographical bounding box.</description></item>
    /// <item><description>Star or unstar a segment for the authenticated athlete.</description></item>
    /// </list>
    /// 
    /// <para><b>Usage Example:</b></para>
    /// <code>
    /// // Retrieve a segment by ID:
    /// var segment = await Segments.GetSegmentByIdAsync(accessToken, "12345");
    /// Console.WriteLine(segment["name"]);
    ///
    /// // Star a segment:
    /// var updatedSegment = await Segments.PutStarredSegmentAsync(accessToken, 12345, true);
    /// </code>
    /// 
    /// <para><b>API Documentation:</b></para>
    /// Refer to the official Strava API documentation for segments:
    /// <see href="https://developers.strava.com/docs/reference/#api-Segments">Strava Segments API Reference</see>.
    /// </remarks>
    static public class Segments
    {
        /// <summary>
        /// Get a segment by its ID.
        /// </summary>
        /// <param name="accessToken">The access token for the API.</param>
        /// <param name="segmentId">The ID of the segment to retrieve.</param>
        /// <returns>A <see cref="JsonObject"/> containing the segment data.</returns>
        /// <exception cref="ArgumentException">Thrown when the access token or segment ID is null or empty.</exception>
        /// <exception cref="HttpRequestException">Thrown when the request to the API fails.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the response is empty.</exception>
        /// <exception cref="JsonException">Thrown when the JSON response cannot be parsed.</exception>
        public static async Task<JsonObject> GetSegmentByIdAsync(string accessToken, string segmentId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (string.IsNullOrWhiteSpace(segmentId))
                throw new ArgumentException("Segment ID cannot be null or empty.", nameof(segmentId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://www.strava.com/api/v3/segments/{segmentId}");
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve segment. Status Code: {response.StatusCode}");

            string jsonResponse = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(jsonResponse))
                throw new InvalidOperationException("Failed to retrieve segment. The response was empty.");

            return JsonNode.Parse(jsonResponse) as JsonObject
                   ?? throw new JsonException("Failed to parse segment JSON.");
        }


        /// <summary>
        /// Get starred segments for the authenticated athlete.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="page">Page number (default: 1).</param>
        /// <param name="perPage">Number of items per page (default: 30).</param>
        /// <returns>A <see cref="JsonArray"/> containing the starred segments.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="page"/> or <paramref name="perPage"/> is less than or equal to zero.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when the API request fails or returns a non-success status code.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the API response is empty.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the response JSON cannot be parsed as a <see cref="JsonArray"/>.
        /// </exception>
        public static async Task<JsonArray> GetStarredSegmentsAsync(string accessToken, int page = 1, int perPage = 30)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (page <= 0)
                throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than 0.");

            if (perPage <= 0)
                throw new ArgumentOutOfRangeException(nameof(perPage), "PerPage must be greater than 0.");

            string url = $"https://www.strava.com/api/v3/segments/starred?page={page}&per_page={perPage}";

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve starred segments. Status Code: {response.StatusCode}");

            string jsonResponse = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(jsonResponse))
                throw new InvalidOperationException("Failed to retrieve starred segments. The response was empty.");

            return JsonNode.Parse(jsonResponse) as JsonArray
                   ?? throw new JsonException("Failed to parse starred segments JSON.");
        }



        /// <summary>
        /// Get a list of top 10 segments for the specified query.
        /// </summary>
        /// <param name="accessToken">Strava access token.</param>
        /// <param name="bounds">Array of four float values [SW lat, SW lon, NE lat, NE lon].</param>
        /// <param name="activityType">Activity type: "running" or "riding".</param>
        /// <param name="minCat">Minimum climbing category (-1 for any).</param>
        /// <param name="maxCat">Maximum climbing category (-1 for any).</param>
        /// <returns>A <see cref="JsonArray"/> containing the top 10 segments matching the query.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty,
        /// or when <paramref name="bounds"/> is null or does not contain exactly 4 elements.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when the request to the Strava API fails or returns a non-success status code.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the API response is empty.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the JSON response cannot be parsed 
        /// or does not contain a 'segments' array.
        /// </exception>$
        public static async Task<JsonArray> GetExploreSegmentsAsync(string accessToken, float[] bounds, string activityType = "", int minCat = -1, int maxCat = -1)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (bounds == null || bounds.Length != 4)
                throw new ArgumentException("Bounds must be [SW lat, SW lon, NE lat, NE lon].", nameof(bounds));

            string boundsString = string.Join(",", bounds);
            string url = $"https://www.strava.com/api/v3/segments/explore?bounds={boundsString}&activity_type={activityType}&min_cat={minCat}&max_cat={maxCat}";

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve explore segments. Status Code: {response.StatusCode}");

            string jsonResponse = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(jsonResponse))
                throw new InvalidOperationException("Failed to retrieve explore segments. The response was empty.");

            var root = JsonNode.Parse(jsonResponse)?.AsObject()
                ?? throw new JsonException("Failed to parse explore segments JSON.");

            return root["segments"]?.AsArray()
                ?? throw new JsonException("The JSON does not contain a 'segments' array.");
        }

        /// <summary>
        /// Stars or unstars a segment for the authenticated athlete.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="segmentId">The ID of the segment to star or unstar.</param>
        /// <param name="starred">True to star the segment; false to unstar.</param>
        /// <returns>A <see cref="JsonObject"/> containing the updated segment data.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty, or when <paramref name="segmentId"/> is invalid.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when the request to the Strava API fails or returns a non-success status code.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the API response is empty.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the API response cannot be parsed.
        /// </exception>
        public static async Task<JsonObject> PutStarredSegmentAsync(string accessToken, long segmentId, bool starred)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (segmentId <= 0)
                throw new ArgumentException("Segment ID must be greater than zero.", nameof(segmentId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var payload = new JsonObject { ["starred"] = starred };
            var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"https://www.strava.com/api/v3/segments/{segmentId}/starred", content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to update starred status. Status Code: {response.StatusCode}, Response: {jsonResponse}");

            if (string.IsNullOrWhiteSpace(jsonResponse))
                throw new InvalidOperationException("Failed to update starred status. The response was empty.");

            return JsonNode.Parse(jsonResponse) as JsonObject
                   ?? throw new JsonException("Failed to parse starred segment JSON.");
        }

    }
}
