using System.Threading.Channels;
using StravaEditBotApi.DTOs.Auth;
using StravaEditBotApi.DTOs.Webhook;

namespace StravaEditBotApi.Services.Webhook;

public class WebhookBackgroundService(
    ChannelReader<StravaWebhookEventDto> channelReader,
    IServiceScopeFactory scopeFactory,
    ILogger<WebhookBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Webhook background service started");

        await foreach (StravaWebhookEventDto evt in channelReader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();
                await webhookService.ProcessEventAsync(evt, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to process webhook event: {ObjectType}/{AspectType} for owner {OwnerId}",
                    evt.ObjectType, evt.AspectType, evt.OwnerId);
            }
        }
    }
}
