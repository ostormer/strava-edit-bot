using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs.Auth;
using StravaEditBotApi.DTOs.Webhook;
using StravaEditBotApi.Models;
using StravaEditBotApi.Services.Auth;
using StravaEditBotApi.Services.Rulesets;
using StravaEditBotApi.Services.Webhook;

namespace StravaEditBotApi.Tests.Unit.Services;

[TestFixture]
public class WebhookServiceTests
{
    private AppDbContext _context = null!;
    private IConfiguration _config = null!;
    private WebhookService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _config = Substitute.For<IConfiguration>();
        _sut = new WebhookService(_context, _config, Substitute.For<ILogger<WebhookService>>());
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private static StravaWebhookEventDto MakeEvent(
        string objectType = "activity",
        string aspectType = "create",
        long ownerId = 67890,
        long objectId = 12345,
        Dictionary<string, string>? updates = null) =>
        new(
            ObjectType: objectType,
            ObjectId: objectId,
            AspectType: aspectType,
            Updates: updates ?? new Dictionary<string, string>(),
            OwnerId: ownerId,
            SubscriptionId: 999,
            EventTime: 1234567890
        );

    private async Task<AppUser> SeedUserAsync(long stravaAthleteId = 67890)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"user-{stravaAthleteId}",
            StravaAthleteId = stravaAthleteId,
            StravaAccessToken = "access-token",
            StravaRefreshToken = "refresh-token",
            StravaTokenExpiresAt = DateTime.UtcNow.AddHours(6),
            StravaFirstname = "John",
            StravaLastname = "Doe",
            StravaProfileMedium = "https://example.com/medium.jpg",
            StravaProfile = "https://example.com/profile.jpg"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<RefreshToken> SeedRefreshTokenAsync(
        string userId,
        bool expired = false,
        bool revoked = false)
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Guid.NewGuid().ToString(),
            ExpiresAt = expired ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = revoked ? DateTime.UtcNow.AddDays(-1) : null
        };
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();
        return token;
    }

    // ========================================================
    // ValidateVerifyToken
    // ========================================================

    [Test]
    public void ValidateVerifyToken_MatchingToken_ReturnsTrue()
    {
        _config["Strava:WebhookVerifyToken"].Returns("my-secret-token");

        bool result = _sut.ValidateVerifyToken("my-secret-token");

        Assert.That(result, Is.True);
    }

    [Test]
    public void ValidateVerifyToken_WrongToken_ReturnsFalse()
    {
        _config["Strava:WebhookVerifyToken"].Returns("my-secret-token");

        bool result = _sut.ValidateVerifyToken("wrong-token");

        Assert.That(result, Is.False);
    }

    [Test]
    public void ValidateVerifyToken_NullConfig_ReturnsFalse()
    {
        _config["Strava:WebhookVerifyToken"].Returns((string?)null);

        bool result = _sut.ValidateVerifyToken("any-token");

        Assert.That(result, Is.False);
    }

    [Test]
    public void ValidateVerifyToken_EmptyConfig_ReturnsFalse()
    {
        _config["Strava:WebhookVerifyToken"].Returns("");

        bool result = _sut.ValidateVerifyToken("");

        Assert.That(result, Is.False);
    }

    [Test]
    public void ValidateVerifyToken_CaseDiffers_ReturnsFalse()
    {
        _config["Strava:WebhookVerifyToken"].Returns("MyToken");

        bool result = _sut.ValidateVerifyToken("mytoken");

        Assert.That(result, Is.False);
    }

    // ========================================================
    // ProcessEventAsync — routing
    // ========================================================

    [Test]
    public async Task ProcessEventAsync_ActivityCreateEvent_CompletesWithoutThrowing()
    {
        var evt = MakeEvent(objectType: "activity", aspectType: "create");

        Assert.DoesNotThrowAsync(() => _sut.ProcessEventAsync(evt, CancellationToken.None));
    }

    [Test]
    public async Task ProcessEventAsync_AthleteUpdateEvent_CompletesWithoutThrowing()
    {
        var evt = MakeEvent(objectType: "athlete", aspectType: "update");

        Assert.DoesNotThrowAsync(() => _sut.ProcessEventAsync(evt, CancellationToken.None));
    }

    [Test]
    public async Task ProcessEventAsync_UnknownObjectType_CompletesWithoutThrowing()
    {
        var evt = MakeEvent(objectType: "gear", aspectType: "update");

        Assert.DoesNotThrowAsync(() => _sut.ProcessEventAsync(evt, CancellationToken.None));
    }

    [Test]
    public async Task ProcessEventAsync_ActivityDeleteEvent_CompletesWithoutThrowing()
    {
        var evt = MakeEvent(objectType: "activity", aspectType: "delete");

        Assert.DoesNotThrowAsync(() => _sut.ProcessEventAsync(evt, CancellationToken.None));
    }

    // ========================================================
    // ProcessEventAsync — activity/create handling
    // ========================================================

    [Test]
    public async Task ProcessEventAsync_ActivityCreate_UnknownAthlete_DoesNotModifyDatabase()
    {
        var evt = MakeEvent(objectType: "activity", aspectType: "create", ownerId: 99999);

        await _sut.ProcessEventAsync(evt, CancellationToken.None);

        Assert.That(_context.Users.ToList(), Is.Empty);
    }

    [Test]
    public async Task ProcessEventAsync_ActivityCreate_KnownAthlete_DoesNotModifyUser()
    {
        var user = await SeedUserAsync(stravaAthleteId: 12345);
        var evt = MakeEvent(objectType: "activity", aspectType: "create", ownerId: 12345);

        await _sut.ProcessEventAsync(evt, CancellationToken.None);

        var fromDb = await _context.Users.FindAsync(user.Id);
        Assert.That(fromDb, Is.Not.Null);
        Assert.That(fromDb!.StravaAccessToken, Is.EqualTo("access-token"));
    }

    // ========================================================
    // ProcessEventAsync — athlete/update deauthorization
    // ========================================================

    [Test]
    public async Task ProcessEventAsync_AthleteUpdate_NotDeauth_DoesNotModifyStravaTokens()
    {
        var user = await SeedUserAsync(stravaAthleteId: 12345);
        var evt = MakeEvent(
            objectType: "athlete",
            aspectType: "update",
            ownerId: 12345,
            updates: new Dictionary<string, string> { { "firstname", "Jane" } }
        );

        await _sut.ProcessEventAsync(evt, CancellationToken.None);

        var fromDb = await _context.Users.FindAsync(user.Id);
        Assert.That(fromDb!.StravaAccessToken, Is.EqualTo("access-token"));
    }

    [Test]
    public async Task ProcessEventAsync_AthleteUpdate_Deauth_UnknownAthlete_DoesNotModifyDatabase()
    {
        var evt = MakeEvent(
            objectType: "athlete",
            aspectType: "update",
            ownerId: 99999,
            updates: new Dictionary<string, string> { { "authorized", "false" } }
        );

        await _sut.ProcessEventAsync(evt, CancellationToken.None);

        Assert.That(_context.Users.ToList(), Is.Empty);
    }

    [Test]
    public async Task ProcessEventAsync_AthleteUpdate_Deauth_ClearsStravaTokens()
    {
        var user = await SeedUserAsync(stravaAthleteId: 12345);
        var evt = MakeEvent(
            objectType: "athlete",
            aspectType: "update",
            ownerId: 12345,
            updates: new Dictionary<string, string> { { "authorized", "false" } }
        );

        await _sut.ProcessEventAsync(evt, CancellationToken.None);

        var fromDb = await _context.Users.FindAsync(user.Id);
        Assert.That(fromDb!.StravaAccessToken, Is.Null);
        Assert.That(fromDb.StravaRefreshToken, Is.Null);
        Assert.That(fromDb.StravaTokenExpiresAt, Is.Null);
    }

    [Test]
    public async Task ProcessEventAsync_AthleteUpdate_Deauth_ClearsStravaProfileData()
    {
        var user = await SeedUserAsync(stravaAthleteId: 12345);
        var evt = MakeEvent(
            objectType: "athlete",
            aspectType: "update",
            ownerId: 12345,
            updates: new Dictionary<string, string> { { "authorized", "false" } }
        );

        await _sut.ProcessEventAsync(evt, CancellationToken.None);

        var fromDb = await _context.Users.FindAsync(user.Id);
        Assert.That(fromDb!.StravaFirstname, Is.Null);
        Assert.That(fromDb.StravaLastname, Is.Null);
        Assert.That(fromDb.StravaProfileMedium, Is.Null);
        Assert.That(fromDb.StravaProfile, Is.Null);
    }

    [Test]
    public async Task ProcessEventAsync_AthleteUpdate_Deauth_PreservesStravaAthleteId()
    {
        var user = await SeedUserAsync(stravaAthleteId: 12345);
        var evt = MakeEvent(
            objectType: "athlete",
            aspectType: "update",
            ownerId: 12345,
            updates: new Dictionary<string, string> { { "authorized", "false" } }
        );

        await _sut.ProcessEventAsync(evt, CancellationToken.None);

        var fromDb = await _context.Users.FindAsync(user.Id);
        Assert.That(fromDb!.StravaAthleteId, Is.EqualTo(12345));
    }

    [Test]
    public async Task ProcessEventAsync_AthleteUpdate_Deauth_RevokesActiveRefreshTokens()
    {
        var user = await SeedUserAsync(stravaAthleteId: 12345);
        await SeedRefreshTokenAsync(user.Id);
        await SeedRefreshTokenAsync(user.Id);
        var evt = MakeEvent(
            objectType: "athlete",
            aspectType: "update",
            ownerId: 12345,
            updates: new Dictionary<string, string> { { "authorized", "false" } }
        );

        await _sut.ProcessEventAsync(evt, CancellationToken.None);

        var tokens = await _context.RefreshTokens.Where(r => r.UserId == user.Id).ToListAsync();
        Assert.That(tokens, Has.Count.EqualTo(2));
        Assert.That(tokens.All(t => t.RevokedAt is not null), Is.True);
    }

    [Test]
    public async Task ProcessEventAsync_AthleteUpdate_Deauth_DoesNotRevokeExpiredTokens()
    {
        var user = await SeedUserAsync(stravaAthleteId: 12345);
        var expiredToken = await SeedRefreshTokenAsync(user.Id, expired: true);
        var evt = MakeEvent(
            objectType: "athlete",
            aspectType: "update",
            ownerId: 12345,
            updates: new Dictionary<string, string> { { "authorized", "false" } }
        );

        await _sut.ProcessEventAsync(evt, CancellationToken.None);

        var fromDb = await _context.RefreshTokens.FindAsync(expiredToken.Id);
        Assert.That(fromDb!.RevokedAt, Is.Null);
    }

    [Test]
    public async Task ProcessEventAsync_AthleteUpdate_Deauth_DoesNotRevokeAlreadyRevokedTokens()
    {
        var user = await SeedUserAsync(stravaAthleteId: 12345);
        DateTime originalRevokedAt = DateTime.UtcNow.AddDays(-1);
        var alreadyRevoked = await SeedRefreshTokenAsync(user.Id, revoked: true);
        var evt = MakeEvent(
            objectType: "athlete",
            aspectType: "update",
            ownerId: 12345,
            updates: new Dictionary<string, string> { { "authorized", "false" } }
        );

        await _sut.ProcessEventAsync(evt, CancellationToken.None);

        // Token was already revoked — it was not in the active set, so unchanged
        var fromDb = await _context.RefreshTokens.FindAsync(alreadyRevoked.Id);
        Assert.That(fromDb!.RevokedAt, Is.Not.Null);
    }

    [Test]
    public async Task ProcessEventAsync_AthleteUpdate_Deauth_DoesNotAffectOtherUsersTokens()
    {
        var targetUser = await SeedUserAsync(stravaAthleteId: 12345);
        var otherUser = await SeedUserAsync(stravaAthleteId: 99999);
        var otherToken = await SeedRefreshTokenAsync(otherUser.Id);
        var evt = MakeEvent(
            objectType: "athlete",
            aspectType: "update",
            ownerId: 12345,
            updates: new Dictionary<string, string> { { "authorized", "false" } }
        );

        await _sut.ProcessEventAsync(evt, CancellationToken.None);

        var fromDb = await _context.RefreshTokens.FindAsync(otherToken.Id);
        Assert.That(fromDb!.RevokedAt, Is.Null);
    }
}
