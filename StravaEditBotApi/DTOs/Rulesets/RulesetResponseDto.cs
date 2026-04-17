using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.DTOs.Rulesets;

public record RulesetResponseDto(
    int Id,
    string Name,
    string? Description,
    int Priority,
    bool IsEnabled,
    bool IsValid,
    FilterExpression? Filter,
    RulesetEffect? Effect,
    int? CreatedFromTemplateId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<RulesetValidationError> ValidationErrors
);
