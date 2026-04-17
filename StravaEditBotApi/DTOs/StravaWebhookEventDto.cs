using System.Text.Json.Serialization;

namespace StravaEditBotApi.DTOs;

public record StravaWebhookEventDto(
    [property: JsonPropertyName("object_type")] string ObjectType,
    [property: JsonPropertyName("object_id")] long ObjectId,
    [property: JsonPropertyName("aspect_type")] string AspectType,
    [property: JsonPropertyName("updates")] Dictionary<string, string> Updates,
    [property: JsonPropertyName("owner_id")] long OwnerId,
    [property: JsonPropertyName("subscription_id")] long SubscriptionId,
    [property: JsonPropertyName("event_time")] long EventTime
);
