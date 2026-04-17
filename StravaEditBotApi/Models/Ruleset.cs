using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.Models;

public class Ruleset
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; }
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Recomputed on every save. False if filter/effect is missing or incomplete.
    /// Only valid rulesets are evaluated at runtime.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Nullable — null means the user has not yet configured a filter (draft state).
    /// </summary>
    public FilterExpression? Filter { get; set; }

    /// <summary>
    /// Nullable — null means the user has not yet configured an effect (draft state).
    /// </summary>
    public RulesetEffect? Effect { get; set; }

    public int? CreatedFromTemplateId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public AppUser User { get; set; } = null!;
    public RulesetTemplate? CreatedFromTemplate { get; set; }
    public ICollection<RulesetRun> Runs { get; set; } = new List<RulesetRun>();
}
