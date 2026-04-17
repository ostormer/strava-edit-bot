namespace StravaEditBotApi.DTOs.Templates;

public record ShareRulesetResponseDto(
    RulesetTemplateResponseDto Template,
    List<string> SanitizedProperties
);
