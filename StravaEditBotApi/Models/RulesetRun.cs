namespace StravaEditBotApi.Models;

public class RulesetRun
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public long StravaActivityId { get; set; }

    /// <summary>
    /// Nullable — null if no ruleset matched.
    /// Set to null (not deleted) when the referenced ruleset is deleted.
    /// </summary>
    public int? RulesetId { get; set; }

    /// <summary>
    /// Snapshot of the ruleset name at the time of the run.
    /// Preserved even if the ruleset is later deleted or renamed.
    /// </summary>
    public string? RulesetName { get; set; }

    /// <summary>
    /// NoMatch | Applied | Failed | Skipped
    /// </summary>
    public string Status { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    /// <summary>
    /// JSON object listing which fields were changed and to what values.
    /// Null if no match.
    /// </summary>
    public Dictionary<string, string>? FieldsChanged { get; set; }

    public DateTime ProcessedAt { get; set; }
    public DateTime StravaEventTime { get; set; }

    // Navigation properties
    public AppUser User { get; set; } = null!;
    public Ruleset? Ruleset { get; set; }
}

/// <summary>
/// Well-known values for RulesetRun.Status.
/// </summary>
public static class RulesetRunStatus
{
    public const string NoMatch = "NoMatch";
    public const string Applied = "Applied";
    public const string Failed = "Failed";
    public const string Skipped = "Skipped";
}
