using Microsoft.AspNetCore.Identity;

namespace StravaEditBotApi.Models;

public class AppUser : IdentityUser
{
    public long? StravaAthleteId { get; set; }
    public string? StravaAccessToken { get; set; }
    public string? StravaRefreshToken { get; set; }
    public DateTime? StravaTokenExpiresAt { get; set; }
    public string? StravaFirstname { get; set; }
    public string? StravaLastname { get; set; }
    public string? StravaProfileMedium { get; set; }
    public string? StravaProfile { get; set; }

    // Navigation properties
    public ICollection<Ruleset> Rulesets { get; set; } = new List<Ruleset>();
    public ICollection<RulesetRun> RulesetRuns { get; set; } = new List<RulesetRun>();
    public ICollection<RulesetTemplate> RulesetTemplates { get; set; } = new List<RulesetTemplate>();
    public ICollection<CustomVariable> CustomVariables { get; set; } = new List<CustomVariable>();
}
