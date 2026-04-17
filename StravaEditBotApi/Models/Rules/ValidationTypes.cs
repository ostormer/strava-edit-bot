namespace StravaEditBotApi.Models.Rules;

/// <summary>
/// Result of validating a ruleset's filter and effect.
/// Returned by the validation endpoint and included in create/update responses.
/// </summary>
public record RulesetValidationResult(
    bool IsValid,
    List<RulesetValidationError> Errors
);

/// <summary>
/// A single validation error with a path pointing to the problematic node.
/// </summary>
public record RulesetValidationError(
    /// <summary>
    /// JSON-path-style pointer to the error location.
    /// Examples: "filter", "filter.conditions[2].value", "effect.name"
    /// </summary>
    string Path,

    /// <summary>
    /// Machine-readable error code.
    /// Examples: "filter_required", "incomplete_check", "invalid_operator"
    /// </summary>
    string Code,

    /// <summary>
    /// Human-readable description of the problem.
    /// </summary>
    string Message
);
