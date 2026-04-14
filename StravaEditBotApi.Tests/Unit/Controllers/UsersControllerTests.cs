using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using StravaEditBotApi.Controllers;
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Models;

namespace StravaEditBotApi.Tests.Unit.Controllers;

[TestFixture]
public class UsersControllerTests
{
    private UserManager<AppUser> _userManager = null!;
    private UsersController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var store = Substitute.For<IUserStore<AppUser>>();
        _userManager = Substitute.For<UserManager<AppUser>>(
            store, null, null, null, null, null, null, null, null);

        _sut = new UsersController(_userManager);
    }

    private void SetUserClaims(string? userId)
    {
        var claims = userId is not null
            ? new[] { new Claim(ClaimTypes.NameIdentifier, userId) }
            : Array.Empty<Claim>();

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims))
            }
        };
    }

    private static AppUser MakeUser(
        string? id = null,
        string? firstname = null,
        string? lastname = null,
        string? profileMedium = null,
        string? profile = null)
    {
        return new AppUser
        {
            Id = id ?? "user-123",
            StravaFirstname = firstname ?? "Jane",
            StravaLastname = lastname ?? "Doe",
            StravaProfileMedium = profileMedium ?? "https://example.com/medium.jpg",
            StravaProfile = profile ?? "https://example.com/profile.jpg"
        };
    }

    // ========================================================
    // GetCurrentUserAsync
    // ========================================================

    [Test]
    public async Task GetCurrentUserAsync_ValidUser_ReturnsOk()
    {
        var user = MakeUser();
        SetUserClaims(user.Id);
        _userManager.FindByIdAsync(user.Id).Returns(user);

        var result = await _sut.GetCurrentUserAsync();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetCurrentUserAsync_ValidUser_ReturnsCorrectDto()
    {
        var user = MakeUser(firstname: "Jane", lastname: "Doe",
            profileMedium: "https://example.com/medium.jpg",
            profile: "https://example.com/profile.jpg");
        SetUserClaims(user.Id);
        _userManager.FindByIdAsync(user.Id).Returns(user);

        var result = await _sut.GetCurrentUserAsync();

        var ok = (OkObjectResult)result;
        var dto = (UserDto)ok.Value!;
        Assert.That(dto.Firstname, Is.EqualTo("Jane"));
        Assert.That(dto.Lastname, Is.EqualTo("Doe"));
        Assert.That(dto.ProfileMedium, Is.EqualTo("https://example.com/medium.jpg"));
        Assert.That(dto.Profile, Is.EqualTo("https://example.com/profile.jpg"));
    }

    [Test]
    public async Task GetCurrentUserAsync_NullStravaFields_ReturnsEmptyStrings()
    {
        var user = MakeUser(firstname: null, lastname: null, profileMedium: null, profile: null);
        user.StravaFirstname = null;
        user.StravaLastname = null;
        user.StravaProfileMedium = null;
        user.StravaProfile = null;
        SetUserClaims(user.Id);
        _userManager.FindByIdAsync(user.Id).Returns(user);

        var result = await _sut.GetCurrentUserAsync();

        var ok = (OkObjectResult)result;
        var dto = (UserDto)ok.Value!;
        Assert.That(dto.Firstname, Is.EqualTo(string.Empty));
        Assert.That(dto.Lastname, Is.EqualTo(string.Empty));
        Assert.That(dto.ProfileMedium, Is.EqualTo(string.Empty));
        Assert.That(dto.Profile, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task GetCurrentUserAsync_MissingNameIdentifierClaim_ReturnsUnauthorized()
    {
        SetUserClaims(userId: null);

        var result = await _sut.GetCurrentUserAsync();

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task GetCurrentUserAsync_MissingNameIdentifierClaim_DoesNotCallUserManager()
    {
        SetUserClaims(userId: null);

        await _sut.GetCurrentUserAsync();

        await _userManager.DidNotReceive().FindByIdAsync(Arg.Any<string>());
    }

    [Test]
    public async Task GetCurrentUserAsync_UserNotFound_ReturnsNotFound()
    {
        SetUserClaims("nonexistent-id");
        _userManager.FindByIdAsync("nonexistent-id").Returns((AppUser?)null);

        var result = await _sut.GetCurrentUserAsync();

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetCurrentUserAsync_ValidUser_CallsUserManagerWithCorrectId()
    {
        string userId = "specific-user-id";
        var user = MakeUser(id: userId);
        SetUserClaims(userId);
        _userManager.FindByIdAsync(userId).Returns(user);

        await _sut.GetCurrentUserAsync();

        await _userManager.Received(1).FindByIdAsync(userId);
    }
}
