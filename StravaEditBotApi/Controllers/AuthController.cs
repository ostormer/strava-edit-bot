using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs.Auth;
using StravaEditBotApi.DTOs.Webhook;
using StravaEditBotApi.Models;
using StravaEditBotApi.Services.Auth;
using StravaEditBotApi.Services.Rulesets;
using StravaEditBotApi.Services.Webhook;

namespace StravaEditBotApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IStravaAuthService stravaAuthService,
    ITokenService tokenService,
    AppDbContext db,
    IWebHostEnvironment env
) : ControllerBase
{
    private static readonly TimeSpan _refreshTokenLifetime = TimeSpan.FromDays(7);
    private const string RefreshTokenCookieName = "refreshToken";

    [HttpPost("strava/callback")]
    public async Task<IActionResult> StravaCallbackAsync([FromBody] StravaCallbackDto dto)
    {
        StravaTokenData tokenData;
        try
        {
            tokenData = await stravaAuthService.ExchangeCodeAsync(dto.Code);
        }
        catch (Exception)
        {
            return BadRequest("Failed to exchange Strava authorization code.");
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.StravaAthleteId == tokenData.AthleteId);

        if (user is null)
        {
            user = new AppUser
            {
                UserName = tokenData.AthleteId.ToString(),
                StravaAthleteId = tokenData.AthleteId,
            };
            await db.Users.AddAsync(user);
        }

        user.StravaAccessToken = tokenData.AccessToken;
        user.StravaRefreshToken = tokenData.RefreshToken;
        user.StravaTokenExpiresAt = tokenData.ExpiresAt;
        user.StravaFirstname = tokenData.Firstname;
        user.StravaLastname = tokenData.Lastname;
        user.StravaProfileMedium = tokenData.ProfileMedium;
        user.StravaProfile = tokenData.Profile;
        await db.SaveChangesAsync();

        return await IssueTokensAsync(user);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshAsync()
    {
        string? rawToken = Request.Cookies[RefreshTokenCookieName];
        if (rawToken is null)
        {
            return Unauthorized();
        }

        string hash = tokenService.HashToken(rawToken);
        var stored = await db.RefreshTokens
            .Include(r => r.User)
            .SingleOrDefaultAsync(r => r.TokenHash == hash);

        if (stored is null || !stored.IsActive)
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
        if (rawToken is not null)
        {
            string hash = tokenService.HashToken(rawToken);
            var stored = await db.RefreshTokens.SingleOrDefaultAsync(r => r.TokenHash == hash);
            if (stored is not null)
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

        return Ok(new AuthResponseDto(
            accessToken,
            user.StravaFirstname ?? string.Empty,
            user.StravaLastname ?? string.Empty,
            user.StravaProfileMedium ?? string.Empty,
            user.StravaProfile ?? string.Empty
        ));
    }
}
