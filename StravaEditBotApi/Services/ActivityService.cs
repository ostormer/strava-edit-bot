
using Microsoft.EntityFrameworkCore;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Models;

namespace StravaEditBotApi.Services;

public class ActivityService(AppDbContext context) : IActivityService
{
    private readonly AppDbContext _context = context;

    public async Task<IEnumerable<Activity>> GetAllAsync() =>
        await _context.Activities.ToListAsync();

    public async Task<Activity?> GetByIdAsync(int id) => await _context.Activities.FindAsync(id);

    public async Task<Activity> CreateAsync(CreateActivityDto dto)
    {
        var Activity = new Activity(
            dto.Name,
            dto.Description,
            dto.ActivitySport,
            dto.StartTime,
            dto.Distance,
            dto.ElapsedTime
        );

        _context.Activities.Add(Activity);
        await _context.SaveChangesAsync();
        return Activity;
    }

    public async Task<bool> UpdateAsync(int id, UpdateActivityDto dto)
    {
        var existing = await _context.Activities.FindAsync(id);
        if (existing is null)
        {
            return false;
        }

        if (dto.Name is not null)
        {
            existing.Name = dto.Name;
        }
        if (dto.Description is not null)
        {
            existing.Description = dto.Description;
        }
        if (dto.ActivitySport is not null)
        {
            existing.ActivitySport = dto.ActivitySport;
        }
        if (dto.StartTime.HasValue)
        {
            existing.StartTime = dto.StartTime.Value;
        }
        if (dto.Distance.HasValue)
        {
            existing.Distance = dto.Distance.Value;
        }
        if (dto.ElapsedTime.HasValue)
        {
            existing.ElapsedTime = dto.ElapsedTime.Value;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _context.Activities.FindAsync(id);
        if (existing is null)
        {
            return false;
        }

        _context.Activities.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
}
