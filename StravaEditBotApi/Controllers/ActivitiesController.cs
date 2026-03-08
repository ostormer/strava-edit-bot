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
    public async Task<IActionResult> GetAll()
    {
        var activities = await _activityService.GetAllAsync();
        return Ok(activities);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var activity = await _activityService.GetByIdAsync(id);
        if (activity == null)
        {
            return NotFound($"Activity with ID {id} not found.");
        }
        return Ok(activity);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateActivityDto dto)
    {
        var created = await _activityService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, [FromBody] CreateActivityDto dto)
    {
        var updated = await _activityService.UpdateAsync(id, dto);
        if (!updated)
            return NotFound($"Activity with ID {id} not found.");

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _activityService.DeleteAsync(id);
        if (!deleted)
            return NotFound($"Activity with ID {id} not found.");

        return NoContent();
    }
}
