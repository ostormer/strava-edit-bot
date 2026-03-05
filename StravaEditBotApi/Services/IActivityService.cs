using StravaEditBotApi.DTOs;
using StravaEditBotApi.Models;

namespace StravaEditBotApi.Services;


public interface IActivityService
{
    Task<IEnumerable<Activity>> GetAllAsync();
    Task<Activity?> GetByIdAsync(int id);
    Task<Activity> CreateAsync(CreateActivityDto dto);
    Task<bool> UpdateAsync(int id, CreateActivityDto dto);
    Task<bool> DeleteAsync(int id);
}