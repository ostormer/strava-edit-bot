using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using StravaEditBotApi.Controllers;
using StravaEditBotApi.DTOs.Rulesets;
using StravaEditBotApi.DTOs.Templates;
using StravaEditBotApi.Models.Rules;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Tests.Unit.Controllers;

[TestFixture]
public class RulesetsControllerTests
{
    private IRulesetService _rulesetService = null!;
    private IRulesetValidator _validator = null!;
    private RulesetsController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _rulesetService = Substitute.For<IRulesetService>();
        _validator = Substitute.For<IRulesetValidator>();
        _sut = new RulesetsController(_rulesetService, _validator);
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

    private static RulesetResponseDto MakeRulesetResponse(
        int id = 1,
        string name = "Test Ruleset") =>
        new(
            Id: id,
            Name: name,
            Description: null,
            Priority: 0,
            IsEnabled: true,
            IsValid: false,
            Filter: null,
            Effect: null,
            CreatedFromTemplateId: null,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            ValidationErrors: []
        );

    private static CreateRulesetDto MakeCreateDto(string name = "Test Ruleset") =>
        new(
            Name: name,
            Description: null,
            Filter: null,
            Effect: null,
            IsEnabled: true
        );

    private static UpdateRulesetDto MakeUpdateDto(string? name = "Updated Ruleset") =>
        new(
            Name: name,
            Description: null,
            Filter: null,
            Effect: null,
            IsEnabled: null
        );

    private static RulesetTemplateResponseDto MakeTemplateResponse(int id = 10) =>
        new(
            Id: id,
            Name: "Shared Template",
            Description: null,
            Filter: null,
            Effect: null,
            IsPublic: false,
            ShareToken: "abc-token",
            UsageCount: 0,
            BundledVariables: null,
            CreatedAt: DateTime.UtcNow,
            SanitizedProperties: null
        );

    // ========================================================
    // GetRulesetsAsync
    // ========================================================

    [Test]
    public async Task GetRulesetsAsync_MissingClaim_ReturnsUnauthorized()
    {
        SetUserClaims(null);

        var result = await _sut.GetRulesetsAsync(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task GetRulesetsAsync_MissingClaim_DoesNotCallService()
    {
        SetUserClaims(null);

        await _sut.GetRulesetsAsync(CancellationToken.None);

        await _rulesetService.DidNotReceive().GetUserRulesetsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetRulesetsAsync_ValidUser_ReturnsOk()
    {
        SetUserClaims("user-1");
        _rulesetService.GetUserRulesetsAsync("user-1", Arg.Any<CancellationToken>())
            .Returns([MakeRulesetResponse()]);

        var result = await _sut.GetRulesetsAsync(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetRulesetsAsync_ValidUser_CallsServiceWithUserId()
    {
        SetUserClaims("user-42");
        _rulesetService.GetUserRulesetsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await _sut.GetRulesetsAsync(CancellationToken.None);

        await _rulesetService.Received(1).GetUserRulesetsAsync("user-42", Arg.Any<CancellationToken>());
    }

    // ========================================================
    // GetRulesetAsync
    // ========================================================

    [Test]
    public async Task GetRulesetAsync_MissingClaim_ReturnsUnauthorized()
    {
        SetUserClaims(null);

        var result = await _sut.GetRulesetAsync(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task GetRulesetAsync_NotFound_ReturnsNotFound()
    {
        SetUserClaims("user-1");
        _rulesetService.GetByIdAsync("user-1", 99, Arg.Any<CancellationToken>())
            .Returns((RulesetResponseDto?)null);

        var result = await _sut.GetRulesetAsync(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetRulesetAsync_Found_ReturnsOk()
    {
        SetUserClaims("user-1");
        _rulesetService.GetByIdAsync("user-1", 1, Arg.Any<CancellationToken>())
            .Returns(MakeRulesetResponse(id: 1));

        var result = await _sut.GetRulesetAsync(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetRulesetAsync_Found_ReturnsCorrectDto()
    {
        SetUserClaims("user-1");
        var expected = MakeRulesetResponse(id: 5, name: "My Ruleset");
        _rulesetService.GetByIdAsync("user-1", 5, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.GetRulesetAsync(5, CancellationToken.None);

        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.EqualTo(expected));
    }

    // ========================================================
    // CreateRulesetAsync
    // ========================================================

    [Test]
    public async Task CreateRulesetAsync_MissingClaim_ReturnsUnauthorized()
    {
        SetUserClaims(null);

        var result = await _sut.CreateRulesetAsync(MakeCreateDto(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task CreateRulesetAsync_ValidDto_ReturnsCreatedAtAction()
    {
        SetUserClaims("user-1");
        var dto = MakeCreateDto("New Ruleset");
        _rulesetService.CreateAsync("user-1", dto, Arg.Any<CancellationToken>())
            .Returns(MakeRulesetResponse(id: 7, name: "New Ruleset"));

        var result = await _sut.CreateRulesetAsync(dto, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task CreateRulesetAsync_ValidDto_ReturnsCorrectRouteAndBody()
    {
        SetUserClaims("user-1");
        var dto = MakeCreateDto();
        var created = MakeRulesetResponse(id: 7);
        _rulesetService.CreateAsync(Arg.Any<string>(), Arg.Any<CreateRulesetDto>(), Arg.Any<CancellationToken>())
            .Returns(created);

        var result = await _sut.CreateRulesetAsync(dto, CancellationToken.None);

        var r = (CreatedAtActionResult)result;
        Assert.That(r.StatusCode, Is.EqualTo(201));
        Assert.That(r.ActionName, Is.EqualTo(nameof(RulesetsController.GetRulesetAsync)));
        Assert.That(r.RouteValues!["id"], Is.EqualTo(7));
        Assert.That(r.Value, Is.EqualTo(created));
    }

    [Test]
    public async Task CreateRulesetAsync_ValidDto_CallsServiceExactlyOnce()
    {
        SetUserClaims("user-1");
        var dto = MakeCreateDto();
        _rulesetService.CreateAsync(Arg.Any<string>(), Arg.Any<CreateRulesetDto>(), Arg.Any<CancellationToken>())
            .Returns(MakeRulesetResponse());

        await _sut.CreateRulesetAsync(dto, CancellationToken.None);

        await _rulesetService.Received(1).CreateAsync("user-1", dto, Arg.Any<CancellationToken>());
    }

    // ========================================================
    // UpdateRulesetAsync
    // ========================================================

    [Test]
    public async Task UpdateRulesetAsync_MissingClaim_ReturnsUnauthorized()
    {
        SetUserClaims(null);

        var result = await _sut.UpdateRulesetAsync(1, MakeUpdateDto(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task UpdateRulesetAsync_NotFound_ReturnsNotFound()
    {
        SetUserClaims("user-1");
        _rulesetService.UpdateAsync("user-1", 99, Arg.Any<UpdateRulesetDto>(), Arg.Any<CancellationToken>())
            .Returns((RulesetResponseDto?)null);

        var result = await _sut.UpdateRulesetAsync(99, MakeUpdateDto(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task UpdateRulesetAsync_Found_ReturnsOk()
    {
        SetUserClaims("user-1");
        _rulesetService.UpdateAsync("user-1", 1, Arg.Any<UpdateRulesetDto>(), Arg.Any<CancellationToken>())
            .Returns(MakeRulesetResponse(id: 1));

        var result = await _sut.UpdateRulesetAsync(1, MakeUpdateDto(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task UpdateRulesetAsync_Found_ReturnsUpdatedDto()
    {
        SetUserClaims("user-1");
        var updated = MakeRulesetResponse(id: 1, name: "Updated Name");
        _rulesetService.UpdateAsync("user-1", 1, Arg.Any<UpdateRulesetDto>(), Arg.Any<CancellationToken>())
            .Returns(updated);

        var result = await _sut.UpdateRulesetAsync(1, MakeUpdateDto(), CancellationToken.None);

        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.EqualTo(updated));
    }

    // ========================================================
    // DeleteRulesetAsync
    // ========================================================

    [Test]
    public async Task DeleteRulesetAsync_MissingClaim_ReturnsUnauthorized()
    {
        SetUserClaims(null);

        var result = await _sut.DeleteRulesetAsync(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task DeleteRulesetAsync_NotFound_ReturnsNotFound()
    {
        SetUserClaims("user-1");
        _rulesetService.DeleteAsync("user-1", 99, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.DeleteRulesetAsync(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task DeleteRulesetAsync_Deleted_ReturnsNoContent()
    {
        SetUserClaims("user-1");
        _rulesetService.DeleteAsync("user-1", 1, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.DeleteRulesetAsync(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteRulesetAsync_Deleted_CallsServiceWithCorrectArgs()
    {
        SetUserClaims("user-1");
        _rulesetService.DeleteAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _sut.DeleteRulesetAsync(5, CancellationToken.None);

        await _rulesetService.Received(1).DeleteAsync("user-1", 5, Arg.Any<CancellationToken>());
    }

    // ========================================================
    // ReorderRulesetsAsync
    // ========================================================

    [Test]
    public async Task ReorderRulesetsAsync_MissingClaim_ReturnsUnauthorized()
    {
        SetUserClaims(null);

        var result = await _sut.ReorderRulesetsAsync(new ReorderRulesetsDto([1, 2, 3]), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task ReorderRulesetsAsync_ServiceReturnsNull_ReturnsBadRequest()
    {
        SetUserClaims("user-1");
        _rulesetService.ReorderAsync("user-1", Arg.Any<ReorderRulesetsDto>(), Arg.Any<CancellationToken>())
            .Returns((List<RulesetResponseDto>?)null);

        var result = await _sut.ReorderRulesetsAsync(new ReorderRulesetsDto([1, 2]), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task ReorderRulesetsAsync_ValidOrder_ReturnsOk()
    {
        SetUserClaims("user-1");
        var reordered = new List<RulesetResponseDto> { MakeRulesetResponse(id: 2), MakeRulesetResponse(id: 1) };
        _rulesetService.ReorderAsync("user-1", Arg.Any<ReorderRulesetsDto>(), Arg.Any<CancellationToken>())
            .Returns(reordered);

        var result = await _sut.ReorderRulesetsAsync(new ReorderRulesetsDto([2, 1]), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task ReorderRulesetsAsync_ValidOrder_CallsServiceExactlyOnce()
    {
        SetUserClaims("user-1");
        _rulesetService.ReorderAsync(Arg.Any<string>(), Arg.Any<ReorderRulesetsDto>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await _sut.ReorderRulesetsAsync(new ReorderRulesetsDto([1]), CancellationToken.None);

        await _rulesetService.Received(1).ReorderAsync(Arg.Any<string>(), Arg.Any<ReorderRulesetsDto>(), Arg.Any<CancellationToken>());
    }

    // ========================================================
    // ToggleEnabledAsync
    // ========================================================

    [Test]
    public async Task ToggleEnabledAsync_MissingClaim_ReturnsUnauthorized()
    {
        SetUserClaims(null);

        var result = await _sut.ToggleEnabledAsync(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task ToggleEnabledAsync_NotFound_ReturnsNotFound()
    {
        SetUserClaims("user-1");
        _rulesetService.ToggleEnabledAsync("user-1", 99, Arg.Any<CancellationToken>())
            .Returns((RulesetResponseDto?)null);

        var result = await _sut.ToggleEnabledAsync(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task ToggleEnabledAsync_Found_ReturnsOk()
    {
        SetUserClaims("user-1");
        _rulesetService.ToggleEnabledAsync("user-1", 1, Arg.Any<CancellationToken>())
            .Returns(MakeRulesetResponse(id: 1));

        var result = await _sut.ToggleEnabledAsync(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task ToggleEnabledAsync_Found_CallsServiceWithCorrectArgs()
    {
        SetUserClaims("user-1");
        _rulesetService.ToggleEnabledAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(MakeRulesetResponse());

        await _sut.ToggleEnabledAsync(3, CancellationToken.None);

        await _rulesetService.Received(1).ToggleEnabledAsync("user-1", 3, Arg.Any<CancellationToken>());
    }

    // ========================================================
    // ShareRulesetAsync
    // ========================================================

    [Test]
    public async Task ShareRulesetAsync_MissingClaim_ReturnsUnauthorized()
    {
        SetUserClaims(null);

        var result = await _sut.ShareRulesetAsync(1, new CreateTemplateFromRulesetDto("Name", null, false), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task ShareRulesetAsync_NotFound_ReturnsNotFound()
    {
        SetUserClaims("user-1");
        (RulesetTemplateResponseDto Template, List<string> SanitizedProperties)? nullResult = null;
        _rulesetService.ShareAsync("user-1", 99, Arg.Any<CreateTemplateFromRulesetDto>(), Arg.Any<CancellationToken>())
            .Returns(nullResult);

        var result = await _sut.ShareRulesetAsync(99, new CreateTemplateFromRulesetDto("Name", null, false), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task ShareRulesetAsync_Found_ReturnsOkWithTemplate()
    {
        SetUserClaims("user-1");
        var template = MakeTemplateResponse(id: 10);
        (RulesetTemplateResponseDto Template, List<string> SanitizedProperties)? shareResult = (template, []);
        _rulesetService.ShareAsync("user-1", 1, Arg.Any<CreateTemplateFromRulesetDto>(), Arg.Any<CancellationToken>())
            .Returns(shareResult);

        var result = await _sut.ShareRulesetAsync(1, new CreateTemplateFromRulesetDto("Name", null, false), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.EqualTo(template));
    }

    // ========================================================
    // ValidateRuleset
    // ========================================================

    [Test]
    public void ValidateRuleset_ReturnsOkWithValidationResult()
    {
        var dto = new ValidateRulesetDto(Filter: null, Effect: null);
        var validationResult = new RulesetValidationResult(IsValid: false, Errors: [new RulesetValidationError("filter", "filter_required", "Filter is required.")]);
        _validator.Validate(null, null).Returns(validationResult);

        var result = _sut.ValidateRuleset(dto);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.EqualTo(validationResult));
    }

    [Test]
    public void ValidateRuleset_CallsValidatorWithDtoValues()
    {
        var filter = new CheckFilter("name", "contains", null);
        var dto = new ValidateRulesetDto(Filter: filter, Effect: null);
        _validator.Validate(Arg.Any<FilterExpression?>(), Arg.Any<RulesetEffect?>())
            .Returns(new RulesetValidationResult(true, []));

        _sut.ValidateRuleset(dto);

        _validator.Received(1).Validate(filter, null);
    }

    [Test]
    public void ValidateRuleset_ValidResult_ReturnsIsValidTrue()
    {
        var dto = new ValidateRulesetDto(Filter: null, Effect: null);
        _validator.Validate(null, null).Returns(new RulesetValidationResult(true, []));

        var result = _sut.ValidateRuleset(dto);

        var ok = (OkObjectResult)result;
        var validationResult = (RulesetValidationResult)ok.Value!;
        Assert.That(validationResult.IsValid, Is.True);
        Assert.That(validationResult.Errors, Is.Empty);
    }
}
