namespace StravaEditBotApi.DTOs.Templates;

public record CreateTemplateFromRulesetDto(
    string Name,
    string? Description,
    bool IsPublic
);
