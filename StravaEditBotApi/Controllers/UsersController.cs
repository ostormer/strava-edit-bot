using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Models;

namespace StravaEditBotApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(UserManager<AppUser> userManager) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUserAsync()
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        AppUser? user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(new UserDto(
            user.StravaFirstname ?? string.Empty,
            user.StravaLastname ?? string.Empty,
            user.StravaProfileMedium ?? string.Empty,
            user.StravaProfile ?? string.Empty
        ));
    }
}
