using System.Text.Json;
using System.Text.Json.Serialization;

namespace StravaEditBotApi.Models.Rules;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AndFilter), "and")]
[JsonDerivedType(typeof(OrFilter), "or")]
[JsonDerivedType(typeof(NotFilter), "not")]
[JsonDerivedType(typeof(CheckFilter), "check")]
public abstract record FilterExpression;

public record AndFilter(
    [property: JsonPropertyName("conditions")]
    List<FilterExpression> Conditions
) : FilterExpression;

public record OrFilter(
    [property: JsonPropertyName("conditions")]
    List<FilterExpression> Conditions
) : FilterExpression;

public record NotFilter(
    [property: JsonPropertyName("condition")]
    FilterExpression Condition
) : FilterExpression;

/// <summary>
/// Leaf node: checks a single activity property against a value.
/// All fields are nullable to support saving incomplete/draft rulesets.
/// All three must be non-null for the ruleset to be valid.
/// </summary>
public record CheckFilter(
    [property: JsonPropertyName("property")]
    string? Property,

    [property: JsonPropertyName("operator")]
    string? Operator,

    [property: JsonPropertyName("value")]
    JsonElement? Value
) : FilterExpression;
