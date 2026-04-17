using StravaEditBotApi.DTOs.Templates;

namespace StravaEditBotApi.Services.Rulesets;

public interface IRulesetTemplateService
{
    Task<List<RulesetTemplateResponseDto>> GetPublicTemplatesAsync(CancellationToken ct = default);
    Task<RulesetTemplateResponseDto?> GetByShareTokenAsync(string token, CancellationToken ct = default);
    Task<RulesetTemplateResponseDto?> InstantiateAsync(string userId, int templateId, CancellationToken ct = default);
}
