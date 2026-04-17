using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using StravaEditBotApi.Controllers;
using StravaEditBotApi.DTOs.Templates;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Tests.Unit.Controllers;

[TestFixture]
public class RulesetTemplatesControllerTests
{
    private IRulesetTemplateService _templateService = null!;
    private RulesetTemplatesController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _templateService = Substitute.For<IRulesetTemplateService>();
        _sut = new RulesetTemplatesController(_templateService);
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

    private static RulesetTemplateResponseDto MakeTemplateResponse(
        int id = 1,
        string name = "Test Template",
        string? shareToken = null) =>
        new(
            Id: id,
            Name: name,
            Description: null,
            Filter: null,
            Effect: null,
            IsPublic: true,
            ShareToken: shareToken,
            UsageCount: 0,
            BundledVariables: null,
            CreatedAt: DateTime.UtcNow,
            SanitizedProperties: null
        );

    // ========================================================
    // GetPublicTemplatesAsync
    // ========================================================

    [Test]
    public async Task GetPublicTemplatesAsync_ReturnsOk()
    {
        _templateService.GetPublicTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns([MakeTemplateResponse()]);

        var result = await _sut.GetPublicTemplatesAsync(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetPublicTemplatesAsync_ReturnsAllTemplates()
    {
        var templates = new List<RulesetTemplateResponseDto>
        {
            MakeTemplateResponse(id: 1, name: "Template A"),
            MakeTemplateResponse(id: 2, name: "Template B")
        };
        _templateService.GetPublicTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns(templates);

        var result = await _sut.GetPublicTemplatesAsync(CancellationToken.None);

        var ok = (OkObjectResult)result;
        var returned = (List<RulesetTemplateResponseDto>)ok.Value!;
        Assert.That(returned, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetPublicTemplatesAsync_CallsServiceExactlyOnce()
    {
        _templateService.GetPublicTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        await _sut.GetPublicTemplatesAsync(CancellationToken.None);

        await _templateService.Received(1).GetPublicTemplatesAsync(Arg.Any<CancellationToken>());
    }

    // ========================================================
    // GetByShareTokenAsync
    // ========================================================

    [Test]
    public async Task GetByShareTokenAsync_NotFound_ReturnsNotFound()
    {
        _templateService.GetByShareTokenAsync("invalid-token", Arg.Any<CancellationToken>())
            .Returns((RulesetTemplateResponseDto?)null);

        var result = await _sut.GetByShareTokenAsync("invalid-token", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetByShareTokenAsync_Found_ReturnsOk()
    {
        _templateService.GetByShareTokenAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(MakeTemplateResponse(shareToken: "valid-token"));

        var result = await _sut.GetByShareTokenAsync("valid-token", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetByShareTokenAsync_Found_ReturnsCorrectDto()
    {
        var expected = MakeTemplateResponse(id: 5, shareToken: "abc123");
        _templateService.GetByShareTokenAsync("abc123", Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.GetByShareTokenAsync("abc123", CancellationToken.None);

        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetByShareTokenAsync_CallsServiceWithToken()
    {
        _templateService.GetByShareTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(MakeTemplateResponse());

        await _sut.GetByShareTokenAsync("specific-token", CancellationToken.None);

        await _templateService.Received(1).GetByShareTokenAsync("specific-token", Arg.Any<CancellationToken>());
    }

    // ========================================================
    // InstantiateTemplateAsync
    // ========================================================

    [Test]
    public async Task InstantiateTemplateAsync_MissingClaim_ReturnsUnauthorized()
    {
        SetUserClaims(null);

        var result = await _sut.InstantiateTemplateAsync(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task InstantiateTemplateAsync_MissingClaim_DoesNotCallService()
    {
        SetUserClaims(null);

        await _sut.InstantiateTemplateAsync(1, CancellationToken.None);

        await _templateService.DidNotReceive().InstantiateAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task InstantiateTemplateAsync_NotFound_ReturnsNotFound()
    {
        SetUserClaims("user-1");
        _templateService.InstantiateAsync("user-1", 99, Arg.Any<CancellationToken>())
            .Returns((RulesetTemplateResponseDto?)null);

        var result = await _sut.InstantiateTemplateAsync(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task InstantiateTemplateAsync_Found_ReturnsOk()
    {
        SetUserClaims("user-1");
        _templateService.InstantiateAsync("user-1", 1, Arg.Any<CancellationToken>())
            .Returns(MakeTemplateResponse(id: 1));

        var result = await _sut.InstantiateTemplateAsync(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task InstantiateTemplateAsync_Found_ReturnsCorrectDto()
    {
        SetUserClaims("user-1");
        var expected = MakeTemplateResponse(id: 3, name: "Instantiated Template");
        _templateService.InstantiateAsync("user-1", 3, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.InstantiateTemplateAsync(3, CancellationToken.None);

        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.EqualTo(expected));
    }

    [Test]
    public async Task InstantiateTemplateAsync_Found_CallsServiceWithCorrectArgs()
    {
        SetUserClaims("user-42");
        _templateService.InstantiateAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(MakeTemplateResponse());

        await _sut.InstantiateTemplateAsync(7, CancellationToken.None);

        await _templateService.Received(1).InstantiateAsync("user-42", 7, Arg.Any<CancellationToken>());
    }
}
