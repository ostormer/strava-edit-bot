using FluentValidation.TestHelper;
using StravaEditBotApi.DTOs.Variables;
using StravaEditBotApi.Models.Rules;
using StravaEditBotApi.Validators;

namespace StravaEditBotApi.Tests.Unit.Validators;

[TestFixture]
public class CreateCustomVariableDtoValidatorTests
{
    private CreateCustomVariableDtoValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateCustomVariableDtoValidator();

    // ========================================================
    // Factory helpers
    // ========================================================

    private static CreateCustomVariableDto MakeValidDto(
        string? name = null,
        string? description = null,
        CustomVariableDefinition? definition = null)
    {
        return new CreateCustomVariableDto(
            Name: name ?? "pace_label",
            Description: description,
            Definition: definition ?? MakeValidDefinition()
        );
    }

    private static VariableCase MakeVariableCase() =>
        new VariableCase
        {
            Condition = new CheckFilter("distance", "gt", null),
            Output = "Fast"
        };

    private static CustomVariableDefinition MakeValidDefinition(
        string? defName = null,
        List<VariableCase>? cases = null,
        string? defaultValue = null)
    {
        return new CustomVariableDefinition
        {
            Name = defName ?? "pace_label",
            Cases = cases ?? [],
            DefaultValue = defaultValue ?? "Slow"
        };
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
        _validator.TestValidate(MakeValidDto(description: "A useful variable")).ShouldNotHaveAnyValidationErrors();
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
    // Name — MaximumLength(50)
    // ========================================================

    [TestCase(1)]
    [TestCase(25)]
    [TestCase(50)]
    public void Name_AtOrUnderMaxLength_ShouldPass(int length)
    {
        string name = "a" + new string('b', length - 1);
        var result = _validator.TestValidate(MakeValidDto(name: name));
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [TestCase(51)]
    [TestCase(100)]
    public void Name_ExceedsMaxLength_ShouldFail(int length)
    {
        string name = "a" + new string('b', length - 1);
        var result = _validator.TestValidate(MakeValidDto(name: name));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    // ========================================================
    // Name — Matches ^[a-z][a-z0-9_]*$
    // ========================================================

    [TestCase("a")]
    [TestCase("pace_label")]
    [TestCase("v1")]
    [TestCase("my_var_123")]
    public void Name_ValidPattern_ShouldPass(string name)
    {
        var result = _validator.TestValidate(MakeValidDto(name: name));
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [TestCase("1starts_with_digit", TestName = "Name_StartsWithDigit_ShouldFail")]
    [TestCase("HasUppercase", TestName = "Name_ContainsUppercase_ShouldFail")]
    [TestCase("has space", TestName = "Name_ContainsSpace_ShouldFail")]
    [TestCase("has-hyphen", TestName = "Name_ContainsHyphen_ShouldFail")]
    [TestCase("_starts_with_underscore", TestName = "Name_StartsWithUnderscore_ShouldFail")]
    public void Name_InvalidPattern_ShouldFail(string name)
    {
        var result = _validator.TestValidate(MakeValidDto(name: name));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Name_InvalidPattern_ShouldReturnMeaningfulMessage()
    {
        var result = _validator.TestValidate(MakeValidDto(name: "InvalidName"));
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Variable name must start with a lowercase letter and contain only lowercase letters, digits, and underscores.");
    }

    // ========================================================
    // Description — MaximumLength(500) when not null
    // ========================================================

    [TestCase(1)]
    [TestCase(250)]
    [TestCase(500)]
    public void Description_AtOrUnderMaxLength_ShouldPass(int length)
    {
        var result = _validator.TestValidate(MakeValidDto(description: new string('d', length)));
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [TestCase(501)]
    [TestCase(1000)]
    public void Description_ExceedsMaxLength_ShouldFail(int length)
    {
        var result = _validator.TestValidate(MakeValidDto(description: new string('d', length)));
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Test]
    public void Description_Null_ShouldSkipLengthValidation()
    {
        var result = _validator.TestValidate(MakeValidDto(description: null));
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    // ========================================================
    // Definition — NotNull
    // ========================================================

    [Test]
    public void Definition_Null_ShouldFail()
    {
        var dto = new CreateCustomVariableDto(
            Name: "pace_label",
            Description: null,
            Definition: null!
        );
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Definition);
    }

    // ========================================================
    // Definition.Cases — NotNull, Count <= 20
    // ========================================================

    [TestCase(0)]
    [TestCase(10)]
    [TestCase(20)]
    public void Definition_Cases_AtOrUnderLimit_ShouldPass(int caseCount)
    {
        var cases = Enumerable.Range(0, caseCount)
            .Select(_ => MakeVariableCase())
            .ToList();

        var result = _validator.TestValidate(MakeValidDto(definition: MakeValidDefinition(cases: cases)));
        result.ShouldNotHaveValidationErrorFor(x => x.Definition.Cases);
    }

    [TestCase(21)]
    [TestCase(50)]
    public void Definition_Cases_ExceedsLimit_ShouldFail(int caseCount)
    {
        var cases = Enumerable.Range(0, caseCount)
            .Select(_ => MakeVariableCase())
            .ToList();

        var result = _validator.TestValidate(MakeValidDto(definition: MakeValidDefinition(cases: cases)));
        result.ShouldHaveValidationErrorFor(x => x.Definition.Cases)
            .WithErrorMessage("A variable may have at most 20 cases.");
    }

    // ========================================================
    // Definition.DefaultValue — MaximumLength(500)
    // ========================================================

    [Test]
    public void Definition_DefaultValue_Empty_ShouldPass()
    {
        // The validator only enforces MaximumLength on DefaultValue (NotNull is a no-op
        // because DefaultValue is a non-nullable string — the type system enforces non-null).
        var result = _validator.TestValidate(MakeValidDto(definition: MakeValidDefinition(defaultValue: "")));
        result.ShouldNotHaveValidationErrorFor(x => x.Definition.DefaultValue);
    }

    [TestCase(1)]
    [TestCase(250)]
    [TestCase(500)]
    public void Definition_DefaultValue_AtOrUnderMaxLength_ShouldPass(int length)
    {
        var result = _validator.TestValidate(MakeValidDto(definition: MakeValidDefinition(defaultValue: new string('v', length))));
        result.ShouldNotHaveValidationErrorFor(x => x.Definition.DefaultValue);
    }

    [TestCase(501)]
    [TestCase(1000)]
    public void Definition_DefaultValue_ExceedsMaxLength_ShouldFail(int length)
    {
        var result = _validator.TestValidate(MakeValidDto(definition: MakeValidDefinition(defaultValue: new string('v', length))));
        result.ShouldHaveValidationErrorFor(x => x.Definition.DefaultValue);
    }
}
