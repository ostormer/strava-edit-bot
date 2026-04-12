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
    /// Provides methods for uploading activities and retrieving upload statuses from the Strava API.
    /// </summary>
    /// <remarks>
    /// The <c>Uploads</c> class supports two main operations:
    /// <list type="bullet">
    /// <item><description>Uploading activity files (FIT, GPX, TCX) to Strava.</description></item>
    /// <item><description>Checking the processing status of previously uploaded activity files.</description></item>
    /// </list>
    /// 
    /// <para><b>Important Notes:</b></para>
    /// <list type="bullet">
    /// <item><description>Uploads may take a while to process. You should poll the upload status using <see cref="GetUploadAsync"/>.</description></item>
    /// <item><description>Uploading activities requires an authenticated athlete's access token with write permissions.</description></item>
    /// </list>
    /// 
    /// <para><b>Example Usage:</b></para>
    /// <code>
    /// // Upload an activity file
    /// var uploadResponse = await Uploads.UploadActivityAsync(accessToken, "activity.fit", "fit", "Morning Ride", "Great ride", false, false);
    /// Console.WriteLine(uploadResponse["id"]);
    /// 
    /// // Check upload status
    /// var uploadStatus = await Uploads.GetUploadAsync(accessToken, (long)uploadResponse["id"]);
    /// Console.WriteLine(uploadStatus["status"]);
    /// </code>
    /// 
    /// <para><b>API Reference:</b></para>
    /// See: <see href="https://developers.strava.com/docs/reference/#api-Uploads">Strava Uploads API Documentation</see>.
    /// </remarks>
    static public class Uploads
    {
        /// <summary>
        /// Uploads an activity file to Strava.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="filePath">The path to the activity file (FIT, GPX, TCX).</param>
        /// <param name="dataType">The file type: "fit", "gpx", or "tcx".</param>
        /// <param name="name">Optional: The name of the activity.</param>
        /// <param name="description">Optional: A description of the activity.</param>
        /// <param name="isTrainer">Optional: Set to true if this is a trainer ride.</param>
        /// <param name="isCommute">Optional: Set to true if this is a commute.</param>
        /// <returns>A <see cref="JsonObject"/> containing the upload status.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> or <paramref name="filePath"/> is invalid.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Thrown when the specified file does not exist.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when the upload request fails.
        /// </exception>
        public static async Task<JsonObject> UploadActivityAsync(string accessToken, string filePath, string dataType, string? name = null, string? description = null, bool isTrainer = false, bool isCommute = false)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file does not exist.", filePath);

            if (string.IsNullOrWhiteSpace(dataType))
                throw new ArgumentException("Data type must be specified (fit, gpx, tcx).", nameof(dataType));

            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) }; // Uploads können länger dauern
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(File.OpenRead(filePath)), "file", Path.GetFileName(filePath));
            content.Add(new StringContent(dataType), "data_type");

            if (!string.IsNullOrWhiteSpace(name))
                content.Add(new StringContent(name), "name");
            if (!string.IsNullOrWhiteSpace(description))
                content.Add(new StringContent(description), "description");

            if (isTrainer) content.Add(new StringContent("true"), "trainer");
            if (isCommute) content.Add(new StringContent("true"), "commute");

            var response = await client.PostAsync("https://www.strava.com/api/v3/uploads", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to upload activity. Status Code: {response.StatusCode}, Response: {responseContent}");

            return JsonNode.Parse(responseContent) as JsonObject
                   ?? throw new JsonException("Failed to parse upload response JSON.");
        }


        /// <summary>
        /// Get an upload status by its ID.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="uploadId">The ID of the upload to retrieve.</param>
        /// <returns>A <see cref="JsonObject"/> containing the upload status and details.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty or when <paramref name="uploadId"/> is invalid.
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
        public static async Task<JsonObject> GetUploadAsync(string accessToken, long uploadId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (uploadId <= 0)
                throw new ArgumentException("Upload ID must be greater than zero.", nameof(uploadId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://www.strava.com/api/v3/uploads/{uploadId}");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve upload status. Status Code: {response.StatusCode}, Response: {content}");

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Failed to retrieve upload status. The response was empty.");

            return JsonNode.Parse(content) as JsonObject
                   ?? throw new JsonException("Failed to parse upload JSON.");
        }

    }
}
