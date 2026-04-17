using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StravaEditBotApi.DTOs.Rulesets;
using StravaEditBotApi.DTOs.Templates;
using StravaEditBotApi.Models.Rules;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Controllers;

[ApiController]
[Route("api/rulesets")]
[Authorize]
public class RulesetsController(
    IRulesetService rulesetService,
    IRulesetValidator validator
) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRulesetsAsync(CancellationToken ct)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        List<RulesetResponseDto> rulesets = await rulesetService.GetUserRulesetsAsync(userId, ct);
        return Ok(rulesets);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRulesetAsync(int id, CancellationToken ct)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        RulesetResponseDto? ruleset = await rulesetService.GetByIdAsync(userId, id, ct);
        if (ruleset is null)
        {
            return NotFound();
        }

        return Ok(ruleset);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRulesetAsync([FromBody] CreateRulesetDto dto, CancellationToken ct)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        RulesetResponseDto created = await rulesetService.CreateAsync(userId, dto, ct);
        return CreatedAtAction(nameof(GetRulesetAsync), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRulesetAsync(int id, [FromBody] UpdateRulesetDto dto, CancellationToken ct)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        RulesetResponseDto? updated = await rulesetService.UpdateAsync(userId, id, dto, ct);
        if (updated is null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRulesetAsync(int id, CancellationToken ct)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        bool deleted = await rulesetService.DeleteAsync(userId, id, ct);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderRulesetsAsync([FromBody] ReorderRulesetsDto dto, CancellationToken ct)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        List<RulesetResponseDto>? reordered = await rulesetService.ReorderAsync(userId, dto, ct);
        if (reordered is null)
        {
            return BadRequest("OrderedIds must contain exactly all of the user's ruleset IDs.");
        }

        return Ok(reordered);
    }

    [HttpPatch("{id:int}/toggle")]
    public async Task<IActionResult> ToggleEnabledAsync(int id, CancellationToken ct)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        RulesetResponseDto? toggled = await rulesetService.ToggleEnabledAsync(userId, id, ct);
        if (toggled is null)
        {
            return NotFound();
        }

        return Ok(toggled);
    }

    [HttpPost("{id:int}/share")]
    public async Task<IActionResult> ShareRulesetAsync(int id, [FromBody] CreateTemplateFromRulesetDto dto, CancellationToken ct)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await rulesetService.ShareAsync(userId, id, dto, ct);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result.Value.Template);
    }

    [HttpPost("validate")]
    public IActionResult ValidateRuleset([FromBody] ValidateRulesetDto dto)
    {
        RulesetValidationResult result = validator.Validate(dto.Filter, dto.Effect);
        return Ok(result);
    }
}
