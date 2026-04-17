using FluentValidation;
using StravaEditBotApi.DTOs.Variables;

namespace StravaEditBotApi.Validators;

public class UpdateCustomVariableDtoValidator : AbstractValidator<UpdateCustomVariableDto>
{
    public UpdateCustomVariableDtoValidator()
    {
        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);

        When(x => x.Definition is not null, () =>
        {
            RuleFor(x => x.Definition!.Cases)
                .NotNull()
                .Must(c => c.Count <= 20)
                .WithMessage("A variable may have at most 20 cases.");

            RuleFor(x => x.Definition!.DefaultValue)
                .NotNull()
                .MaximumLength(500);
        });
    }
}
