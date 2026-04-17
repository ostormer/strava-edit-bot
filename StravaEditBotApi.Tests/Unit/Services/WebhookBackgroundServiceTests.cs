using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Tests.Unit.Services;

[TestFixture]
public class WebhookBackgroundServiceTests
{
    private IServiceScopeFactory _scopeFactory = null!;
    private IWebhookService _webhookService = null!;

    // Exposes the protected ExecuteAsync for direct testing
    private sealed class TestableWebhookBackgroundService(
        ChannelReader<StravaWebhookEventDto> channelReader,
        IServiceScopeFactory scopeFactory,
        ILogger<WebhookBackgroundService> logger
    ) : WebhookBackgroundService(channelReader, scopeFactory, logger)
    {
        public new Task ExecuteAsync(CancellationToken ct) => base.ExecuteAsync(ct);
    }

    [SetUp]
    public void SetUp()
    {
        _webhookService = Substitute.For<IWebhookService>();
        _scopeFactory = Substitute.For<IServiceScopeFactory>();

        var scope = Substitute.For<IServiceScope>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        scope.ServiceProvider.Returns(serviceProvider);
        serviceProvider.GetService(typeof(IWebhookService)).Returns(_webhookService);
        _scopeFactory.CreateScope().Returns(scope);
    }

    private static StravaWebhookEventDto MakeEvent(
        string objectType = "activity",
        string aspectType = "create",
        long ownerId = 67890) =>
        new(
            ObjectType: objectType,
            ObjectId: 12345,
            AspectType: aspectType,
            Updates: new Dictionary<string, string>(),
            OwnerId: ownerId,
            SubscriptionId: 999,
            EventTime: 1234567890
        );

    private TestableWebhookBackgroundService CreateSut(ChannelReader<StravaWebhookEventDto> reader) =>
        new(reader, _scopeFactory, Substitute.For<ILogger<WebhookBackgroundService>>());

    // ========================================================
    // ExecuteAsync
    // ========================================================

    [Test]
    public async Task ExecuteAsync_EventInChannel_CallsProcessEventAsync()
    {
        var channel = Channel.CreateUnbounded<StravaWebhookEventDto>();
        var sut = CreateSut(channel.Reader);
        var evt = MakeEvent();

        channel.Writer.TryWrite(evt);
        channel.Writer.Complete();

        await sut.ExecuteAsync(CancellationToken.None);

        await _webhookService.Received(1).ProcessEventAsync(evt, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_MultipleEvents_ProcessesAll()
    {
        var channel = Channel.CreateUnbounded<StravaWebhookEventDto>();
        var sut = CreateSut(channel.Reader);

        channel.Writer.TryWrite(MakeEvent(ownerId: 1));
        channel.Writer.TryWrite(MakeEvent(ownerId: 2));
        channel.Writer.TryWrite(MakeEvent(ownerId: 3));
        channel.Writer.Complete();

        await sut.ExecuteAsync(CancellationToken.None);

        await _webhookService.Received(3).ProcessEventAsync(
            Arg.Any<StravaWebhookEventDto>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Test]
    public async Task ExecuteAsync_EmptyChannel_DoesNotCallProcessEventAsync()
    {
        var channel = Channel.CreateUnbounded<StravaWebhookEventDto>();
        var sut = CreateSut(channel.Reader);

        channel.Writer.Complete();

        await sut.ExecuteAsync(CancellationToken.None);

        await _webhookService.DidNotReceive().ProcessEventAsync(
            Arg.Any<StravaWebhookEventDto>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Test]
    public async Task ExecuteAsync_ProcessEventThrows_ContinuesProcessingNextEvent()
    {
        var channel = Channel.CreateUnbounded<StravaWebhookEventDto>();
        var sut = CreateSut(channel.Reader);
        var firstEvent = MakeEvent(ownerId: 1);
        var secondEvent = MakeEvent(ownerId: 2);

        _webhookService
            .ProcessEventAsync(firstEvent, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Processing failed")));

        channel.Writer.TryWrite(firstEvent);
        channel.Writer.TryWrite(secondEvent);
        channel.Writer.Complete();

        await sut.ExecuteAsync(CancellationToken.None);

        await _webhookService.Received(1).ProcessEventAsync(secondEvent, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_ProcessEventThrows_DoesNotThrowFromExecuteAsync()
    {
        var channel = Channel.CreateUnbounded<StravaWebhookEventDto>();
        var sut = CreateSut(channel.Reader);

        _webhookService
            .ProcessEventAsync(Arg.Any<StravaWebhookEventDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("boom")));

        channel.Writer.TryWrite(MakeEvent());
        channel.Writer.Complete();

        Assert.DoesNotThrowAsync(() => sut.ExecuteAsync(CancellationToken.None));
    }

    [Test]
    public async Task ExecuteAsync_CreatesNewScopeForEachEvent()
    {
        var channel = Channel.CreateUnbounded<StravaWebhookEventDto>();
        var sut = CreateSut(channel.Reader);

        channel.Writer.TryWrite(MakeEvent(ownerId: 1));
        channel.Writer.TryWrite(MakeEvent(ownerId: 2));
        channel.Writer.Complete();

        await sut.ExecuteAsync(CancellationToken.None);

        _scopeFactory.Received(2).CreateScope();
    }

    [Test]
    public async Task ExecuteAsync_CancellationBeforeEvents_DoesNotCallProcessEventAsync()
    {
        var channel = Channel.CreateUnbounded<StravaWebhookEventDto>();
        var sut = CreateSut(channel.Reader);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            await sut.ExecuteAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected: ReadAllAsync throws when the token is already cancelled
        }

        await _webhookService.DidNotReceive().ProcessEventAsync(
            Arg.Any<StravaWebhookEventDto>(),
            Arg.Any<CancellationToken>()
        );
    }
}
