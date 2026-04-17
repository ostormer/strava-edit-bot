using FluentValidation.TestHelper;
using StravaEditBotApi.DTOs.Templates;
using StravaEditBotApi.Validators;

namespace StravaEditBotApi.Tests.Unit.Validators;

[TestFixture]
public class CreateTemplateFromRulesetDtoValidatorTests
{
    private CreateTemplateFromRulesetDtoValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateTemplateFromRulesetDtoValidator();

    // ========================================================
    // Factory helpers
    // ========================================================

    private static CreateTemplateFromRulesetDto MakeValidDto(
        string? name = null,
        string? description = null,
        bool isPublic = false)
    {
        return new CreateTemplateFromRulesetDto(
            Name: name ?? "My Template",
            Description: description,
            IsPublic: isPublic
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
    public void ValidDto_WithDescription_PassesValidation()
    {
        _validator.TestValidate(MakeValidDto(description: "A useful template")).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void ValidDto_NullDescription_PassesValidation()
    {
        _validator.TestValidate(MakeValidDto(description: null)).ShouldNotHaveAnyValidationErrors();
    }

    // ========================================================
    // Name — NotEmpty
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

    // ========================================================
    // Name — MaximumLength(200)
    // ========================================================

    [TestCase(1)]
    [TestCase(100)]
    [TestCase(200)]
    public void Name_AtOrUnderMaxLength_ShouldPass(int length)
    {
        var result = _validator.TestValidate(MakeValidDto(name: new string('a', length)));
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [TestCase(201)]
    [TestCase(500)]
    public void Name_ExceedsMaxLength_ShouldFail(int length)
    {
        var result = _validator.TestValidate(MakeValidDto(name: new string('a', length)));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    // ========================================================
    // Description — MaximumLength(2000) when not null
    // ========================================================

    [TestCase(1)]
    [TestCase(1000)]
    [TestCase(2000)]
    public void Description_AtOrUnderMaxLength_ShouldPass(int length)
    {
        var result = _validator.TestValidate(MakeValidDto(description: new string('d', length)));
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [TestCase(2001)]
    [TestCase(5000)]
    public void Description_ExceedsMaxLength_ShouldFail(int length)
    {
        var result = _validator.TestValidate(MakeValidDto(description: new string('d', length)));
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Test]
    public void Description_Null_ShouldNotValidateLength()
    {
        var result = _validator.TestValidate(MakeValidDto(description: null));
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }
}
