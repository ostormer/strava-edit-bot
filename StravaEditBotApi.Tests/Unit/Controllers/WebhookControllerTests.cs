using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StravaEditBotApi.Controllers;
using StravaEditBotApi.DTOs.Auth;
using StravaEditBotApi.DTOs.Webhook;
using StravaEditBotApi.Services.Auth;
using StravaEditBotApi.Services.Rulesets;
using StravaEditBotApi.Services.Webhook;

namespace StravaEditBotApi.Tests.Unit.Controllers;

[TestFixture]
public class WebhookControllerTests
{
    private IWebhookService _webhookService = null!;
    private ChannelWriter<StravaWebhookEventDto> _channelWriter = null!;
    private WebhookController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _webhookService = Substitute.For<IWebhookService>();
        _channelWriter = Substitute.For<ChannelWriter<StravaWebhookEventDto>>();
        _sut = new WebhookController(
            _webhookService,
            _channelWriter,
            Substitute.For<ILogger<WebhookController>>()
        );
    }

    private static StravaWebhookEventDto MakeEvent(
        string objectType = "activity",
        string aspectType = "create",
        long ownerId = 67890,
        Dictionary<string, string>? updates = null) =>
        new(
            ObjectType: objectType,
            ObjectId: 12345,
            AspectType: aspectType,
            Updates: updates ?? new Dictionary<string, string>(),
            OwnerId: ownerId,
            SubscriptionId: 999,
            EventTime: 1234567890
        );

    // ========================================================
    // Validate
    // ========================================================

    [Test]
    public void Validate_ValidToken_ReturnsOkWithChallenge()
    {
        _webhookService.ValidateVerifyToken("my-token").Returns(true);

        var result = _sut.Validate("subscribe", "abc123", "my-token");

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        var body = ok.Value as Dictionary<string, string>;
        Assert.That(body, Is.Not.Null);
        Assert.That(body!["hub.challenge"], Is.EqualTo("abc123"));
    }

    [Test]
    public void Validate_InvalidToken_ReturnsUnauthorized()
    {
        _webhookService.ValidateVerifyToken(Arg.Any<string>()).Returns(false);

        var result = _sut.Validate("subscribe", "abc123", "wrong-token");

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public void Validate_ValidToken_ReturnsChallengeFromQueryParam()
    {
        _webhookService.ValidateVerifyToken(Arg.Any<string>()).Returns(true);

        var result = _sut.Validate("subscribe", "unique-challenge-xyz", "token");

        var ok = (OkObjectResult)result;
        var body = (Dictionary<string, string>)ok.Value!;
        Assert.That(body["hub.challenge"], Is.EqualTo("unique-challenge-xyz"));
    }

    [Test]
    public void Validate_CallsValidateVerifyTokenWithProvidedToken()
    {
        _webhookService.ValidateVerifyToken("specific-token").Returns(true);

        _sut.Validate("subscribe", "challenge", "specific-token");

        _webhookService.Received(1).ValidateVerifyToken("specific-token");
    }

    // ========================================================
    // Receive
    // ========================================================

    [Test]
    public void Receive_EventSuccessfullyEnqueued_ReturnsOk()
    {
        _channelWriter.TryWrite(Arg.Any<StravaWebhookEventDto>()).Returns(true);

        var result = _sut.Receive(MakeEvent());

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public void Receive_EnqueueFails_StillReturnsOk()
    {
        _channelWriter.TryWrite(Arg.Any<StravaWebhookEventDto>()).Returns(false);

        var result = _sut.Receive(MakeEvent());

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public void Receive_CallsTryWriteWithProvidedEvent()
    {
        var evt = MakeEvent(objectType: "activity", aspectType: "create", ownerId: 42);
        _channelWriter.TryWrite(Arg.Any<StravaWebhookEventDto>()).Returns(true);

        _sut.Receive(evt);

        _channelWriter.Received(1).TryWrite(evt);
    }

    [Test]
    public void Receive_CallsTryWriteExactlyOnce()
    {
        _channelWriter.TryWrite(Arg.Any<StravaWebhookEventDto>()).Returns(true);

        _sut.Receive(MakeEvent());

        _channelWriter.Received(1).TryWrite(Arg.Any<StravaWebhookEventDto>());
    }
}
