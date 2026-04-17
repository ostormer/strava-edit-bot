using StravaEditBotApi.DTOs.Auth;
using StravaEditBotApi.DTOs.Webhook;

namespace StravaEditBotApi.Services.Webhook;

public interface IWebhookService
{
    bool ValidateVerifyToken(string token);
    Task ProcessEventAsync(StravaWebhookEventDto evt, CancellationToken ct);
}
