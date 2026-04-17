using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.DTOs.Variables;

public record CustomVariableResponseDto(
    int Id,
    string Name,
    string? Description,
    CustomVariableDefinition Definition,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
