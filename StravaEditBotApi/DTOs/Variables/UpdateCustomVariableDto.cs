using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.DTOs.Variables;

public record UpdateCustomVariableDto(
    string? Description,
    CustomVariableDefinition? Definition
);
