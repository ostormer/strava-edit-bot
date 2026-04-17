using System.Text.Json.Serialization;

namespace StravaEditBotApi.Models.Rules;

/// <summary>
/// Defines which activity fields to edit and what values to set.
/// Null fields are not sent to the Strava API.
/// String fields support {variable} template interpolation.
/// </summary>
public record RulesetEffect
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("sport_type")]
    public string? SportType { get; init; }

    [JsonPropertyName("gear_id")]
    public string? GearId { get; init; }

    [JsonPropertyName("commute")]
    public bool? Commute { get; init; }

    [JsonPropertyName("trainer")]
    public bool? Trainer { get; init; }

    [JsonPropertyName("hide_from_home")]
    public bool? HideFromHome { get; init; }
}
