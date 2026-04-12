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
    /// Provides methods to interact with Strava's Activities API.
    /// </summary>
    /// <remarks>
    /// This static class contains methods for retrieving, creating, and updating athlete activities in Strava.
    /// It communicates with Strava's REST API endpoints related to activities and returns JSON responses.
    /// 
    /// <para><b>Features:</b></para>
    /// <list type="bullet">
    /// <item><description>Retrieve authenticated athlete's activities.</description></item>
    /// <item><description>Fetch details of a specific activity by ID.</description></item>
    /// <item><description>Get related data like laps, zones, comments, and kudos.</description></item>
    /// <item><description>Upload new activities and update existing ones.</description></item>
    /// </list>
    /// 
    /// <para><b>Usage:</b></para>
    /// All methods require a valid <c>accessToken</c> (OAuth token) with appropriate scopes granted by Strava.
    /// Many methods also support pagination and optional filtering parameters.
    /// 
    /// <para><b>API Documentation:</b></para>
    /// Refer to the official Strava API documentation: 
    /// <see href="https://developers.strava.com/docs/reference/">Strava API Reference</see>.
    /// </remarks>
    /// <example>
    /// Example: Retrieve the authenticated athlete's activities:
    /// <code>
    /// var activities = await Activities.GetAthletesActivitiesAsync(accessToken, page: 1, perPage: 30);
    /// </code>
    /// 
    /// Example: Upload a new activity:
    /// <code>
    /// var uploadResponse = await Activities.PostActivityAsync(accessToken, "Morning Ride", "gpx", @"C:\ride.gpx");
    /// </code>
    /// </example>
    static public class Activities
    {
        /// <summary>
        /// Retrieves the activities of the authenticated athlete with optional filtering and pagination.
        /// </summary>
        /// <param name="accessToken">The OAuth access token for authentication. Must be valid and not expired.</param>
        /// <param name="before">Unix timestamp to filter activities before this date. Use 0 for no filter.</param>
        /// <param name="after">Unix timestamp to filter activities after this date. Use 0 for no filter.</param>
        /// <param name="page">Page number for pagination. Must be greater than 0.</param>
        /// <param name="perPage">Number of activities per page. Must be between 1 and 200.</param>
        /// <returns>A <see cref="JsonArray"/> containing the athlete's activities.</returns>
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
        /// <remarks>
        /// This method retrieves the authenticated athlete's activities from Strava. Activities are returned
        /// in reverse chronological order (most recent first).
        /// 
        /// <para><b>Filtering:</b></para>
        /// <list type="bullet">
        /// <item><description>Use <paramref name="after"/> to get activities from a specific date onwards</description></item>
        /// <item><description>Use <paramref name="before"/> to get activities up to a specific date</description></item>
        /// <item><description>Both filters use Unix timestamps (seconds since epoch)</description></item>
        /// <item><description>Use 0 for either parameter to disable that filter</description></item>
        /// </list>
        /// 
        /// <para><b>Pagination:</b></para>
        /// <list type="bullet">
        /// <item><description>Activities are paginated with a maximum of 200 per page</description></item>
        /// <item><description>Use <paramref name="page"/> to navigate through pages</description></item>
        /// <item><description>Use <paramref name="perPage"/> to control the number of activities returned</description></item>
        /// <item><description>Empty array is returned when no more activities are available</description></item>
        /// </list>
        /// 
        /// <para><b>Required Scope:</b></para>
        /// This method requires the <c>activity:read_all</c> scope to access all activities, or <c>activity:read</c> for public activities only.
        /// 
        /// <para><b>Rate Limits:</b></para>
        /// This endpoint is subject to Strava's API rate limits. Consider implementing exponential backoff for retry logic.
        /// </remarks>
        /// <example>
        /// <para>Get recent activities:</para>
        /// <code>
        /// var activities = await Activities.GetAthletesActivitiesAsync(accessToken, page: 1, perPage: 10);
        /// foreach (var activity in activities)
        /// {
        ///     Console.WriteLine($"Activity: {activity["name"]} - {activity["distance"]}m");
        /// }
        /// </code>
        /// 
        /// <para>Get activities from last 30 days:</para>
        /// <code>
        /// int after = (int)DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds();
        /// var recentActivities = await Activities.GetAthletesActivitiesAsync(accessToken, after: after);
        /// </code>
        /// 
        /// <para>Get activities before a specific date:</para>
        /// <code>
        /// int before = (int)DateTimeOffset.Parse("2024-01-01").ToUnixTimeSeconds();
        /// var oldActivities = await Activities.GetAthletesActivitiesAsync(accessToken, before: before);
        /// </code>
        /// 
        /// <para>Implement pagination:</para>
        /// <code>
        /// int page = 1;
        /// int perPage = 50;
        /// bool hasMoreActivities = true;
        /// 
        /// while (hasMoreActivities)
        /// {
        ///     var activities = await Activities.GetAthletesActivitiesAsync(accessToken, page: page, perPage: perPage);
        ///     
        ///     if (activities.Count == 0)
        ///     {
        ///         hasMoreActivities = false;
        ///     }
        ///     else
        ///     {
        ///         // Process activities
        ///         foreach (var activity in activities)
        ///         {
        ///             Console.WriteLine($"Activity: {activity["name"]}");
        ///         }
        ///         
        ///         page++;
        ///     }
        /// }
        /// </code>
        /// </example>
        public static async Task<JsonArray> GetAthletesActivitiesAsync(string accessToken, int before = 0, int after = 0, int page = 1, int perPage = 30)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (page <= 0)
                throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than 0.");

            if (perPage <= 0)
                throw new ArgumentOutOfRangeException(nameof(perPage), "PerPage must be greater than 0.");

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var queryParams = new List<string>
            {
                $"page={page}",
                $"per_page={perPage}"
            };

            if (before > 0) queryParams.Add($"before={before}");
            if (after > 0) queryParams.Add($"after={after}");

            var url = $"https://www.strava.com/api/v3/athlete/activities?{string.Join("&", queryParams)}";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve activities. Status Code: {response.StatusCode}");

            var jsonResponse = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(jsonResponse))
                throw new InvalidOperationException("Failed to retrieve activities. The response was empty.");

            return JsonNode.Parse(jsonResponse) as JsonArray
                   ?? throw new JsonException("Failed to parse activities JSON.");
        }

        /// <summary>
        /// Get a specific activity by its ID.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="activityId">The ID of the activity to retrieve.</param>
        /// <returns>A <see cref="JsonObject"/> containing the activity details.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> or <paramref name="activityId"/> is null or empty.
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
        public static async Task<JsonObject> GetActivityByIdAsync(string accessToken, string activityId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (string.IsNullOrWhiteSpace(activityId))
                throw new ArgumentException("Activity ID cannot be null or empty.", nameof(activityId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://www.strava.com/api/v3/activities/{activityId}");
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve activity. Status Code: {response.StatusCode}");

            var jsonResponse = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(jsonResponse))
                throw new InvalidOperationException("Failed to retrieve activity. The response was empty.");

            return JsonNode.Parse(jsonResponse) as JsonObject
                   ?? throw new JsonException("Failed to parse activity JSON.");
        }

        /// <summary>
        /// Get a list of laps for a specific activity.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="activityId">The ID of the activity to retrieve laps for.</param>
        /// <returns>A <see cref="JsonArray"/> containing the laps of the specified activity.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> or <paramref name="activityId"/> is null or empty.
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
        public static async Task<JsonArray> GetActivityLapsAsync(string accessToken, string activityId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (string.IsNullOrWhiteSpace(activityId))
                throw new ArgumentException("Activity ID cannot be null or empty.", nameof(activityId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://www.strava.com/api/v3/activities/{activityId}/laps");
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve laps. Status Code: {response.StatusCode}");

            var jsonResponse = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(jsonResponse))
                throw new InvalidOperationException("Failed to retrieve laps. The response was empty.");

            return JsonNode.Parse(jsonResponse) as JsonArray
                   ?? throw new JsonException("Failed to parse laps JSON.");
        }

        /// <summary>
        /// Get the zones for a specific activity.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="activityId">The ID of the activity to retrieve zones for.</param>
        /// <returns>A <see cref="JsonArray"/> containing the activity's zones.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> or <paramref name="activityId"/> is null or empty.
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
        static public async Task<JsonArray> GetActivityZonesAsync(string accessToken, string activityId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (string.IsNullOrWhiteSpace(activityId))
                throw new ArgumentException("Activity ID cannot be null or empty.", nameof(activityId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://www.strava.com/api/v3/activities/{activityId}/zones");
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve zones. Status Code: {response.StatusCode}");

            var jsonResponse = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(jsonResponse))
                throw new InvalidOperationException("Failed to retrieve zones. The response was empty.");

            return JsonNode.Parse(jsonResponse) as JsonArray
                   ?? throw new JsonException("Failed to parse zones JSON.");
        }

        /// <summary>
        /// Get the comments for a specific activity.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="activityId">The ID of the activity to retrieve comments for.</param>
        /// <returns>A <see cref="JsonArray"/> containing the comments of the activity.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> or <paramref name="activityId"/> is null or empty.
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
        public static async Task<JsonArray> GetActivityCommentsAsync(string accessToken, string activityId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (string.IsNullOrWhiteSpace(activityId))
                throw new ArgumentException("Activity ID cannot be null or empty.", nameof(activityId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://www.strava.com/api/v3/activities/{activityId}/comments");
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve comments. Status Code: {response.StatusCode}");

            var jsonResponse = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(jsonResponse))
                throw new InvalidOperationException("Failed to retrieve comments. The response was empty.");

            return JsonNode.Parse(jsonResponse) as JsonArray
                   ?? throw new JsonException("Failed to parse comments JSON.");
        }

        /// <summary>
        /// Get the kudos given for a specific activity.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="activityId">The ID of the activity to retrieve kudos for.</param>
        /// <returns>A <see cref="JsonArray"/> containing the kudos for the activity.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> or <paramref name="activityId"/> is null or empty.
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
        public static async Task<JsonArray> GetActivityKudosAsync(string accessToken, string activityId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (string.IsNullOrWhiteSpace(activityId))
                throw new ArgumentException("Activity ID cannot be null or empty.", nameof(activityId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://www.strava.com/api/v3/activities/{activityId}/kudos");
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve kudos. Status Code: {response.StatusCode}");

            var jsonResponse = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(jsonResponse))
                throw new InvalidOperationException("Failed to retrieve kudos. The response was empty.");

            return JsonNode.Parse(jsonResponse) as JsonArray
                   ?? throw new JsonException("Failed to parse kudos JSON.");
        }

        /// <summary>
        /// Posts (uploads) an activity file to Strava.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="name">The name of the activity.</param>
        /// <param name="dataType">The file type of the upload (fit, gpx, tcx).</param>
        /// <param name="filePath">The path to the activity file.</param>
        /// <param name="description">Optional: A description of the activity.</param>
        /// <param name="isTrainer">Optional: Set to true if this is a trainer ride.</param>
        /// <param name="isCommute">Optional: Set to true if this is a commute activity.</param>
        /// <param name="externalId">Optional: An external ID to uniquely identify this upload.</param>
        /// <param name="sportType">Optional: The sport type (e.g., "Run", "Ride").</param>
        /// <returns>A <see cref="JsonObject"/> containing the upload status.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when accessToken, name, or dataType are invalid.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Thrown when the specified file does not exist.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when the Strava API request fails.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the response cannot be parsed as JSON.
        /// </exception>
        public static async Task<JsonObject> PostActivityAsync(string accessToken, string name, string dataType, string filePath, string? description = null, bool isTrainer = false, bool isCommute = false, string? externalId = null, string? sportType = null)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));

            if (string.IsNullOrWhiteSpace(dataType))
                throw new ArgumentException("Data type must be specified (fit, gpx, tcx).", nameof(dataType));

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("The specified activity file does not exist.", filePath);

            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) }; // Uploads können länger dauern
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(File.OpenRead(filePath)), "file", Path.GetFileName(filePath));
            content.Add(new StringContent(dataType), "data_type");
            content.Add(new StringContent(name), "name");

            if (!string.IsNullOrWhiteSpace(description))
                content.Add(new StringContent(description), "description");

            if (isTrainer)
                content.Add(new StringContent("true"), "trainer");

            if (isCommute)
                content.Add(new StringContent("true"), "commute");

            if (!string.IsNullOrWhiteSpace(externalId))
                content.Add(new StringContent(externalId), "external_id");

            if (!string.IsNullOrWhiteSpace(sportType))
                content.Add(new StringContent(sportType), "sport_type");

            var response = await client.PostAsync("https://www.strava.com/api/v3/uploads", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to upload activity. Status Code: {response.StatusCode}, Response: {responseContent}");

            return JsonNode.Parse(responseContent) as JsonObject
                   ?? throw new JsonException("Failed to parse upload response JSON.");
        }

        /// <summary>
        /// Updates an activity by its ID.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="activityId">The ID of the activity to update.</param>
        /// <param name="name">Optional: New name for the activity.</param>
        /// <param name="description">Optional: New description for the activity.</param>
        /// <param name="isTrainer">Optional: Whether the activity is a trainer ride.</param>
        /// <param name="isCommute">Optional: Whether the activity is a commute.</param>
        /// <param name="sportType">Optional: The sport type (e.g., "Run", "Ride").</param>
        /// <param name="gearId">Optional: The gear ID to associate with the activity.</param>
        /// <returns>A <see cref="JsonObject"/> containing the updated activity data.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="accessToken"/> is null or empty or <paramref name="activityId"/> is invalid.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when the request to the Strava API fails or returns a non-success status code.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the JSON response cannot be parsed.
        /// </exception>
        public static async Task<JsonObject> UpdateActivityAsync(string accessToken, long activityId, string? name = null, string? description = null, bool? isTrainer = null, bool? isCommute = null, string? sportType = null, string? gearId = null)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            if (activityId <= 0)
                throw new ArgumentException("Activity ID must be greater than zero.", nameof(activityId));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var content = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(name)) content["name"] = name;
            if (!string.IsNullOrWhiteSpace(description)) content["description"] = description;
            if (isTrainer.HasValue) content["trainer"] = isTrainer.Value.ToString().ToLower();
            if (isCommute.HasValue) content["commute"] = isCommute.Value.ToString().ToLower();
            if (!string.IsNullOrWhiteSpace(sportType)) content["sport_type"] = sportType;
            if (!string.IsNullOrWhiteSpace(gearId)) content["gear_id"] = gearId;

            var response = await client.PutAsync(
                $"https://www.strava.com/api/v3/activities/{activityId}",
                new FormUrlEncodedContent(content)
            );

            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to update activity. Status Code: {response.StatusCode}, Response: {responseContent}");

            return JsonNode.Parse(responseContent) as JsonObject
                   ?? throw new JsonException("Failed to parse update response JSON.");
        }

    }
}