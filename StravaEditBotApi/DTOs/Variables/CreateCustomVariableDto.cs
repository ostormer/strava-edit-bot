using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.DTOs.Variables;

public record CreateCustomVariableDto(
    string Name,
    string? Description,
    CustomVariableDefinition Definition
);
