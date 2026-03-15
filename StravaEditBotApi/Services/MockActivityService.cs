
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Models;

namespace StravaEditBotApi.Services;

public class MockActivityService : IActivityService
{
    // This is a mock service to hold a list of activities in memory. In a real application, this would interact with a database or external API.
    private readonly List<Activity> _activities =
    [
        new Activity(
            "Morning Run",
            "A nice run in the park",
            "Run",
            DateTime.Now.AddDays(-1),
            5.0,
            TimeSpan.FromMinutes(30)
        )
        {
            Id = 1,
        },
        new Activity(
            "Evening Ride",
            "A relaxing bike ride",
            "Ride",
            DateTime.Now.AddDays(-2),
            20.0,
            TimeSpan.FromMinutes(60)
        )
        {
            Id = 2,
        },
    ];

    public async Task<IEnumerable<Activity>> GetAllAsync() => await Task.FromResult(_activities);

    public async Task<Activity?> GetByIdAsync(int id) =>
        await Task.FromResult(_activities.FirstOrDefault(a => a.Id == id));

    public async Task<Activity> CreateAsync(CreateActivityDto dto)
    {
        int newId = _activities.Count > 0 ? _activities.Max(a => a.Id) + 1 : 1;

        var newActivity = new Activity(
            dto.Name,
            dto.Description,
            dto.ActivitySport,
            dto.StartTime,
            dto.Distance,
            dto.ElapsedTime
        )
        {
            Id = newId,
        };

        _activities.Add(newActivity);
        return await Task.FromResult(newActivity);
    }

    public async Task<bool> UpdateAsync(int id, CreateActivityDto dto)
    {
        var existing = await GetByIdAsync(id);
        if (existing is null)
        {
            return await Task.FromResult(false);
        }

        _activities.Remove(existing);
        _activities.Add(
            new Activity(
                dto.Name,
                dto.Description,
                dto.ActivitySport,
                dto.StartTime,
                dto.Distance,
                dto.ElapsedTime
            )
            {
                Id = id,
            }
        );

        return await Task.FromResult(true);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await GetByIdAsync(id);
        if (existing is null)
        {
            return await Task.FromResult(false);
        }

        _activities.Remove(existing);
        return await Task.FromResult(true);
    }
}
