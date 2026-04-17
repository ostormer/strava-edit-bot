using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs.Rulesets;
using StravaEditBotApi.DTOs.Templates;
using StravaEditBotApi.Models;
using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.Services;

public class RulesetService(
    AppDbContext db,
    IRulesetValidator validator,
    IFilterSanitizer sanitizer
) : IRulesetService
{
    public async Task<List<RulesetResponseDto>> GetUserRulesetsAsync(string userId, CancellationToken ct = default)
    {
        List<Ruleset> rulesets = await db.Rulesets
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.Priority)
            .ToListAsync(ct);

        return rulesets.Select(r => ToDto(r, [])).ToList();
    }

    public async Task<RulesetResponseDto?> GetByIdAsync(string userId, int rulesetId, CancellationToken ct = default)
    {
        Ruleset? ruleset = await db.Rulesets
            .SingleOrDefaultAsync(r => r.Id == rulesetId && r.UserId == userId, ct);

        if (ruleset is null)
        {
            return null;
        }

        RulesetValidationResult validation = validator.Validate(ruleset.Filter, ruleset.Effect);
        return ToDto(ruleset, validation.Errors);
    }

    public async Task<RulesetResponseDto> CreateAsync(string userId, CreateRulesetDto dto, CancellationToken ct = default)
    {
        int maxPriority = await db.Rulesets
            .Where(r => r.UserId == userId)
            .Select(r => (int?)r.Priority)
            .MaxAsync(ct) ?? -1;

        RulesetValidationResult validation = validator.Validate(dto.Filter, dto.Effect);
        DateTime now = DateTime.UtcNow;

        var ruleset = new Ruleset
        {
            UserId = userId,
            Name = dto.Name,
            Description = dto.Description,
            Priority = maxPriority + 1,
            IsEnabled = dto.IsEnabled,
            IsValid = validation.IsValid,
            Filter = dto.Filter,
            Effect = dto.Effect,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Rulesets.Add(ruleset);
        await db.SaveChangesAsync(ct);

        return ToDto(ruleset, validation.Errors);
    }

    public async Task<RulesetResponseDto?> UpdateAsync(string userId, int rulesetId, UpdateRulesetDto dto, CancellationToken ct = default)
    {
        Ruleset? ruleset = await db.Rulesets
            .SingleOrDefaultAsync(r => r.Id == rulesetId && r.UserId == userId, ct);

        if (ruleset is null)
        {
            return null;
        }

        if (dto.Name is not null)
        {
            ruleset.Name = dto.Name;
        }

        if (dto.Description is not null)
        {
            ruleset.Description = dto.Description;
        }

        // Allow explicitly setting filter/effect (including null to clear them)
        if (dto.Filter is not null || dto.Effect is not null)
        {
            ruleset.Filter = dto.Filter ?? ruleset.Filter;
            ruleset.Effect = dto.Effect ?? ruleset.Effect;
        }

        if (dto.IsEnabled.HasValue)
        {
            ruleset.IsEnabled = dto.IsEnabled.Value;
        }

        RulesetValidationResult validation = validator.Validate(ruleset.Filter, ruleset.Effect);
        ruleset.IsValid = validation.IsValid;
        ruleset.UpdatedAt = DateTime.UtcNow;

        // EF Core does not track mutations inside value-converted columns.
        db.Entry(ruleset).Property(r => r.Filter).IsModified = true;
        db.Entry(ruleset).Property(r => r.Effect).IsModified = true;

        await db.SaveChangesAsync(ct);

        return ToDto(ruleset, validation.Errors);
    }

    public async Task<bool> DeleteAsync(string userId, int rulesetId, CancellationToken ct = default)
    {
        Ruleset? ruleset = await db.Rulesets
            .SingleOrDefaultAsync(r => r.Id == rulesetId && r.UserId == userId, ct);

        if (ruleset is null)
        {
            return false;
        }

        // Null out RulesetRun.RulesetId before deletion.
        // The DB FK is NoAction (SQL Server can't have SET NULL here due to multiple cascade paths).
        await db.RulesetRuns
            .Where(r => r.RulesetId == rulesetId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.RulesetId, (int?)null), ct);

        db.Rulesets.Remove(ruleset);
        await db.SaveChangesAsync(ct);

        // Renumber remaining priorities to close the gap
        List<Ruleset> remaining = await db.Rulesets
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.Priority)
            .ToListAsync(ct);

        for (int i = 0; i < remaining.Count; i++)
        {
            remaining[i].Priority = i;
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<RulesetResponseDto>?> ReorderAsync(string userId, ReorderRulesetsDto dto, CancellationToken ct = default)
    {
        List<Ruleset> rulesets = await db.Rulesets
            .Where(r => r.UserId == userId)
            .ToListAsync(ct);

        // All provided IDs must belong to the user and match their total count
        if (dto.OrderedIds.Count != rulesets.Count ||
            dto.OrderedIds.Any(id => rulesets.All(r => r.Id != id)))
        {
            return null;
        }

        Dictionary<int, Ruleset> byId = rulesets.ToDictionary(r => r.Id);

        for (int i = 0; i < dto.OrderedIds.Count; i++)
        {
            byId[dto.OrderedIds[i]].Priority = i;
        }

        await db.SaveChangesAsync(ct);

        return rulesets
            .OrderBy(r => r.Priority)
            .Select(r => ToDto(r, []))
            .ToList();
    }

    public async Task<RulesetResponseDto?> ToggleEnabledAsync(string userId, int rulesetId, CancellationToken ct = default)
    {
        Ruleset? ruleset = await db.Rulesets
            .SingleOrDefaultAsync(r => r.Id == rulesetId && r.UserId == userId, ct);

        if (ruleset is null)
        {
            return null;
        }

        ruleset.IsEnabled = !ruleset.IsEnabled;
        ruleset.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return ToDto(ruleset, []);
    }

    public async Task<(RulesetTemplateResponseDto Template, List<string> SanitizedProperties)?> ShareAsync(
        string userId, int rulesetId, CreateTemplateFromRulesetDto dto, CancellationToken ct = default)
    {
        Ruleset? ruleset = await db.Rulesets
            .SingleOrDefaultAsync(r => r.Id == rulesetId && r.UserId == userId, ct);

        if (ruleset is null)
        {
            return null;
        }

        FilterExpression? sanitizedFilter = ruleset.Filter;
        List<string> sanitizedProperties = [];

        if (ruleset.Filter is not null)
        {
            (sanitizedFilter, sanitizedProperties) = sanitizer.SanitizeForSharing(ruleset.Filter);
        }

        string shareToken = GenerateShareToken();
        DateTime now = DateTime.UtcNow;

        var template = new RulesetTemplate
        {
            Name = dto.Name,
            Description = dto.Description,
            Filter = sanitizedFilter,
            Effect = ruleset.Effect,
            CreatedByUserId = userId,
            IsPublic = dto.IsPublic,
            ShareToken = shareToken,
            CreatedAt = now
        };

        db.RulesetTemplates.Add(template);
        await db.SaveChangesAsync(ct);

        return (ToTemplateDto(template, sanitizedProperties), sanitizedProperties);
    }

    private static string GenerateShareToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static RulesetResponseDto ToDto(Ruleset ruleset, List<RulesetValidationError> errors)
    {
        return new RulesetResponseDto(
            ruleset.Id,
            ruleset.Name,
            ruleset.Description,
            ruleset.Priority,
            ruleset.IsEnabled,
            ruleset.IsValid,
            ruleset.Filter,
            ruleset.Effect,
            ruleset.CreatedFromTemplateId,
            ruleset.CreatedAt,
            ruleset.UpdatedAt,
            errors
        );
    }

    private static RulesetTemplateResponseDto ToTemplateDto(RulesetTemplate template, List<string>? sanitizedProperties)
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
