using System.Text.Json.Serialization;

namespace StravaEditBotApi.Models.Rules;

/// <summary>
/// A user-defined template variable.
/// Evaluated at runtime: cases are checked in order, first match wins.
/// </summary>
public record CustomVariableDefinition
{
    /// <summary>
    /// Variable name (without braces). Used as {name} in effect templates.
    /// Required when bundled in a RulesetTemplate; redundant (but kept) on the CustomVariable entity.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("cases")]
    public required List<VariableCase> Cases { get; init; }

    [JsonPropertyName("default_value")]
    public required string DefaultValue { get; init; }
}

/// <summary>
/// A single case in a custom variable: "if condition, then output".
/// </summary>
public record VariableCase
{
    [JsonPropertyName("condition")]
    public required FilterExpression Condition { get; init; }

    /// <summary>
    /// The string value to output if the condition matches.
    /// May contain {variable} references to built-in variables, but not other custom variables.
    /// </summary>
    [JsonPropertyName("output")]
    public required string Output { get; init; }
}
