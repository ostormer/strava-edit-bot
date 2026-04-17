using FluentValidation;
using StravaEditBotApi.DTOs.Rulesets;

namespace StravaEditBotApi.Validators;

public class UpdateRulesetDtoValidator : AbstractValidator<UpdateRulesetDto>
{
    public UpdateRulesetDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.Name is not null);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);
    }
}
