using System.Text.Json;
using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.DTOs.Rulesets;

public record CreateRulesetDto(
    string Name,
    string? Description,
    FilterExpression? Filter,
    RulesetEffect? Effect,
    bool IsEnabled
);
