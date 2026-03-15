using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Controllers;

[ApiController]
[Route("api/activities")]
public class ActivitiesController(
    IActivityService activityService,
    ILogger<ActivitiesController> logger
) : ControllerBase
{
    private readonly IActivityService _activityService = activityService;
    private readonly ILogger<ActivitiesController> _logger = logger;

    [HttpGet("test-error")]
    public IActionResult TestError()
    {
        throw new InvalidOperationException("This is a test exception");
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        var activities = await _activityService.GetAllAsync();
        return Ok(activities);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(int id)
    {
        var activity = await _activityService.GetByIdAsync(id);
        if (activity == null)
        {
            return NotFound($"Activity with ID {id} not found.");
        }
        return Ok(activity);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateActivityDto dto)
    {
        var created = await _activityService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutAsync(int id, [FromBody] UpdateActivityDto dto)
    {
        bool updated = await _activityService.UpdateAsync(id, dto);
        if (!updated)
        {
            return NotFound($"Activity with ID {id} not found.");
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        bool deleted = await _activityService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound($"Activity with ID {id} not found.");
        }

        return NoContent();
    }
}
