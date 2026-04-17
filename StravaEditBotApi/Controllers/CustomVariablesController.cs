using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StravaEditBotApi.DTOs.Variables;
using StravaEditBotApi.Services.Auth;
using StravaEditBotApi.Services.Rulesets;
using StravaEditBotApi.Services.Webhook;

namespace StravaEditBotApi.Controllers;

[ApiController]
[Route("api/variables")]
[Authorize]
public class CustomVariablesController(ICustomVariableService variableService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetVariablesAsync(CancellationToken ct)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        List<CustomVariableResponseDto> variables = await variableService.GetUserVariablesAsync(userId, ct);
        return Ok(variables);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetVariableAsync(int id, CancellationToken ct)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        CustomVariableResponseDto? variable = await variableService.GetByIdAsync(userId, id, ct);
        if (variable is null)
        {
            return NotFound();
        }

        return Ok(variable);
    }

    [HttpPost]
    public async Task<IActionResult> CreateVariableAsync([FromBody] CreateCustomVariableDto dto, CancellationToken ct)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        (CustomVariableResponseDto? created, string? error) = await variableService.CreateAsync(userId, dto, ct);

        if (error is not null)
        {
            return Conflict(new { error });
        }

        return CreatedAtAction(nameof(GetVariableAsync), new { id = created!.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateVariableAsync(int id, [FromBody] UpdateCustomVariableDto dto, CancellationToken ct)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        (CustomVariableResponseDto? updated, string? error) = await variableService.UpdateAsync(userId, id, dto, ct);

        if (error is not null)
        {
            return Conflict(new { error });
        }

        if (updated is null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteVariableAsync(int id, CancellationToken ct)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        bool deleted = await variableService.DeleteAsync(userId, id, ct);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
