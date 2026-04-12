using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Tests.Integration;

[TestFixture]
public class AuthIntegrationTests
{
    private WebAppFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebAppFactory();
        _client = _factory.CreateClient();
    }

    [SetUp]
    public void SetUp()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.RefreshTokens.RemoveRange(db.RefreshTokens);
        db.Users.RemoveRange(db.Users);
        db.SaveChanges();

        // Reset the stub to default behaviour before each test.
        _factory.StravaAuthService.ExchangeCodeAsync(Arg.Any<string>())
            .Returns(new StravaTokenData(111222, "strava-access", "strava-refresh", DateTime.UtcNow.AddHours(6)));
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ========================================================
    // POST /api/auth/strava/callback
    // ========================================================

    [Test]
    public async Task StravaCallback_ValidCode_Returns200()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/strava/callback", new StravaCallbackDto("valid-code"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task StravaCallback_ValidCode_ReturnsAccessTokenInBody()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/strava/callback", new StravaCallbackDto("valid-code"));
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.That(body, Is.Not.Null);
        Assert.That(body!.AccessToken, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task StravaCallback_ValidCode_SetsRefreshTokenCookie()
    {
        using var rawClient = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false });

        var response = await rawClient.PostAsJsonAsync("/api/auth/strava/callback", new StravaCallbackDto("valid-code"));

        var setCookieHeader = response.Headers
            .GetValues("Set-Cookie")
            .FirstOrDefault(v => v.Contains("refreshToken"));

        Assert.That(setCookieHeader, Is.Not.Null);
        Assert.That(setCookieHeader, Does.Contain("httponly").IgnoreCase);
    }

    [Test]
    public async Task StravaCallback_NewUser_CreatesUserInDatabase()
    {
        await _client.PostAsJsonAsync("/api/auth/strava/callback", new StravaCallbackDto("valid-code"));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.SingleOrDefault(u => u.StravaAthleteId == 111222);

        Assert.That(user, Is.Not.Null);
    }

    [Test]
    public async Task StravaCallback_ExistingUser_DoesNotDuplicateUser()
    {
        // First login creates the user.
        await _client.PostAsJsonAsync("/api/auth/strava/callback", new StravaCallbackDto("first-code"));

        // Second login with the same athlete ID should reuse the existing user.
        await _client.PostAsJsonAsync("/api/auth/strava/callback", new StravaCallbackDto("second-code"));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.That(db.Users.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task StravaCallback_StravaExchangeFailure_Returns400()
    {
        _factory.StravaAuthService.ExchangeCodeAsync(Arg.Any<string>())
            .Throws(new HttpRequestException("Strava error"));

        var response = await _client.PostAsJsonAsync("/api/auth/strava/callback", new StravaCallbackDto("bad-code"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task StravaCallback_ValidCode_StoresRefreshTokenInDatabase()
    {
        await _client.PostAsJsonAsync("/api/auth/strava/callback", new StravaCallbackDto("valid-code"));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.That(db.RefreshTokens.Count(), Is.EqualTo(1));
    }
}
