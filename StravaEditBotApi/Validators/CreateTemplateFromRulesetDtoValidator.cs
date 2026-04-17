using FluentValidation;
using StravaEditBotApi.DTOs.Templates;

namespace StravaEditBotApi.Validators;

public class CreateTemplateFromRulesetDtoValidator : AbstractValidator<CreateTemplateFromRulesetDto>
{
    public CreateTemplateFromRulesetDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);
    }
}
