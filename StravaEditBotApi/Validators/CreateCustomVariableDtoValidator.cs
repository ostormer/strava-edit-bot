using FluentValidation;
using StravaEditBotApi.DTOs.Variables;

namespace StravaEditBotApi.Validators;

public class CreateCustomVariableDtoValidator : AbstractValidator<CreateCustomVariableDto>
{
    public CreateCustomVariableDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50)
            .Matches(@"^[a-z][a-z0-9_]*$")
            .WithMessage("Variable name must start with a lowercase letter and contain only lowercase letters, digits, and underscores.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);

        RuleFor(x => x.Definition)
            .NotNull();

        RuleFor(x => x.Definition.Cases)
            .NotNull()
            .Must(c => c.Count <= 20)
            .WithMessage("A variable may have at most 20 cases.")
            .When(x => x.Definition is not null);

        RuleFor(x => x.Definition.DefaultValue)
            .NotNull()
            .MaximumLength(500)
            .When(x => x.Definition is not null);
    }
}
