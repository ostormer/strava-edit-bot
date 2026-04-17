using Microsoft.EntityFrameworkCore;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs.Templates;
using StravaEditBotApi.Models;
using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.Services;

public class RulesetTemplateService(
    AppDbContext db,
    IRulesetValidator validator
) : IRulesetTemplateService
{
    public async Task<List<RulesetTemplateResponseDto>> GetPublicTemplatesAsync(CancellationToken ct = default)
    {
        List<RulesetTemplate> templates = await db.RulesetTemplates
            .Where(t => t.IsPublic)
            .OrderBy(t => t.UsageCount == 0)
            .ThenByDescending(t => t.UsageCount)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);

        return templates.Select(t => ToDto(t, null)).ToList();
    }

    public async Task<RulesetTemplateResponseDto?> GetByShareTokenAsync(string token, CancellationToken ct = default)
    {
        RulesetTemplate? template = await db.RulesetTemplates
            .SingleOrDefaultAsync(t => t.ShareToken == token, ct);

        if (template is null)
        {
            return null;
        }

        return ToDto(template, null);
    }

    public async Task<RulesetTemplateResponseDto?> InstantiateAsync(string userId, int templateId, CancellationToken ct = default)
    {
        RulesetTemplate? template = await db.RulesetTemplates
            .SingleOrDefaultAsync(t => t.Id == templateId, ct);

        if (template is null)
        {
            return null;
        }

        RulesetValidationResult validation = validator.Validate(template.Filter, template.Effect);

        int maxPriority = await db.Rulesets
            .Where(r => r.UserId == userId)
            .Select(r => (int?)r.Priority)
            .MaxAsync(ct) ?? -1;

        DateTime now = DateTime.UtcNow;

        var ruleset = new Ruleset
        {
            UserId = userId,
            Name = template.Name,
            Priority = maxPriority + 1,
            IsEnabled = true,
            IsValid = validation.IsValid,
            Filter = template.Filter,
            Effect = template.Effect,
            CreatedFromTemplateId = template.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Rulesets.Add(ruleset);

        // Create bundled custom variables if not already present
        if (template.BundledVariables is not null)
        {
            HashSet<string> existingNames = (await db.CustomVariables
                .Where(cv => cv.UserId == userId)
                .Select(cv => cv.Name)
                .ToListAsync(ct))
                .ToHashSet(StringComparer.Ordinal);

            foreach (CustomVariableDefinition bundled in template.BundledVariables)
            {
                if (!existingNames.Contains(bundled.Name))
                {
                    db.CustomVariables.Add(new CustomVariable
                    {
                        UserId = userId,
                        Name = bundled.Name,
                        Definition = bundled,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }
        }

        template.UsageCount++;
        await db.SaveChangesAsync(ct);

        return ToDto(template, null);
    }

    private static RulesetTemplateResponseDto ToDto(RulesetTemplate template, List<string>? sanitizedProperties)
    {
        return new RulesetTemplateResponseDto(
            template.Id,
            template.Name,
            template.Description,
            template.Filter,
            template.Effect,
            template.IsPublic,
            template.ShareToken,
            template.UsageCount,
            template.BundledVariables,
            template.CreatedAt,
            sanitizedProperties
        );
    }
}
