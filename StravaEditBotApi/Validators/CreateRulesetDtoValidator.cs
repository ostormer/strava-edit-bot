using FluentValidation;
using StravaEditBotApi.DTOs.Rulesets;

namespace StravaEditBotApi.Validators;

public class CreateRulesetDtoValidator : AbstractValidator<CreateRulesetDto>
{
    public CreateRulesetDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);
    }
}
