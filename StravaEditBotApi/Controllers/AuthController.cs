using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Models;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<AppUser> userManager,
    ITokenService tokenService,
    AppDbContext db,
    IWebHostEnvironment env
) : ControllerBase
{
    private static readonly TimeSpan _refreshTokenLifetime = TimeSpan.FromDays(7);
    private const string RefreshTokenCookieName = "refreshToken";

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterDto dto)
    {
        var user = new AppUser { UserName = dto.Email, Email = dto.Email };
        var result = await userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }
        return await IssueTokensAsync(user);
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user == null || !await userManager.CheckPasswordAsync(user, dto.Password))
        {
            return Unauthorized();
        }

        return await IssueTokensAsync(user);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshAsync()
    {
        string? rawToken = Request.Cookies[RefreshTokenCookieName];
        if (rawToken == null)
        {
            return Unauthorized();
        }

        string hash = tokenService.HashToken(rawToken);
        var stored = await db.RefreshTokens
            .Include(r => r.User)
            .SingleOrDefaultAsync(r => r.TokenHash == hash);

        if (stored == null || !stored.IsActive)
        {
            return Unauthorized();
        }

        // Revoke the old token before issuing a new one (rotation).

        stored.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await IssueTokensAsync(stored.User);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync()
    {
        string? rawToken = Request.Cookies[RefreshTokenCookieName];
        if (rawToken != null)
        {
            string hash = tokenService.HashToken(rawToken);
            var stored = await db.RefreshTokens.SingleOrDefaultAsync(r => r.TokenHash == hash);
            if (stored != null)
            {
                stored.RevokedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
        }

        Response.Cookies.Delete(RefreshTokenCookieName);
        return Ok();
    }

    private async Task<IActionResult> IssueTokensAsync(AppUser user)
    {
        string accessToken = tokenService.GenerateAccessToken(user);
        string rawRefresh = tokenService.GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenService.HashToken(rawRefresh),
            ExpiresAt = DateTime.UtcNow.Add(_refreshTokenLifetime),
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        Response.Cookies.Append(RefreshTokenCookieName, rawRefresh, new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsDevelopment(), // HTTP in dev, HTTPS in prod
            SameSite = SameSiteMode.Strict,
            MaxAge = _refreshTokenLifetime,
        });

        return Ok(new AuthResponseDto(accessToken));
    }
}
