using StravaEditBotApi.DTOs.Variables;

namespace StravaEditBotApi.Services.Rulesets;

public interface ICustomVariableService
{
    Task<List<CustomVariableResponseDto>> GetUserVariablesAsync(string userId, CancellationToken ct = default);
    Task<CustomVariableResponseDto?> GetByIdAsync(string userId, int variableId, CancellationToken ct = default);
    Task<(CustomVariableResponseDto? Result, string? Error)> CreateAsync(string userId, CreateCustomVariableDto dto, CancellationToken ct = default);
    Task<(CustomVariableResponseDto? Result, string? Error)> UpdateAsync(string userId, int variableId, UpdateCustomVariableDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(string userId, int variableId, CancellationToken ct = default);
}
