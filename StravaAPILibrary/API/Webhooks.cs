using System.Text.Json;
using System.Text.Json.Nodes;

namespace StravaAPILibrary.API;

/// <summary>
/// Provides methods to manage Strava webhook push subscriptions.
/// </summary>
/// <remarks>
/// This static class wraps the Strava Push Subscriptions API. Unlike other API classes
/// in this library, these methods use <c>client_id</c> and <c>client_secret</c> for
/// authentication instead of a user access token — they are admin/setup operations,
/// not per-user calls.
///
/// <para>Each Strava application can have at most one active push subscription.</para>
///
/// <para><b>API Documentation:</b></para>
/// <see href="https://developers.strava.com/docs/webhooks/">Strava Webhooks</see>.
/// </remarks>
static public class Webhooks
{
    private const string SubscriptionsUrl = "https://www.strava.com/api/v3/push_subscriptions";

    /// <summary>
    /// Creates a new webhook push subscription for the application.
    /// </summary>
    /// <param name="clientId">The Strava application's client ID.</param>
    /// <param name="clientSecret">The Strava application's client secret.</param>
    /// <param name="callbackUrl">The URL that Strava will POST webhook events to (max 255 characters).</param>
    /// <param name="verifyToken">A shared secret used to verify the subscription handshake.</param>
    /// <returns>A <see cref="JsonObject"/> containing the subscription ID.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when any parameter is null or empty.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// Thrown when the Strava API returns a non-success status code.
    /// </exception>
    /// <exception cref="JsonException">
    /// Thrown when the response cannot be parsed as a <see cref="JsonObject"/>.
    /// </exception>
    public static async Task<JsonObject> CreateSubscriptionAsync(string clientId, string clientSecret, string callbackUrl, string verifyToken)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("Client ID cannot be null or empty.", nameof(clientId));

        if (string.IsNullOrWhiteSpace(clientSecret))
            throw new ArgumentException("Client secret cannot be null or empty.", nameof(clientSecret));

        if (string.IsNullOrWhiteSpace(callbackUrl))
            throw new ArgumentException("Callback URL cannot be null or empty.", nameof(callbackUrl));

        if (string.IsNullOrWhiteSpace(verifyToken))
            throw new ArgumentException("Verify token cannot be null or empty.", nameof(verifyToken));

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        var formData = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["callback_url"] = callbackUrl,
            ["verify_token"] = verifyToken,
        };

        var response = await client.PostAsync(SubscriptionsUrl, new FormUrlEncodedContent(formData));
        string responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Failed to create webhook subscription. Status Code: {response.StatusCode}, Response: {responseContent}");

        return JsonNode.Parse(responseContent) as JsonObject
               ?? throw new JsonException("Failed to parse subscription response JSON.");
    }

    /// <summary>
    /// Retrieves the existing webhook subscription for the application.
    /// </summary>
    /// <param name="clientId">The Strava application's client ID.</param>
    /// <param name="clientSecret">The Strava application's client secret.</param>
    /// <returns>A <see cref="JsonArray"/> containing the subscription(s). Empty if none exist.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when any parameter is null or empty.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// Thrown when the Strava API returns a non-success status code.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the API response is empty.
    /// </exception>
    /// <exception cref="JsonException">
    /// Thrown when the response cannot be parsed as a <see cref="JsonArray"/>.
    /// </exception>
    public static async Task<JsonArray> GetSubscriptionAsync(string clientId, string clientSecret)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("Client ID cannot be null or empty.", nameof(clientId));

        if (string.IsNullOrWhiteSpace(clientSecret))
            throw new ArgumentException("Client secret cannot be null or empty.", nameof(clientSecret));

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        string url = $"{SubscriptionsUrl}?client_id={Uri.EscapeDataString(clientId)}&client_secret={Uri.EscapeDataString(clientSecret)}";

        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Failed to retrieve webhook subscription. Status Code: {response.StatusCode}");

        string jsonResponse = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(jsonResponse))
            throw new InvalidOperationException("Failed to retrieve webhook subscription. The response was empty.");

        return JsonNode.Parse(jsonResponse) as JsonArray
               ?? throw new JsonException("Failed to parse subscription response JSON.");
    }

    /// <summary>
    /// Deletes an existing webhook subscription.
    /// </summary>
    /// <param name="clientId">The Strava application's client ID.</param>
    /// <param name="clientSecret">The Strava application's client secret.</param>
    /// <param name="subscriptionId">The ID of the subscription to delete.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="clientId"/> or <paramref name="clientSecret"/> is null or empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="subscriptionId"/> is less than or equal to zero.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// Thrown when the Strava API returns a non-success status code.
    /// </exception>
    public static async Task DeleteSubscriptionAsync(string clientId, string clientSecret, long subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("Client ID cannot be null or empty.", nameof(clientId));

        if (string.IsNullOrWhiteSpace(clientSecret))
            throw new ArgumentException("Client secret cannot be null or empty.", nameof(clientSecret));

        if (subscriptionId <= 0)
            throw new ArgumentOutOfRangeException(nameof(subscriptionId), "Subscription ID must be greater than 0.");

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        string url = $"{SubscriptionsUrl}/{subscriptionId}?client_id={Uri.EscapeDataString(clientId)}&client_secret={Uri.EscapeDataString(clientSecret)}";

        var response = await client.DeleteAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to delete webhook subscription. Status Code: {response.StatusCode}, Response: {responseContent}");
        }
    }
}
