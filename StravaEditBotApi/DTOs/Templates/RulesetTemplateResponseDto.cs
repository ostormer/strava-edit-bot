using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.DTOs.Templates;

public record RulesetTemplateResponseDto(
    int Id,
    string Name,
    string? Description,
    FilterExpression? Filter,
    RulesetEffect? Effect,
    bool IsPublic,
    string? ShareToken,
    int UsageCount,
    List<CustomVariableDefinition>? BundledVariables,
    DateTime CreatedAt,

    /// <summary>
    /// Properties that had their values removed during sanitization.
    /// Populated only on the share creation response.
    /// </summary>
    List<string>? SanitizedProperties
);
