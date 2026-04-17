using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs.Runs;
using StravaEditBotApi.Models;

namespace StravaEditBotApi.Controllers;

[ApiController]
[Route("api/runs")]
[Authorize]
public class RulesetRunsController(AppDbContext db) : ControllerBase
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    [HttpGet]
    public async Task<IActionResult> GetRunsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken ct = default)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        int clampedSize = Math.Clamp(pageSize, 1, MaxPageSize);

        List<RulesetRun> runs = await db.RulesetRuns
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ProcessedAt)
            .Skip((page - 1) * clampedSize)
            .Take(clampedSize)
            .ToListAsync(ct);

        return Ok(runs.Select(ToDto));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetRunAsync(long id, CancellationToken ct)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        RulesetRun? run = await db.RulesetRuns
            .SingleOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct);

        if (run is null)
        {
            return NotFound();
        }

        return Ok(ToDto(run));
    }

    private static RulesetRunResponseDto ToDto(RulesetRun run)
    {
        return new RulesetRunResponseDto(
            run.Id,
            run.StravaActivityId,
            run.RulesetId,
            run.RulesetName,
            run.Status,
            run.ErrorMessage,
            run.FieldsChanged,
            run.ProcessedAt,
            run.StravaEventTime
        );
    }
}
