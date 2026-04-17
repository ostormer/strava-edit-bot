using FluentValidation.TestHelper;
using StravaEditBotApi.DTOs.Rulesets;
using StravaEditBotApi.Validators;

namespace StravaEditBotApi.Tests.Unit.Validators;

[TestFixture]
public class UpdateRulesetDtoValidatorTests
{
    private UpdateRulesetDtoValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateRulesetDtoValidator();

    // ========================================================
    // Factory helpers
    // ========================================================

    private static UpdateRulesetDto MakeDto(
        string? name = "Morning Run",
        string? description = "A nice run",
        bool? isEnabled = true)
    {
        return new UpdateRulesetDto(
            Name: name,
            Description: description,
            Filter: null,
            Effect: null,
            IsEnabled: isEnabled
        );
    }

    private static UpdateRulesetDto MakeAllNullDto()
    {
        return new UpdateRulesetDto(
            Name: null,
            Description: null,
            Filter: null,
            Effect: null,
            IsEnabled: null
        );
    }

    // ========================================================
    // Happy path
    // ========================================================

    [Test]
    public void ValidDto_FullyPopulated_PassesValidation()
    {
        _validator.TestValidate(MakeDto()).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void ValidDto_AllNull_PassesValidation()
    {
        _validator.TestValidate(MakeAllNullDto()).ShouldNotHaveAnyValidationErrors();
    }

    // ========================================================
    // Name — NotEmpty (when non-null)
    // ========================================================

    [Test]
    public void Name_Empty_ShouldFail()
    {
        var result = _validator.TestValidate(MakeDto(name: ""));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Name_Whitespace_ShouldFail()
    {
        var result = _validator.TestValidate(MakeDto(name: "   "));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Name_Null_SkipsValidation()
    {
        var result = _validator.TestValidate(MakeDto(name: null));
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // ========================================================
    // Name — MaximumLength(200)
    // ========================================================

    [TestCase(1)]
    [TestCase(100)]
    [TestCase(200)]
    public void Name_AtOrUnderMaxLength_ShouldPass(int length)
    {
        var result = _validator.TestValidate(MakeDto(name: new string('a', length)));
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [TestCase(201)]
    [TestCase(500)]
    public void Name_ExceedsMaxLength_ShouldFail(int length)
    {
        var result = _validator.TestValidate(MakeDto(name: new string('a', length)));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    // ========================================================
    // Description — MaximumLength(2000) when non-null
    // ========================================================

    [Test]
    public void Description_Null_SkipsValidation()
    {
        var result = _validator.TestValidate(MakeDto(description: null));
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [TestCase(1)]
    [TestCase(1000)]
    [TestCase(2000)]
    public void Description_AtOrUnderMaxLength_ShouldPass(int length)
    {
        var result = _validator.TestValidate(MakeDto(description: new string('d', length)));
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [TestCase(2001)]
    [TestCase(5000)]
    public void Description_ExceedsMaxLength_ShouldFail(int length)
    {
        var result = _validator.TestValidate(MakeDto(description: new string('d', length)));
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    // ========================================================
    // Fields with no validation rules (Filter, Effect, IsEnabled)
    // ========================================================

    [Test]
    public void IsEnabled_Null_PassesValidation()
    {
        var result = _validator.TestValidate(MakeDto(isEnabled: null));
        result.ShouldNotHaveValidationErrorFor(x => x.IsEnabled);
    }

    [Test]
    public void IsEnabled_False_PassesValidation()
    {
        var result = _validator.TestValidate(MakeDto(isEnabled: false));
        result.ShouldNotHaveValidationErrorFor(x => x.IsEnabled);
    }
}
