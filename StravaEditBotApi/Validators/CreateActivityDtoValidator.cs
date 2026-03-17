using FluentValidation;
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Constants;

namespace StravaEditBotApi.Validators;

public class CreateActivityDtoValidator : AbstractValidator<CreateActivityDto>
{
    public CreateActivityDtoValidator()
    {

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.ActivitySport)
            .NotEmpty().WithMessage("Activity sport is required.")
            .Must(SportTypes.Valid.Contains)
                .WithMessage($"Activity sport must be one of: {SportTypes.FormattedList}.");

        RuleFor(x => x.StartTime)
            .Must(startTime => startTime <= DateTime.UtcNow)
                .WithMessage("Start time cannot be in the future.");

        RuleFor(x => x.Distance)
            .InclusiveBetween(0, 1000).WithMessage("Distance must be between 0 and 1000 kilometers.");

        RuleFor(x => x.ElapsedTime)
            .GreaterThan(TimeSpan.Zero).WithMessage("Elapsed time must be non-negative.");
    }
}