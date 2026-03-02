namespace StravaEditBotApi.Services;

using StravaEditBotApi.Models;
using StravaEditBotApi.DTOs;

public class ActivityService : IActivityService
{
    // This is a mock service to hold a list of activities in memory. In a real application, this would interact with a database or external API.
    private readonly List<Activity> _activities =
    [
        new Activity(1, "Morning Run", "A nice run in the park", "Run", DateTime.Now.AddDays(-1), 5.0, TimeSpan.FromMinutes(30)),
        new Activity(2, "Evening Ride", "A relaxing bike ride", "Ride", DateTime.Now.AddDays(-2), 20.0, TimeSpan.FromMinutes(60))
    ];

    public async Task<IEnumerable<Activity>> GetAllAsync() => await Task.FromResult(_activities);
    
    public async Task<Activity?> GetByIdAsync(int id) =>
        await Task.FromResult(_activities.FirstOrDefault(a => a.Id == id));

    public async Task<Activity> CreateAsync(CreateActivityDto dto)
    {
        var newId = _activities.Count > 0 ? _activities.Max(a => a.Id) + 1 : 1;

        var newActivity = new Activity(
            newId,
            dto.Name,
            dto.Description,
            dto.ActivitySport,
            dto.StartTime,
            dto.Distance,
            dto.ElapsedTime
        );

        _activities.Add(newActivity);
        return await Task.FromResult(newActivity);
    }

    public async Task<bool> UpdateAsync(int id, CreateActivityDto dto)
    {
        var existing = await GetByIdAsync(id);
        if (existing is null) return await Task.FromResult(false);

        _activities.Remove(existing);
        _activities.Add(new Activity(
            id,
            dto.Name,
            dto.Description,
            dto.ActivitySport,
            dto.StartTime,
            dto.Distance,
            dto.ElapsedTime
        ));
        return await Task.FromResult(true);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await GetByIdAsync(id);
        if (existing is null) return await Task.FromResult(false);
        
        _activities.Remove(existing);
        return await Task.FromResult(true);
    }
}