using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Controllers;

[ApiController]
[Route("api/webhook")]
public class WebhookController(
    IWebhookService webhookService,
    ChannelWriter<StravaWebhookEventDto> channelWriter,
    ILogger<WebhookController> logger
) : ControllerBase
{
    /// <summary>
    /// Handles the Strava subscription validation handshake.
    /// Strava sends a GET request with a challenge when a subscription is created.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Validate(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.challenge")] string challenge,
        [FromQuery(Name = "hub.verify_token")] string verifyToken)
    {
        if (!webhookService.ValidateVerifyToken(verifyToken))
        {
            logger.LogWarning("Webhook validation failed: invalid verify token");
            return Unauthorized();
        }

        logger.LogInformation("Webhook subscription validated successfully");
        return Ok(new Dictionary<string, string> { { "hub.challenge", challenge } });
    }

    /// <summary>
    /// Receives webhook events from Strava.
    /// Enqueues the event for background processing and returns 200 immediately.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public IActionResult Receive([FromBody] StravaWebhookEventDto evt)
    {
        if (!channelWriter.TryWrite(evt))
        {
            logger.LogWarning(
                "Failed to enqueue webhook event: {ObjectType}/{AspectType} for owner {OwnerId}",
                evt.ObjectType, evt.AspectType, evt.OwnerId);
        }

        return Ok();
    }
}
