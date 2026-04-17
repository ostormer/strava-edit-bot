using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StravaEditBotApi.DTOs.Templates;
using StravaEditBotApi.Services.Auth;
using StravaEditBotApi.Services.Rulesets;
using StravaEditBotApi.Services.Webhook;

namespace StravaEditBotApi.Controllers;

[ApiController]
[Route("api/templates")]
public class RulesetTemplatesController(IRulesetTemplateService templateService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPublicTemplatesAsync(CancellationToken ct)
    {
        List<RulesetTemplateResponseDto> templates = await templateService.GetPublicTemplatesAsync(ct);
        return Ok(templates);
    }

    [HttpGet("shared/{shareToken}")]
    public async Task<IActionResult> GetByShareTokenAsync(string shareToken, CancellationToken ct)
    {
        RulesetTemplateResponseDto? template = await templateService.GetByShareTokenAsync(shareToken, ct);
        if (template is null)
        {
            return NotFound();
        }

        return Ok(template);
    }

    [HttpPost("{id:int}/use")]
    [Authorize]
    public async Task<IActionResult> InstantiateTemplateAsync(int id, CancellationToken ct)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        RulesetTemplateResponseDto? result = await templateService.InstantiateAsync(userId, id, ct);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}
