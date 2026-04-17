using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using StravaEditBotApi.Controllers;
using StravaEditBotApi.DTOs.Variables;
using StravaEditBotApi.Models.Rules;
using StravaEditBotApi.Services.Auth;
using StravaEditBotApi.Services.Rulesets;
using StravaEditBotApi.Services.Webhook;

namespace StravaEditBotApi.Tests.Unit.Controllers;

[TestFixture]
public class CustomVariablesControllerTests
{
    private ICustomVariableService _variableService = null!;
    private CustomVariablesController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _variableService = Substitute.For<ICustomVariableService>();
        _sut = new CustomVariablesController(_variableService);
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

    private static CustomVariableDefinition MakeDefinition() =>
        new()
        {
            Name = "test_var",
            Cases = [],
            DefaultValue = "default"
        };

    private static CustomVariableResponseDto MakeVariableResponse(
        int id = 1,
        string name = "Test Variable") =>
        new(
            Id: id,
            Name: name,
            Description: null,
            Definition: MakeDefinition(),
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

    private static CreateCustomVariableDto MakeCreateDto(string name = "Test Variable") =>
        new(
            Name: name,
            Description: null,
            Definition: MakeDefinition()
        );

    private static UpdateCustomVariableDto MakeUpdateDto(string? description = "Updated") =>
        new(
            Description: description,
            Definition: null
        );

    // ========================================================
    // GetVariablesAsync
    // ========================================================

    [Test]
    public async Task GetVariablesAsync_MissingClaim_ReturnsUnauthorized()
    {
        SetUserClaims(null);

        var result = await _sut.GetVariablesAsync(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task GetVariablesAsync_MissingClaim_DoesNotCallService()
    {
        SetUserClaims(null);

        await _sut.GetVariablesAsync(CancellationToken.None);

        await _variableService.DidNotReceive().GetUserVariablesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetVariablesAsync_ValidUser_ReturnsOk()
    {
        SetUserClaims("user-1");
        _variableService.GetUserVariablesAsync("user-1", Arg.Any<CancellationToken>())
            .Returns([MakeVariableResponse()]);

        var result = await _sut.GetVariablesAsync(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetVariablesAsync_ValidUser_CallsServiceWithUserId()
    {
        SetUserClaims("user-42");
        _variableService.GetUserVariablesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await _sut.GetVariablesAsync(CancellationToken.None);

        await _variableService.Received(1).GetUserVariablesAsync("user-42", Arg.Any<CancellationToken>());
    }

    // ========================================================
    // GetVariableAsync
    // ========================================================

    [Test]
    public async Task GetVariableAsync_MissingClaim_ReturnsUnauthorized()
    {
        SetUserClaims(null);

        var result = await _sut.GetVariableAsync(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task GetVariableAsync_NotFound_ReturnsNotFound()
    {
        SetUserClaims("user-1");
        _variableService.GetByIdAsync("user-1", 99, Arg.Any<CancellationToken>())
            .Returns((CustomVariableResponseDto?)null);

        var result = await _sut.GetVariableAsync(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetVariableAsync_Found_ReturnsOk()
    {
        SetUserClaims("user-1");
        _variableService.GetByIdAsync("user-1", 1, Arg.Any<CancellationToken>())
            .Returns(MakeVariableResponse(id: 1));

        var result = await _sut.GetVariableAsync(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetVariableAsync_Found_ReturnsCorrectDto()
    {
        SetUserClaims("user-1");
        var expected = MakeVariableResponse(id: 5, name: "My Variable");
        _variableService.GetByIdAsync("user-1", 5, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.GetVariableAsync(5, CancellationToken.None);

        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.EqualTo(expected));
    }

    // ========================================================
    // CreateVariableAsync
    // ========================================================

    [Test]
    public async Task CreateVariableAsync_MissingClaim_ReturnsUnauthorized()
    {
        SetUserClaims(null);

        var result = await _sut.CreateVariableAsync(MakeCreateDto(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task CreateVariableAsync_DuplicateName_ReturnsConflict()
    {
        SetUserClaims("user-1");
        _variableService.CreateAsync("user-1", Arg.Any<CreateCustomVariableDto>(), Arg.Any<CancellationToken>())
            .Returns(((CustomVariableResponseDto?)null, "A variable with this name already exists."));

        var result = await _sut.CreateVariableAsync(MakeCreateDto("Duplicate"), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
    }

    [Test]
    public async Task CreateVariableAsync_ValidDto_ReturnsCreatedAtAction()
    {
        SetUserClaims("user-1");
        var dto = MakeCreateDto("New Variable");
        _variableService.CreateAsync("user-1", dto, Arg.Any<CancellationToken>())
            .Returns((MakeVariableResponse(id: 3, name: "New Variable"), (string?)null));

        var result = await _sut.CreateVariableAsync(dto, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task CreateVariableAsync_ValidDto_ReturnsCorrectRouteAndBody()
    {
        SetUserClaims("user-1");
        var created = MakeVariableResponse(id: 3);
        _variableService.CreateAsync(Arg.Any<string>(), Arg.Any<CreateCustomVariableDto>(), Arg.Any<CancellationToken>())
            .Returns((created, (string?)null));

        var result = await _sut.CreateVariableAsync(MakeCreateDto(), CancellationToken.None);

        var r = (CreatedAtActionResult)result;
        Assert.That(r.StatusCode, Is.EqualTo(201));
        Assert.That(r.ActionName, Is.EqualTo(nameof(CustomVariablesController.GetVariableAsync)));
        Assert.That(r.RouteValues!["id"], Is.EqualTo(3));
        Assert.That(r.Value, Is.EqualTo(created));
    }

    [Test]
    public async Task CreateVariableAsync_ValidDto_CallsServiceExactlyOnce()
    {
        SetUserClaims("user-1");
        var dto = MakeCreateDto();
        _variableService.CreateAsync(Arg.Any<string>(), Arg.Any<CreateCustomVariableDto>(), Arg.Any<CancellationToken>())
            .Returns((MakeVariableResponse(), (string?)null));

        await _sut.CreateVariableAsync(dto, CancellationToken.None);

        await _variableService.Received(1).CreateAsync("user-1", dto, Arg.Any<CancellationToken>());
    }

    // ========================================================
    // UpdateVariableAsync
    // ========================================================

    [Test]
    public async Task UpdateVariableAsync_MissingClaim_ReturnsUnauthorized()
    {
        SetUserClaims(null);

        var result = await _sut.UpdateVariableAsync(1, MakeUpdateDto(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task UpdateVariableAsync_DuplicateName_ReturnsConflict()
    {
        SetUserClaims("user-1");
        _variableService.UpdateAsync("user-1", 1, Arg.Any<UpdateCustomVariableDto>(), Arg.Any<CancellationToken>())
            .Returns(((CustomVariableResponseDto?)null, "A variable with this name already exists."));

        var result = await _sut.UpdateVariableAsync(1, MakeUpdateDto(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
    }

    [Test]
    public async Task UpdateVariableAsync_NotFound_ReturnsNotFound()
    {
        SetUserClaims("user-1");
        _variableService.UpdateAsync("user-1", 99, Arg.Any<UpdateCustomVariableDto>(), Arg.Any<CancellationToken>())
            .Returns(((CustomVariableResponseDto?)null, (string?)null));

        var result = await _sut.UpdateVariableAsync(99, MakeUpdateDto(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task UpdateVariableAsync_Found_ReturnsOk()
    {
        SetUserClaims("user-1");
        _variableService.UpdateAsync("user-1", 1, Arg.Any<UpdateCustomVariableDto>(), Arg.Any<CancellationToken>())
            .Returns((MakeVariableResponse(id: 1), (string?)null));

        var result = await _sut.UpdateVariableAsync(1, MakeUpdateDto(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task UpdateVariableAsync_Found_ReturnsUpdatedDto()
    {
        SetUserClaims("user-1");
        var updated = MakeVariableResponse(id: 1, name: "Updated Variable");
        _variableService.UpdateAsync("user-1", 1, Arg.Any<UpdateCustomVariableDto>(), Arg.Any<CancellationToken>())
            .Returns((updated, (string?)null));

        var result = await _sut.UpdateVariableAsync(1, MakeUpdateDto(), CancellationToken.None);

        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.EqualTo(updated));
    }

    // ========================================================
    // DeleteVariableAsync
    // ========================================================

    [Test]
    public async Task DeleteVariableAsync_MissingClaim_ReturnsUnauthorized()
    {
        SetUserClaims(null);

        var result = await _sut.DeleteVariableAsync(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task DeleteVariableAsync_NotFound_ReturnsNotFound()
    {
        SetUserClaims("user-1");
        _variableService.DeleteAsync("user-1", 99, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.DeleteVariableAsync(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task DeleteVariableAsync_Deleted_ReturnsNoContent()
    {
        SetUserClaims("user-1");
        _variableService.DeleteAsync("user-1", 1, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.DeleteVariableAsync(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteVariableAsync_Deleted_CallsServiceWithCorrectArgs()
    {
        SetUserClaims("user-1");
        _variableService.DeleteAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _sut.DeleteVariableAsync(5, CancellationToken.None);

        await _variableService.Received(1).DeleteAsync("user-1", 5, Arg.Any<CancellationToken>());
    }
}
