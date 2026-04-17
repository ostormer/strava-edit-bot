using FluentValidation.TestHelper;
using StravaEditBotApi.DTOs.Variables;
using StravaEditBotApi.Models.Rules;
using StravaEditBotApi.Validators;

namespace StravaEditBotApi.Tests.Unit.Validators;

[TestFixture]
public class UpdateCustomVariableDtoValidatorTests
{
    private UpdateCustomVariableDtoValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateCustomVariableDtoValidator();

    // ========================================================
    // Happy paths — null fields are optional
    // ========================================================

    [Test]
    public void BothFieldsNull_PassesValidation()
    {
        _validator.TestValidate(MakeDto()).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void DescriptionOnly_PassesValidation()
    {
        _validator.TestValidate(MakeDto(description: "updated desc")).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void DefinitionOnly_PassesValidation()
    {
        _validator.TestValidate(MakeDto(definition: MakeDefinition())).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void BothFieldsSet_PassesValidation()
    {
        var dto = MakeDto(description: "updated desc", definition: MakeDefinition());
        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    // ========================================================
    // Description rules — only applied when non-null
    // ========================================================

    [Test]
    public void Description_ExactlyMaxLength_PassesValidation()
    {
        string description = new string('a', 500);
        _validator.TestValidate(MakeDto(description: description))
            .ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Test]
    public void Description_ExceedsMaxLength_FailsValidation()
    {
        string description = new string('a', 501);
        _validator.TestValidate(MakeDto(description: description))
            .ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Test]
    public void Description_Null_SkipsValidation()
    {
        _validator.TestValidate(MakeDto(description: null))
            .ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    // ========================================================
    // Definition.Cases rules — only applied when Definition non-null
    // ========================================================

    [Test]
    public void Definition_CasesEmpty_PassesValidation()
    {
        var definition = MakeDefinition(cases: []);
        _validator.TestValidate(MakeDto(definition: definition)).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Definition_CasesExactly20_PassesValidation()
    {
        var definition = MakeDefinition(cases: MakeCases(20));
        _validator.TestValidate(MakeDto(definition: definition)).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Definition_Cases21_FailsValidation()
    {
        var definition = MakeDefinition(cases: MakeCases(21));
        var result = _validator.TestValidate(MakeDto(definition: definition));
        result.ShouldHaveValidationErrorFor(x => x.Definition!.Cases);
    }

    [Test]
    public void Definition_Cases21_HasExpectedMessage()
    {
        var definition = MakeDefinition(cases: MakeCases(21));
        var result = _validator.TestValidate(MakeDto(definition: definition));
        result.ShouldHaveValidationErrorFor(x => x.Definition!.Cases)
            .WithErrorMessage("A variable may have at most 20 cases.");
    }

    // ========================================================
    // Definition.DefaultValue rules — only applied when Definition non-null
    // ========================================================

    [Test]
    public void Definition_DefaultValueExactlyMaxLength_PassesValidation()
    {
        string defaultValue = new string('a', 500);
        var definition = MakeDefinition(defaultValue: defaultValue);
        _validator.TestValidate(MakeDto(definition: definition))
            .ShouldNotHaveValidationErrorFor(x => x.Definition!.DefaultValue);
    }

    [Test]
    public void Definition_DefaultValueExceedsMaxLength_FailsValidation()
    {
        string defaultValue = new string('a', 501);
        var definition = MakeDefinition(defaultValue: defaultValue);
        _validator.TestValidate(MakeDto(definition: definition))
            .ShouldHaveValidationErrorFor(x => x.Definition!.DefaultValue);
    }

    [Test]
    public void Definition_Null_SkipsDefinitionRules()
    {
        var result = _validator.TestValidate(MakeDto(definition: null));
        result.ShouldNotHaveValidationErrorFor(x => x.Definition);
    }

    // ========================================================
    // Helpers
    // ========================================================

    private static UpdateCustomVariableDto MakeDto(
        string? description = null,
        CustomVariableDefinition? definition = null)
    {
        return new UpdateCustomVariableDto(description, definition);
    }

    private static CustomVariableDefinition MakeDefinition(
        string? name = null,
        List<VariableCase>? cases = null,
        string? defaultValue = null)
    {
        return new CustomVariableDefinition
        {
            Name = name ?? "x",
            Cases = cases ?? [],
            DefaultValue = defaultValue ?? "fallback",
        };
    }

    private static List<VariableCase> MakeCases(int count)
    {
        var cases = new List<VariableCase>(count);
        for (int i = 0; i < count; i++)
        {
            cases.Add(new VariableCase
            {
                Condition = new CheckFilter("distance", "gt", null),
                Output = $"output_{i}",
            });
        }

        return cases;
    }
}
