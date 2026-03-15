using FluentValidation;
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Constants;
namespace StravaEditBotApi.Validators;

public class UpdateActivityDtoValidator : AbstractValidator<UpdateActivityDto>
{
    public UpdateActivityDtoValidator()
    {
        // Only validate if a value was actually provided
        RuleFor(x => x.Name)
            .MaximumLength(200)
            .When(x => x.Name is not null);

        RuleFor(x => x.ActivitySport)
            .Must(sport => SportTypes.Valid.Contains(sport!))
                .WithMessage($"ActivitySport must be one of: {SportTypes.FormattedList}")
            .When(x => x.ActivitySport is not null);
    }
}