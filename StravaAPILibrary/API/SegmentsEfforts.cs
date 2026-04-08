using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

/* 
 * ToDo: NOT TESTED! because of the subscription requirement.
 */

namespace StravaAPILibary.API
{
    /// <summary>
    /// Provides methods for interacting with Strava's Segment Efforts API.
    /// </summary>
    /// <remarks>
    /// This static class includes methods to retrieve segment efforts for the authenticated athlete.
    /// Some endpoints require a Strava subscription and the <c>activity:read</c> OAuth scope.
    /// 
    /// <para><b>Features:</b></para>
    /// <list type="bullet">
    /// <item><description>Retrieve a list of segment efforts for the authenticated athlete and a given segment.</description></item>
    /// <item><description>Retrieve details of a specific segment effort by its ID.</description></item>
    /// </list>
    /// 
    /// <para><b>Usage Example:</b></para>
    /// <code>
    /// // Get all segment efforts for a segment (requires subscription):
    /// var efforts = await SegmentsEfforts.GetSegmentEffortsAsync(accessToken, 123456, DateTime.Parse("2024-01-01"), DateTime.Parse("2024-02-01"));
    ///
    /// // Get details of a specific segment effort:
    /// var effort = await SegmentsEfforts.GetSegmentEffortByIdAsync(accessToken, 987654);
    /// Console.WriteLine(effort["elapsed_time"]);
    /// </code>
    /// 
    /// <para><b>API Documentation:</b></para>
    /// Refer to the official Strava API documentation for segment efforts:
    /// <see href="https://developers.strava.com/docs/reference/#api-SegmentEfforts">Strava Segment Efforts API Reference</see>.
    /// </remarks>
    static public class SegmentsEfforts
    {
        /// <summary>
        /// Returns a set of the authenticated athlete's segment efforts for a given segment.
        /// Requires subscription and scope: activity:read.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="segmentId">The ID of the segment.</param>
        /// <param name="startDateLocal">Optional: ISO 8601 formatted start date (e.g. "2024-01-01T00:00:00Z").</param>
        /// <param name="endDateLocal">Optional: ISO 8601 formatted end date (e.g. "2024-02-01T00:00:00Z").</param>
        /// <param name="perPage">Number of items per page. Default: 30.</param>
        /// <returns>A <see cref="JsonArray"/> containing the list of segment efforts.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="accessToken"/> is null or empty, or <paramref name="segmentId"/> is invalid.</exception>
        /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the response is empty.</exception>
        /// <exception cref="JsonException">Thrown when the response cannot be parsed.</exception>
        static public  async Task<JsonArray> GetSegmentEffortsAsync(string accessToken, long segmentId, DateTime? startDateLocal = null, DateTime? endDateLocal = null, int perPage = 30)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            if (segmentId <= 0)
                throw new ArgumentException("Segment ID must be greater than zero.", nameof(segmentId));
            if (perPage <= 0)
                throw new ArgumentOutOfRangeException(nameof(perPage), "PerPage must be greater than zero.");

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            // Build query parameters
            var queryParams = new List<string> { $"segment_id={segmentId}", $"per_page={perPage}" };
            if (startDateLocal.HasValue)
                queryParams.Add($"start_date_local={Uri.EscapeDataString(startDateLocal.Value.ToString("o"))}");
            if (endDateLocal.HasValue)
                queryParams.Add($"end_date_local={Uri.EscapeDataString(endDateLocal.Value.ToString("o"))}");

            var url = $"https://www.strava.com/api/v3/segment_efforts?{string.Join("&", queryParams)}";

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve segment efforts. Status Code: {response.StatusCode}, Response: {content}");

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Failed to retrieve segment efforts. The response was empty.");

            return JsonNode.Parse(content) as JsonArray
                   ?? throw new JsonException("Failed to parse segment efforts JSON.");
        }

        /// <summary>
        /// Get a specific segment effort by its ID.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="segmentEffortId">The ID of the segment effort.</param>
        /// <returns>A <see cref="JsonObject"/> containing the segment effort details.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty, or when <paramref name="segmentEffortId"/> is invalid.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when the request to the Strava API fails or returns a non-success status code.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the API response is empty.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the API response cannot be parsed as JSON.
        /// </exception>
        public static async Task<JsonObject> GetSegmentEffortByIdAsync(string accessToken, long segmentEffortId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (segmentEffortId <= 0)
                throw new ArgumentException("Segment effort ID must be greater than zero.", nameof(segmentEffortId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://www.strava.com/api/v3/segment_efforts/{segmentEffortId}");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve segment effort. Status Code: {response.StatusCode}, Response: {content}");

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Failed to retrieve segment effort. The response was empty.");

            return JsonNode.Parse(content) as JsonObject
                   ?? throw new JsonException("Failed to parse segment effort JSON.");
        }

    }
}
