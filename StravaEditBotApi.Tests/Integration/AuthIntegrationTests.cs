using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs;

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
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static RegisterDto ValidDto(
        string email = "user@example.com",
        string password = "Password123!") => new(email, password);

    // ========================================================
    // POST /api/auth/register
    // ========================================================

    [Test]
    public async Task Register_ValidCredentials_Returns200()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", ValidDto());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Register_ValidCredentials_ReturnsAccessTokenInBody()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", ValidDto());
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.That(body, Is.Not.Null);
        Assert.That(body!.AccessToken, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task Register_ValidCredentials_SetsRefreshTokenCookie()
    {
        // Use a cookie-transparent client so Set-Cookie header is visible.
        using var rawClient = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false });

        var response = await rawClient.PostAsJsonAsync("/api/auth/register", ValidDto());

        var setCookieHeader = response.Headers
            .GetValues("Set-Cookie")
            .FirstOrDefault(v => v.Contains("refreshToken"));

        Assert.That(setCookieHeader, Is.Not.Null);
        Assert.That(setCookieHeader, Does.Contain("httponly").IgnoreCase);
    }

    [Test]
    public async Task Register_DuplicateEmail_Returns400()
    {
        await _client.PostAsJsonAsync("/api/auth/register", ValidDto());

        var response = await _client.PostAsJsonAsync("/api/auth/register", ValidDto());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Register_WeakPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", ValidDto(password: "weak"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Register_InvalidEmailFormat_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", ValidDto(email: "notanemail"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Register_PersistsUserToDatabase()
    {
        var dto = ValidDto(email: "persisted@example.com");

        await _client.PostAsJsonAsync("/api/auth/register", dto);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.SingleOrDefault(u => u.Email == dto.Email);

        Assert.That(user, Is.Not.Null);
    }

    [Test]
    public async Task Register_ValidCredentials_StoresRefreshTokenInDatabase()
    {
        await _client.PostAsJsonAsync("/api/auth/register", ValidDto());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.That(db.RefreshTokens.Count(), Is.EqualTo(1));
    }
}
