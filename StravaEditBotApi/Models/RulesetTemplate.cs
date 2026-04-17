using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.Models;

public class RulesetTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Same schema as Ruleset.Filter. Non-null on system-predefined templates.
    /// </summary>
    public FilterExpression? Filter { get; set; }

    /// <summary>
    /// Same schema as Ruleset.Effect. Non-null on system-predefined templates.
    /// </summary>
    public RulesetEffect? Effect { get; set; }

    public string? CreatedByUserId { get; set; }

    /// <summary>
    /// If true, visible in the public marketplace. If false, only accessible via direct link.
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Unique URL-safe token for link sharing. Generated on creation.
    /// </summary>
    public string? ShareToken { get; set; }

    public int UsageCount { get; set; }

    /// <summary>
    /// Snapshot of custom variable definitions used by this template's effect.
    /// Null if no custom variables are referenced.
    /// </summary>
    public List<CustomVariableDefinition>? BundledVariables { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public AppUser? CreatedByUser { get; set; }
    public ICollection<Ruleset> Rulesets { get; set; } = new List<Ruleset>();
}
