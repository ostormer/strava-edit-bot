using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.DTOs.Rulesets;

public record ValidateRulesetDto(
    FilterExpression? Filter,
    RulesetEffect? Effect
);
