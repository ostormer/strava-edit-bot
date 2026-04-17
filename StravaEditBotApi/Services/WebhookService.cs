using Microsoft.EntityFrameworkCore;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Models;

namespace StravaEditBotApi.Services;

public class WebhookService(
    AppDbContext db,
    IConfiguration configuration,
    ILogger<WebhookService> logger
) : IWebhookService
{
    public bool ValidateVerifyToken(string token)
    {
        string? configuredToken = configuration["Strava:WebhookVerifyToken"];
        if (string.IsNullOrEmpty(configuredToken))
        {
            return false;
        }

        return string.Equals(token, configuredToken, StringComparison.Ordinal);
    }

    public async Task ProcessEventAsync(StravaWebhookEventDto evt, CancellationToken ct)
    {
        switch (evt.ObjectType, evt.AspectType)
        {
            case ("activity", "create"):
                await HandleActivityCreateAsync(evt, ct);
                break;

            case ("athlete", "update"):
                await HandleAthleteUpdateAsync(evt, ct);
                break;

            default:
                logger.LogDebug(
                    "Ignoring webhook event: {ObjectType}/{AspectType} for owner {OwnerId}",
                    evt.ObjectType, evt.AspectType, evt.OwnerId);
                break;
        }
    }

    private async Task HandleActivityCreateAsync(StravaWebhookEventDto evt, CancellationToken ct)
    {
        var user = await db.Users
            .SingleOrDefaultAsync(u => u.StravaAthleteId == evt.OwnerId, ct);

        if (user is null)
        {
            logger.LogWarning(
                "Received activity create event for unknown Strava athlete {AthleteId}",
                evt.OwnerId);
            return;
        }

        logger.LogInformation(
            "Activity {ActivityId} created by athlete {AthleteId} (user {UserId})",
            evt.ObjectId, evt.OwnerId, user.Id);

        // TODO: refresh Strava token and dispatch edit job
    }

    private async Task HandleAthleteUpdateAsync(StravaWebhookEventDto evt, CancellationToken ct)
    {
        if (!evt.Updates.TryGetValue("authorized", out string? authorized) || authorized != "false")
        {
            logger.LogDebug(
                "Ignoring athlete update event for owner {OwnerId} (not a deauthorization)",
                evt.OwnerId);
            return;
        }

        var user = await db.Users
            .SingleOrDefaultAsync(u => u.StravaAthleteId == evt.OwnerId, ct);

        if (user is null)
        {
            logger.LogWarning(
                "Received deauthorization event for unknown Strava athlete {AthleteId}",
                evt.OwnerId);
            return;
        }

        // Clear all Strava data except StravaAthleteId — that's the link
        // that lets re-authorization find the same AppUser instead of
        // creating a duplicate account.
        user.StravaAccessToken = null;
        user.StravaRefreshToken = null;
        user.StravaTokenExpiresAt = null;
        user.StravaFirstname = null;
        user.StravaLastname = null;
        user.StravaProfileMedium = null;
        user.StravaProfile = null;

        // Revoke all active app refresh tokens so the user is forced to
        // log in again. Strava is the only identity provider, so there is
        // no session without Strava authorization.
        DateTime now = DateTime.UtcNow;
        var activeTokens = await db.RefreshTokens
            .Where(r => r.UserId == user.Id && r.RevokedAt == null && r.ExpiresAt > now)
            .ToListAsync(ct);

        foreach (RefreshToken token in activeTokens)
        {
            token.RevokedAt = now;
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Deauthorized athlete {AthleteId} (user {UserId}): cleared Strava data, revoked {TokenCount} refresh token(s)",
            evt.OwnerId, user.Id, activeTokens.Count);
    }
}
