using FluentValidation.TestHelper;
using StravaEditBotApi.DTOs.Rulesets;
using StravaEditBotApi.Validators;

namespace StravaEditBotApi.Tests.Unit.Validators;

[TestFixture]
public class CreateRulesetDtoValidatorTests
{
    private CreateRulesetDtoValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateRulesetDtoValidator();

    // ========================================================
    // Factory helpers
    // ========================================================

    private static CreateRulesetDto MakeValidDto(
        string? name = null,
        string? description = null,
        bool? isEnabled = null)
    {
        return new CreateRulesetDto(
            Name: name ?? "My Ruleset",
            Description: description,
            Filter: null,
            Effect: null,
            IsEnabled: isEnabled ?? true
        );
    }

    // ========================================================
    // Happy path
    // ========================================================

    [Test]
    public void ValidDto_PassesValidation()
    {
        _validator.TestValidate(MakeValidDto()).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void ValidDto_NullDescription_PassesValidation()
    {
        var result = _validator.TestValidate(MakeValidDto(description: null));
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    // ========================================================
    // Name
    // ========================================================

    [Test]
    public void Name_Empty_ShouldFail()
    {
        var result = _validator.TestValidate(MakeValidDto(name: ""));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Name_Whitespace_ShouldFail()
    {
        var result = _validator.TestValidate(MakeValidDto(name: "   "));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Name_ExactlyMaxLength_ShouldPass()
    {
        string name = new('a', 200);
        var result = _validator.TestValidate(MakeValidDto(name: name));
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Name_ExceedsMaxLength_ShouldFail()
    {
        string name = new('a', 201);
        var result = _validator.TestValidate(MakeValidDto(name: name));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [TestCase(1)]
    [TestCase(100)]
    [TestCase(200)]
    public void Name_WithinMaxLength_ShouldPass(int length)
    {
        string name = new('a', length);
        var result = _validator.TestValidate(MakeValidDto(name: name));
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // ========================================================
    // Description
    // ========================================================

    [Test]
    public void Description_ExactlyMaxLength_ShouldPass()
    {
        string description = new('a', 2000);
        var result = _validator.TestValidate(MakeValidDto(description: description));
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Test]
    public void Description_ExceedsMaxLength_ShouldFail()
    {
        string description = new('a', 2001);
        var result = _validator.TestValidate(MakeValidDto(description: description));
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [TestCase(1)]
    [TestCase(1000)]
    [TestCase(2000)]
    public void Description_WithinMaxLength_ShouldPass(int length)
    {
        string description = new('a', length);
        var result = _validator.TestValidate(MakeValidDto(description: description));
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }
}
