using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.DTOs.Rulesets;

public record UpdateRulesetDto(
    string? Name,
    string? Description,
    FilterExpression? Filter,
    RulesetEffect? Effect,
    bool? IsEnabled,
    bool ClearFilter = false,
    bool ClearEffect = false
);
