using StravaEditBotApi.DTOs;

namespace StravaEditBotApi.Services;

public interface IWebhookService
{
    bool ValidateVerifyToken(string token);
    Task ProcessEventAsync(StravaWebhookEventDto evt, CancellationToken ct);
}
