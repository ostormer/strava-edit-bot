using StravaEditBotApi.DTOs.Rulesets;
using StravaEditBotApi.DTOs.Templates;

namespace StravaEditBotApi.Services.Rulesets;

public interface IRulesetService
{
    Task<List<RulesetResponseDto>> GetUserRulesetsAsync(string userId, CancellationToken ct = default);
    Task<RulesetResponseDto?> GetByIdAsync(string userId, int rulesetId, CancellationToken ct = default);
    Task<RulesetResponseDto> CreateAsync(string userId, CreateRulesetDto dto, CancellationToken ct = default);
    Task<RulesetResponseDto?> UpdateAsync(string userId, int rulesetId, UpdateRulesetDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(string userId, int rulesetId, CancellationToken ct = default);
    Task<List<RulesetResponseDto>?> ReorderAsync(string userId, ReorderRulesetsDto dto, CancellationToken ct = default);
    Task<RulesetResponseDto?> ToggleEnabledAsync(string userId, int rulesetId, CancellationToken ct = default);
    Task<(RulesetTemplateResponseDto Template, List<string> SanitizedProperties)?> ShareAsync(
        string userId, int rulesetId, CreateTemplateFromRulesetDto dto, CancellationToken ct = default);
}
