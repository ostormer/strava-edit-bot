using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StravaEditBotApi.Controllers;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Models;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Tests.Unit.Controllers;

[TestFixture]
public class AuthControllerTests
{
    private IStravaAuthService _stravaAuthService = null!;
    private ITokenService _tokenService = null!;
    private AppDbContext _db = null!;
    private IWebHostEnvironment _env = null!;
    private AuthController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _stravaAuthService = Substitute.For<IStravaAuthService>();
        _tokenService = Substitute.For<ITokenService>();
        _env = Substitute.For<IWebHostEnvironment>();

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(dbOptions);

        _sut = new AuthController(_stravaAuthService, _tokenService, _db, _env);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private void SetupTokenService(
        string accessToken = "access-token",
        string rawRefresh = "raw-refresh",
        string refreshHash = "refresh-hash")
    {
        _tokenService.GenerateAccessToken(Arg.Any<AppUser>()).Returns(accessToken);
        _tokenService.GenerateRefreshToken().Returns(rawRefresh);
        _tokenService.HashToken(rawRefresh).Returns(refreshHash);
    }

    private void SetRequestCookie(string value) =>
        _sut.ControllerContext.HttpContext.Request.Headers["Cookie"] = $"refreshToken={value}";

    private StravaTokenData MakeStravaTokenData(long athleteId = 123456) =>
        new(athleteId, "strava-access", "strava-refresh", DateTime.UtcNow.AddHours(6));

    private async Task<RefreshToken> SeedActiveTokenAsync(AppUser user, string tokenHash = "valid-hash")
    {
        await _db.Users.AddAsync(user);
        var token = new RefreshToken
        {
            UserId = user.Id,
            User = user,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
        };
        await _db.RefreshTokens.AddAsync(token);
        await _db.SaveChangesAsync();
        return token;
    }

    // ========================================================
    // StravaCallbackAsync
    // ========================================================

    [Test]
    public async Task StravaCallback_NewUser_ReturnsOk()
    {
        _stravaAuthService.ExchangeCodeAsync(Arg.Any<string>()).Returns(MakeStravaTokenData());
        SetupTokenService();

        var result = await _sut.StravaCallbackAsync(new StravaCallbackDto("auth-code"));

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task StravaCallback_NewUser_ReturnsAccessToken()
    {
        _stravaAuthService.ExchangeCodeAsync(Arg.Any<string>()).Returns(MakeStravaTokenData());
        SetupTokenService(accessToken: "the-access-token");

        var result = await _sut.StravaCallbackAsync(new StravaCallbackDto("auth-code"));

        var ok = (OkObjectResult)result;
        Assert.That(((AuthResponseDto)ok.Value!).AccessToken, Is.EqualTo("the-access-token"));
    }

    [Test]
    public async Task StravaCallback_NewUser_SetsRefreshTokenCookie()
    {
        _stravaAuthService.ExchangeCodeAsync(Arg.Any<string>()).Returns(MakeStravaTokenData());
        SetupTokenService();

        await _sut.StravaCallbackAsync(new StravaCallbackDto("auth-code"));

        var setCookie = _sut.ControllerContext.HttpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.That(setCookie, Does.Contain("refreshToken"));
    }

    [Test]
    public async Task StravaCallback_NewUser_CreatesUserInDb()
    {
        var tokenData = MakeStravaTokenData(athleteId: 999);
        _stravaAuthService.ExchangeCodeAsync(Arg.Any<string>()).Returns(tokenData);
        SetupTokenService();

        await _sut.StravaCallbackAsync(new StravaCallbackDto("auth-code"));

        var user = _db.Users.SingleOrDefault(u => u.StravaAthleteId == 999);
        Assert.That(user, Is.Not.Null);
    }

    [Test]
    public async Task StravaCallback_ExistingUser_DoesNotDuplicateUser()
    {
        var existing = new AppUser
        {
            UserName = "123456",
            StravaAthleteId = 123456,
        };
        await _db.Users.AddAsync(existing);
        await _db.SaveChangesAsync();

        _stravaAuthService.ExchangeCodeAsync(Arg.Any<string>()).Returns(MakeStravaTokenData(athleteId: 123456));
        SetupTokenService();

        await _sut.StravaCallbackAsync(new StravaCallbackDto("auth-code"));

        Assert.That(_db.Users.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task StravaCallback_StravaExchangeThrows_ReturnsBadRequest()
    {
        _stravaAuthService.ExchangeCodeAsync(Arg.Any<string>())
            .Throws(new HttpRequestException("Strava error"));

        var result = await _sut.StravaCallbackAsync(new StravaCallbackDto("bad-code"));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    // ========================================================
    // RefreshAsync
    // ========================================================

    [Test]
    public async Task Refresh_NoCookie_ReturnsUnauthorized()
    {
        var result = await _sut.RefreshAsync();

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task Refresh_TokenNotInDb_ReturnsUnauthorized()
    {
        SetRequestCookie("unknown-token");
        _tokenService.HashToken("unknown-token").Returns("unknown-hash");

        var result = await _sut.RefreshAsync();

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task Refresh_RevokedToken_ReturnsUnauthorized()
    {
        var user = new AppUser { Id = "user-1", UserName = "user-1" };
        await _db.Users.AddAsync(user);
        await _db.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            User = user,
            TokenHash = "revoked-hash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow.AddMinutes(-1),
        });
        await _db.SaveChangesAsync();

        SetRequestCookie("revoked-token");
        _tokenService.HashToken("revoked-token").Returns("revoked-hash");

        var result = await _sut.RefreshAsync();

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task Refresh_ExpiredToken_ReturnsUnauthorized()
    {
        var user = new AppUser { Id = "user-1", UserName = "user-1" };
        await _db.Users.AddAsync(user);
        await _db.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            User = user,
            TokenHash = "expired-hash",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-8),
        });
        await _db.SaveChangesAsync();

        SetRequestCookie("expired-token");
        _tokenService.HashToken("expired-token").Returns("expired-hash");

        var result = await _sut.RefreshAsync();

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task Refresh_ValidToken_ReturnsOk()
    {
        var user = new AppUser { Id = "user-1", UserName = "user-1" };
        await SeedActiveTokenAsync(user, "valid-hash");

        SetRequestCookie("valid-raw-token");
        _tokenService.HashToken("valid-raw-token").Returns("valid-hash");
        SetupTokenService(rawRefresh: "new-raw", refreshHash: "new-hash");

        var result = await _sut.RefreshAsync();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Refresh_ValidToken_ReturnsNewAccessToken()
    {
        var user = new AppUser { Id = "user-1", UserName = "user-1" };
        await SeedActiveTokenAsync(user, "valid-hash");

        SetRequestCookie("valid-raw-token");
        _tokenService.HashToken("valid-raw-token").Returns("valid-hash");
        SetupTokenService(accessToken: "new-access-token", rawRefresh: "new-raw", refreshHash: "new-hash");

        var result = await _sut.RefreshAsync();

        var ok = (OkObjectResult)result;
        Assert.That(((AuthResponseDto)ok.Value!).AccessToken, Is.EqualTo("new-access-token"));
    }

    [Test]
    public async Task Refresh_ValidToken_RevokesOldToken()
    {
        var user = new AppUser { Id = "user-1", UserName = "user-1" };
        await SeedActiveTokenAsync(user, "valid-hash");

        SetRequestCookie("valid-raw-token");
        _tokenService.HashToken("valid-raw-token").Returns("valid-hash");
        SetupTokenService(rawRefresh: "new-raw", refreshHash: "new-hash");

        await _sut.RefreshAsync();

        var revoked = await _db.RefreshTokens.SingleAsync(r => r.TokenHash == "valid-hash");
        Assert.That(revoked.RevokedAt, Is.Not.Null);
    }

    [Test]
    public async Task Refresh_ValidToken_IssuesNewRefreshTokenInDb()
    {
        var user = new AppUser { Id = "user-1", UserName = "user-1" };
        await SeedActiveTokenAsync(user, "valid-hash");

        SetRequestCookie("valid-raw-token");
        _tokenService.HashToken("valid-raw-token").Returns("valid-hash");
        SetupTokenService(rawRefresh: "new-raw", refreshHash: "new-hash");

        await _sut.RefreshAsync();

        Assert.That(_db.RefreshTokens.Count(), Is.EqualTo(2));
        Assert.That(_db.RefreshTokens.Any(r => r.TokenHash == "new-hash"), Is.True);
    }

    // ========================================================
    // LogoutAsync
    // ========================================================

    [Test]
    public async Task Logout_NoCookie_ReturnsOk()
    {
        var result = await _sut.LogoutAsync();

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task Logout_WithValidCookie_ReturnsOk()
    {
        var user = new AppUser { Id = "user-1", UserName = "user-1" };
        await SeedActiveTokenAsync(user, "logout-hash");
        SetRequestCookie("logout-raw-token");
        _tokenService.HashToken("logout-raw-token").Returns("logout-hash");

        var result = await _sut.LogoutAsync();

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task Logout_WithValidCookie_RevokesToken()
    {
        var user = new AppUser { Id = "user-1", UserName = "user-1" };
        await SeedActiveTokenAsync(user, "logout-hash");
        SetRequestCookie("logout-raw-token");
        _tokenService.HashToken("logout-raw-token").Returns("logout-hash");

        await _sut.LogoutAsync();

        var token = await _db.RefreshTokens.SingleAsync(r => r.TokenHash == "logout-hash");
        Assert.That(token.RevokedAt, Is.Not.Null);
    }

    [Test]
    public async Task Logout_TokenNotInDb_StillReturnsOk()
    {
        SetRequestCookie("unknown-raw-token");
        _tokenService.HashToken("unknown-raw-token").Returns("unknown-hash");

        var result = await _sut.LogoutAsync();

        Assert.That(result, Is.InstanceOf<OkResult>());
    }
}
