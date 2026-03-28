using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using StravaEditBotApi.Controllers;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Models;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Tests.Unit.Controllers;

[TestFixture]
public class AuthControllerTests
{
    private UserManager<AppUser> _userManager = null!;
    private ITokenService _tokenService = null!;
    private AppDbContext _db = null!;
    private IWebHostEnvironment _env = null!;
    private AuthController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        // UserManager is a class, not an interface — NSubstitute requires constructor args.
        var store = Substitute.For<IUserStore<AppUser>>();
        _userManager = Substitute.For<UserManager<AppUser>>(
            store, null, null, null, null, null, null, null, null);

        _tokenService = Substitute.For<ITokenService>();
        _env = Substitute.For<IWebHostEnvironment>();

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(dbOptions);

        _sut = new AuthController(_userManager, _tokenService, _db, _env);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // Sets up the three ITokenService calls made inside IssueTokensAsync.
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

    private static AppUser MakeUser(string id = "user-1", string email = "user@example.com") =>
        new() { Id = id, Email = email, UserName = email };

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
    // RegisterAsync
    // ========================================================

    [Test]
    public async Task Register_SuccessfulCreation_ReturnsOk()
    {
        _userManager.CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);
        SetupTokenService();

        var result = await _sut.RegisterAsync(new RegisterDto("user@example.com", "Password1!"));

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Register_SuccessfulCreation_ReturnsAccessToken()
    {
        _userManager.CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);
        SetupTokenService(accessToken: "the-access-token");

        var result = await _sut.RegisterAsync(new RegisterDto("user@example.com", "Password1!"));

        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.InstanceOf<AuthResponseDto>());
        Assert.That(((AuthResponseDto)ok.Value!).AccessToken, Is.EqualTo("the-access-token"));
    }

    [Test]
    public async Task Register_SuccessfulCreation_SetsRefreshTokenCookie()
    {
        _userManager.CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);
        SetupTokenService();

        await _sut.RegisterAsync(new RegisterDto("user@example.com", "Password1!"));

        var setCookie = _sut.ControllerContext.HttpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.That(setCookie, Does.Contain("refreshToken"));
    }

    [Test]
    public async Task Register_SuccessfulCreation_StoresRefreshTokenInDb()
    {
        _userManager.CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);
        SetupTokenService(refreshHash: "stored-hash");

        await _sut.RegisterAsync(new RegisterDto("user@example.com", "Password1!"));

        Assert.That(_db.RefreshTokens.Count(), Is.EqualTo(1));
        Assert.That(_db.RefreshTokens.Single().TokenHash, Is.EqualTo("stored-hash"));
    }

    [Test]
    public async Task Register_FailedCreation_ReturnsBadRequest()
    {
        var error = new IdentityError { Code = "DuplicateEmail", Description = "Email taken." };
        _userManager.CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(error));

        var result = await _sut.RegisterAsync(new RegisterDto("user@example.com", "Password1!"));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    // ========================================================
    // LoginAsync
    // ========================================================

    [Test]
    public async Task Login_UserNotFound_ReturnsUnauthorized()
    {
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((AppUser?)null);

        var result = await _sut.LoginAsync(new LoginDto("missing@example.com", "Password1!"));

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var user = MakeUser();
        _userManager.FindByEmailAsync(user.Email!).Returns(user);
        _userManager.CheckPasswordAsync(user, Arg.Any<string>()).Returns(false);

        var result = await _sut.LoginAsync(new LoginDto(user.Email!, "WrongPassword!"));

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task Login_ValidCredentials_ReturnsOk()
    {
        var user = MakeUser();
        _userManager.FindByEmailAsync(user.Email!).Returns(user);
        _userManager.CheckPasswordAsync(user, Arg.Any<string>()).Returns(true);
        SetupTokenService();

        var result = await _sut.LoginAsync(new LoginDto(user.Email!, "Password1!"));

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Login_ValidCredentials_ReturnsAccessToken()
    {
        var user = MakeUser();
        _userManager.FindByEmailAsync(user.Email!).Returns(user);
        _userManager.CheckPasswordAsync(user, Arg.Any<string>()).Returns(true);
        SetupTokenService(accessToken: "login-access-token");

        var result = await _sut.LoginAsync(new LoginDto(user.Email!, "Password1!"));

        var ok = (OkObjectResult)result;
        Assert.That(((AuthResponseDto)ok.Value!).AccessToken, Is.EqualTo("login-access-token"));
    }

    [Test]
    public async Task Login_ValidCredentials_SetsRefreshTokenCookie()
    {
        var user = MakeUser();
        _userManager.FindByEmailAsync(user.Email!).Returns(user);
        _userManager.CheckPasswordAsync(user, Arg.Any<string>()).Returns(true);
        SetupTokenService();

        await _sut.LoginAsync(new LoginDto(user.Email!, "Password1!"));

        var setCookie = _sut.ControllerContext.HttpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.That(setCookie, Does.Contain("refreshToken"));
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
        var user = MakeUser();
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
        var user = MakeUser();
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
        var user = MakeUser();
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
        var user = MakeUser();
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
        var user = MakeUser();
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
        var user = MakeUser();
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
        var user = MakeUser();
        await SeedActiveTokenAsync(user, "logout-hash");
        SetRequestCookie("logout-raw-token");
        _tokenService.HashToken("logout-raw-token").Returns("logout-hash");

        var result = await _sut.LogoutAsync();

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task Logout_WithValidCookie_RevokesToken()
    {
        var user = MakeUser();
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
