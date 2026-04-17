using Microsoft.EntityFrameworkCore;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs.Variables;
using StravaEditBotApi.Models;
using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.Services;

public class CustomVariableService(AppDbContext db) : ICustomVariableService
{
    private const int MaxVariablesPerUser = 50;

    // Reserved built-in variable names that users cannot override
    private static readonly HashSet<string> _builtInVariables =
    [
        "original_name", "sport_type", "gear_id", "workout_type",
        "distance_km", "distance_mi", "distance_m",
        "elapsed_time_human", "moving_time_human", "stopped_time_human",
        "elapsed_time_minutes", "moving_time_minutes",
        "elevation_gain_m", "elevation_gain_ft", "elev_high_m", "elevation_per_km",
        "average_speed_kmh", "average_speed_mph", "max_speed_kmh",
        "average_pace_min_km", "average_pace_min_mi",
        "average_watts", "calories", "kilojoules",
        "start_time", "start_date", "day_of_week", "month_name", "timezone",
        "athlete_count"
    ];

    public async Task<List<CustomVariableResponseDto>> GetUserVariablesAsync(string userId, CancellationToken ct = default)
    {
        List<CustomVariable> variables = await db.CustomVariables
            .Where(cv => cv.UserId == userId)
            .OrderBy(cv => cv.Name)
            .ToListAsync(ct);

        return variables.Select(ToDto).ToList();
    }

    public async Task<CustomVariableResponseDto?> GetByIdAsync(string userId, int variableId, CancellationToken ct = default)
    {
        CustomVariable? variable = await db.CustomVariables
            .SingleOrDefaultAsync(cv => cv.Id == variableId && cv.UserId == userId, ct);

        return variable is null ? null : ToDto(variable);
    }

    public async Task<(CustomVariableResponseDto? Result, string? Error)> CreateAsync(
        string userId, CreateCustomVariableDto dto, CancellationToken ct = default)
    {
        if (_builtInVariables.Contains(dto.Name))
        {
            return (null, $"'{dto.Name}' is a reserved built-in variable name.");
        }

        int existingCount = await db.CustomVariables
            .CountAsync(cv => cv.UserId == userId, ct);

        if (existingCount >= MaxVariablesPerUser)
        {
            return (null, $"You have reached the limit of {MaxVariablesPerUser} custom variables.");
        }

        bool nameExists = await db.CustomVariables
            .AnyAsync(cv => cv.UserId == userId && cv.Name == dto.Name, ct);

        if (nameExists)
        {
            return (null, $"A custom variable named '{dto.Name}' already exists.");
        }

        DateTime now = DateTime.UtcNow;

        // Ensure the name embedded in the definition matches the entity name
        CustomVariableDefinition definition = dto.Definition with { Name = dto.Name };

        var variable = new CustomVariable
        {
            UserId = userId,
            Name = dto.Name,
            Description = dto.Description,
            Definition = definition,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.CustomVariables.Add(variable);
        await db.SaveChangesAsync(ct);

        return (ToDto(variable), null);
    }

    public async Task<(CustomVariableResponseDto? Result, string? Error)> UpdateAsync(
        string userId, int variableId, UpdateCustomVariableDto dto, CancellationToken ct = default)
    {
        CustomVariable? variable = await db.CustomVariables
            .SingleOrDefaultAsync(cv => cv.Id == variableId && cv.UserId == userId, ct);

        if (variable is null)
        {
            return (null, null);
        }

        if (dto.Description is not null)
        {
            variable.Description = dto.Description;
        }

        if (dto.Definition is not null)
        {
            variable.Definition = dto.Definition;
            db.Entry(variable).Property(cv => cv.Definition).IsModified = true;
        }

        variable.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return (ToDto(variable), null);
    }

    public async Task<bool> DeleteAsync(string userId, int variableId, CancellationToken ct = default)
    {
        CustomVariable? variable = await db.CustomVariables
            .SingleOrDefaultAsync(cv => cv.Id == variableId && cv.UserId == userId, ct);

        if (variable is null)
        {
            return false;
        }

        db.CustomVariables.Remove(variable);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static CustomVariableResponseDto ToDto(CustomVariable variable)
    {
        return new CustomVariableResponseDto(
            variable.Id,
            variable.Name,
            variable.Description,
            variable.Definition,
            variable.CreatedAt,
            variable.UpdatedAt
        );
    }
}
