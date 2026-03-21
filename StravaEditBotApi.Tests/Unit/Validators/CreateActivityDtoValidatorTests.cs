using FluentValidation.TestHelper;
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Validators;

namespace StravaEditBotApi.Tests.Unit.Validators;

[TestFixture]
public class CreateActivityDtoValidatorTests
{
    private CreateActivityDtoValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateActivityDtoValidator();
    }


    // Helper to reduce boilerplate — creates a valid DTO that you can
    // override one field at a time. This is a common pattern: start valid,
    // break one thing, verify that one thing fails.
    private static CreateActivityDto MakeValidDto(
        string? name = null,
        string? description = null,
        string? activitySport = null,
        DateTime? startTime = null,
        double? distance = null,
        TimeSpan? elapsedTime = null)
    {
        return new CreateActivityDto(
            Name: name ?? "Morning Run",
            Description: description ?? "A nice run",
            ActivitySport: activitySport ?? "Run",
            StartTime: startTime ?? DateTime.UtcNow.AddHours(-1),
            Distance: distance ?? 5.0,
            ElapsedTime: elapsedTime ?? TimeSpan.FromMinutes(30)
        );
    }

    // Happy path test: all fields valid
    [Test]
    public void ValidDto_PassesValidation()
    {
        var dto = MakeValidDto();

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    // Name
    [Test]
    public void Name_Empty_ShouldFail()
    {
        var dto = MakeValidDto(name: "");

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Name_ExceedsMaxLength_ShouldFail()
    {
        var dto = MakeValidDto(name: new string('a', 101));

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Name_ExactlyAtMaxLength_ShouldPass()
    {
        var dto = MakeValidDto(name: new string('a', 100));

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }
    
    // ActivitySport
    [TestCase("Run")]
    [TestCase("Ride")]
    [TestCase("Swim")]
    [TestCase("Walk")]
    [TestCase("Hike")]
    [TestCase("WeightTraining")]
    public void ActivitySport_ValidValue_ShouldPass(string sport)
    {
        var dto = MakeValidDto(activitySport: sport);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.ActivitySport);
    }

    [TestCase("")]           // empty string
    [TestCase("Surfing")]    // not in allowed list
    [TestCase("running")]    // case-sensitive
    [TestCase("RUN")]        // case-sensitive
    [TestCase(" Run")]       // leading space
    public void ActivitySport_InvalidValue_ShouldFail(string sport)
    {
        var dto = MakeValidDto(activitySport: sport);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.ActivitySport);
    }

    // Distance
    [TestCase(0)]
    [TestCase(500)]
    [TestCase(1000)]
    public void Distance_WithinRange_ShouldPass(double distance)
    {
        var dto = MakeValidDto(distance: distance);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Distance);
    }

    [TestCase(-0.1)]
    [TestCase(-100)]
    [TestCase(1000.1)]
    [TestCase(9999)]
    public void Distance_OutOfRange_ShouldFail(double distance)
    {
        var dto = MakeValidDto(distance: distance);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Distance);
    }

    // StartTime
    [Test]
    public void StartTime_InThePast_ShouldPass()
    {
        var dto = MakeValidDto(startTime: DateTime.UtcNow.AddHours(-1));

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.StartTime);
    }

    [Test]
    public void StartTime_InTheFuture_ShouldFail()
    {
        var dto = MakeValidDto(startTime: DateTime.UtcNow.AddDays(1));

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.StartTime);
    }

    // ElapsedTime
    [Test]
    public void ElapsedTime_Positive_ShouldPass()
    {
        var dto = MakeValidDto(elapsedTime: TimeSpan.FromMinutes(1));

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.ElapsedTime);
    }

    [Test]
    public void ElapsedTime_Zero_ShouldFail()
    {
        var dto = MakeValidDto(elapsedTime: TimeSpan.Zero);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.ElapsedTime);
    }

    [Test]
    public void ElapsedTime_Negative_ShouldFail()
    {
        var dto = MakeValidDto(elapsedTime: TimeSpan.FromMinutes(-5));

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.ElapsedTime);
    }

    // Description
    [Test]
    public void Description_Null_ShouldPass()
    {
        var dto = MakeValidDto(description: null);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Test]
    public void Description_ExceedsMaxLength_ShouldFail()
    {
        var dto = MakeValidDto(description: new string('a', 2001));

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    // You can also assert on the specific error message

    [Test]
    public void ActivitySport_Invalid_ShouldReturnMeaningfulMessage()
    {
        var dto = MakeValidDto(activitySport: "Surfing");

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.ActivitySport)
            .When(e => e.ErrorMessage.Contains("must be one of"), "Expected error message to contain 'must be one of'");
    }

}