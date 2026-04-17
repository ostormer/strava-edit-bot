using System.Text.Json;
using StravaEditBotApi.Models.Rules;
using StravaEditBotApi.Services.Auth;
using StravaEditBotApi.Services.Rulesets;
using StravaEditBotApi.Services.Webhook;

namespace StravaEditBotApi.Tests.Unit.Services;

[TestFixture]
public class RulesetValidatorTests
{
    private RulesetValidator _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new RulesetValidator();

    // ========================================================
    // Factory helpers
    // ========================================================

    private static CheckFilter MakeCheckFilter(
        string? property = "sport_type",
        string? op = "in",
        JsonElement? value = null)
    {
        JsonElement resolvedValue = value ?? JsonSerializer.SerializeToElement(new[] { "Run" });
        return new CheckFilter(property, op, resolvedValue);
    }

    private static AndFilter MakeValidAndFilter() =>
        new([MakeCheckFilter()]);

    private static RulesetEffect MakeValidEffect(string? name = null) =>
        new() { Name = name ?? "Morning run" };

    // ========================================================
    // Validate — null filter / effect
    // ========================================================

    [Test]
    public void Validate_NullFilter_ReturnsFilterRequiredError()
    {
        var result = _sut.Validate(null, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("filter_required"));
        Assert.That(result.Errors[0].Path, Is.EqualTo("filter"));
    }

    [Test]
    public void Validate_NullEffect_ReturnsEffectRequiredError()
    {
        var result = _sut.Validate(MakeValidAndFilter(), null);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("effect_required"));
        Assert.That(result.Errors[0].Path, Is.EqualTo("effect"));
    }

    [Test]
    public void Validate_BothNull_ReturnsBothErrors()
    {
        var result = _sut.Validate(null, null);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Count.EqualTo(2));
        Assert.That(result.Errors.Select(e => e.Code),
            Is.EquivalentTo(new[] { "filter_required", "effect_required" }));
    }

    // ========================================================
    // Validate — filter_empty
    // ========================================================

    [Test]
    public void Validate_AndFilterWithEmptyConditions_ReturnsFilterEmptyError()
    {
        var result = _sut.Validate(new AndFilter([]), MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("filter_empty"));
    }

    [Test]
    public void Validate_OrFilterWithEmptyConditions_ReturnsFilterEmptyError()
    {
        var result = _sut.Validate(new OrFilter([]), MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("filter_empty"));
    }

    // ========================================================
    // Validate — incomplete_check
    // ========================================================

    [Test]
    public void Validate_CheckFilterWithNullProperty_ReturnsIncompleteCheckError()
    {
        var filter = new AndFilter([new CheckFilter(null, "in", JsonSerializer.SerializeToElement(new[] { "Run" }))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("incomplete_check"));
        Assert.That(result.Errors[0].Path, Does.EndWith(".property"));
    }

    [Test]
    public void Validate_CheckFilterWithNullOperator_ReturnsIncompleteCheckError()
    {
        var filter = new AndFilter([new CheckFilter("sport_type", null, JsonSerializer.SerializeToElement(new[] { "Run" }))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("incomplete_check"));
        Assert.That(result.Errors[0].Path, Does.EndWith(".operator"));
    }

    [Test]
    public void Validate_CheckFilterWithNullValue_ReturnsIncompleteCheckError()
    {
        var filter = new AndFilter([new CheckFilter("sport_type", "in", null)]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("incomplete_check"));
        Assert.That(result.Errors[0].Path, Does.EndWith(".value"));
    }

    // ========================================================
    // Validate — unknown_property
    // ========================================================

    [Test]
    public void Validate_CheckFilterWithUnknownProperty_ReturnsUnknownPropertyError()
    {
        var filter = new AndFilter([new CheckFilter("magic_field", "eq", JsonSerializer.SerializeToElement("hello"))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("unknown_property"));
        Assert.That(result.Errors[0].Path, Does.EndWith(".property"));
    }

    // ========================================================
    // Validate — invalid_operator
    // ========================================================

    [Test]
    public void Validate_CheckFilterWithInvalidOperatorForProperty_ReturnsInvalidOperatorError()
    {
        // sport_type only supports "in" and "not_in" — "gt" is invalid
        var filter = new AndFilter([new CheckFilter("sport_type", "gt", JsonSerializer.SerializeToElement(new[] { "Run" }))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("invalid_operator"));
        Assert.That(result.Errors[0].Path, Does.EndWith(".operator"));
    }

    // ========================================================
    // Validate — nested NotFilter path propagation
    // ========================================================

    [Test]
    public void Validate_NotFilterWrappingIncompleteCheck_ErrorPathContainsConditionSegment()
    {
        var incompleteCheck = new CheckFilter(null, null, null);
        var filter = new NotFilter(incompleteCheck);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Path, Does.Contain(".condition."));
    }

    // ========================================================
    // Validate — max_depth_exceeded
    // ========================================================

    [Test]
    public void Validate_FilterExceedingMaxDepth_ReturnsMaxDepthExceededError()
    {
        // Build a chain of 12 nested NotFilters — beyond the limit of 10
        FilterExpression deepFilter = MakeCheckFilter();
        for (int i = 0; i < 12; i++)
        {
            deepFilter = new NotFilter(deepFilter);
        }

        var result = _sut.Validate(deepFilter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.Code == "max_depth_exceeded"), Is.True);
    }

    // ========================================================
    // Validate — effect_empty
    // ========================================================

    [Test]
    public void Validate_EffectWithAllNullFields_ReturnsEffectEmptyError()
    {
        var effect = new RulesetEffect();

        var result = _sut.Validate(MakeValidAndFilter(), effect);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("effect_empty"));
        Assert.That(result.Errors[0].Path, Is.EqualTo("effect"));
    }

    // ========================================================
    // Validate — unbalanced_braces
    // ========================================================

    [Test]
    public void Validate_EffectNameWithUnbalancedOpenBrace_ReturnsUnbalancedBracesError()
    {
        var effect = new RulesetEffect { Name = "Hello {name" };

        var result = _sut.Validate(MakeValidAndFilter(), effect);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("unbalanced_braces"));
        Assert.That(result.Errors[0].Path, Is.EqualTo("effect.name"));
    }

    [Test]
    public void Validate_EffectDescriptionWithUnbalancedCloseBrace_ReturnsUnbalancedBracesError()
    {
        var effect = new RulesetEffect { Description = "Completed run} today" };

        var result = _sut.Validate(MakeValidAndFilter(), effect);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("unbalanced_braces"));
        Assert.That(result.Errors[0].Path, Is.EqualTo("effect.description"));
    }

    // ========================================================
    // Validate — invalid_regex
    // ========================================================

    [Test]
    public void Validate_MatchesRegexWithInvalidPattern_ReturnsInvalidRegexError()
    {
        var filter = new AndFilter([new CheckFilter("name", "matches_regex", JsonSerializer.SerializeToElement("[unclosed"))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("invalid_regex"));
    }

    // ========================================================
    // Validate — start_time invalid format
    // ========================================================

    [TestCase("8:30")]
    [TestCase("08:30:00")]
    [TestCase("25:00")]
    [TestCase("08-30")]
    [TestCase("notaTime")]
    public void Validate_StartTimeWithInvalidFormat_ReturnsInvalidValueError(string badTime)
    {
        var filter = new AndFilter([new CheckFilter("start_time", "after", JsonSerializer.SerializeToElement(badTime))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("invalid_value"));
    }

    // ========================================================
    // Validate — within_radius
    // ========================================================

    [Test]
    public void Validate_WithinRadiusMissingRequiredKeys_ReturnsInvalidValueError()
    {
        // Object without lat/lng/radius_meters
        var value = JsonSerializer.SerializeToElement(new { lat = 59.9 });
        var filter = new AndFilter([new CheckFilter("start_location", "within_radius", value)]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("invalid_value"));
    }

    [Test]
    public void Validate_WithinRadiusWithAllRequiredKeys_NoError()
    {
        var value = JsonSerializer.SerializeToElement(new { lat = 59.9, lng = 10.7, radius_meters = 500 });
        var filter = new AndFilter([new CheckFilter("start_location", "within_radius", value)]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    // ========================================================
    // Validate — valid filter + effect (happy path)
    // ========================================================

    [Test]
    public void Validate_ValidFilterAndEffect_ReturnsIsValidTrueAndNoErrors()
    {
        var filter = new AndFilter([MakeCheckFilter()]);
        var effect = MakeValidEffect();

        var result = _sut.Validate(filter, effect);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_ValidStartTime_NoError()
    {
        var filter = new AndFilter([new CheckFilter("start_time", "after", JsonSerializer.SerializeToElement("08:30"))]);
        var effect = MakeValidEffect();

        var result = _sut.Validate(filter, effect);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_ValidMatchesRegex_NoError()
    {
        var filter = new AndFilter([new CheckFilter("name", "matches_regex", JsonSerializer.SerializeToElement("^Morning"))]);
        var effect = MakeValidEffect();

        var result = _sut.Validate(filter, effect);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    // ========================================================
    // Validate — OrFilter with non-empty conditions (lines 120-122)
    // ========================================================

    [Test]
    public void Validate_OrFilterWithNonEmptyConditions_ValidatesChildren()
    {
        var filter = new OrFilter([MakeCheckFilter()]);
        var effect = MakeValidEffect();

        var result = _sut.Validate(filter, effect);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_OrFilterWithInvalidChild_ReturnsChildError()
    {
        var filter = new OrFilter([new CheckFilter(null, null, null)]);
        var effect = MakeValidEffect();

        var result = _sut.Validate(filter, effect);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Path, Does.Contain("conditions[0]"));
    }

    // ========================================================
    // Validate — bool property (lines 183-189)
    // ========================================================

    [TestCase("has_location_data")]
    [TestCase("has_power_meter")]
    [TestCase("is_commute")]
    [TestCase("is_trainer")]
    [TestCase("is_manual")]
    [TestCase("is_private")]
    public void Validate_BoolProperty_WithBoolValue_NoError(string property)
    {
        var filter = new AndFilter([new CheckFilter(property, "eq", JsonSerializer.SerializeToElement(true))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_BoolProperty_WithNonBoolValue_ReturnsInvalidValueError()
    {
        var filter = new AndFilter([new CheckFilter("is_commute", "eq", JsonSerializer.SerializeToElement("yes"))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("invalid_value"));
        Assert.That(result.Errors[0].Path, Does.EndWith(".value"));
    }

    [Test]
    public void Validate_IsNullOperator_WithBoolValue_NoError()
    {
        // is_null is treated as a boolean-value operator regardless of property
        var filter = new AndFilter([new CheckFilter("gear_id", "is_null", JsonSerializer.SerializeToElement(true))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_IsNullOperator_WithNonBoolValue_ReturnsInvalidValueError()
    {
        var filter = new AndFilter([new CheckFilter("gear_id", "is_null", JsonSerializer.SerializeToElement(1))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("invalid_value"));
    }

    // ========================================================
    // Validate — numeric property (lines 194-200)
    // ========================================================

    [TestCase("distance_meters")]
    [TestCase("elapsed_time_seconds")]
    [TestCase("average_speed")]
    [TestCase("athlete_count")]
    public void Validate_NumericProperty_WithNumericValue_NoError(string property)
    {
        var filter = new AndFilter([new CheckFilter(property, "gt", JsonSerializer.SerializeToElement(100))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_NumericProperty_WithNonNumericValue_ReturnsInvalidValueError()
    {
        var filter = new AndFilter([new CheckFilter("distance_meters", "gt", JsonSerializer.SerializeToElement("far"))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("invalid_value"));
        Assert.That(result.Errors[0].Path, Does.EndWith(".value"));
    }

    // ========================================================
    // Validate — string array property error path (lines 207-210)
    // ========================================================

    [Test]
    public void Validate_SportTypeIn_WithNonArrayValue_ReturnsInvalidValueError()
    {
        var filter = new AndFilter([new CheckFilter("sport_type", "in", JsonSerializer.SerializeToElement("Run"))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("invalid_value"));
    }

    [Test]
    public void Validate_DayOfWeekNotIn_WithNonArrayValue_ReturnsInvalidValueError()
    {
        var filter = new AndFilter([new CheckFilter("day_of_week", "not_in", JsonSerializer.SerializeToElement("Monday"))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("invalid_value"));
    }

    // ========================================================
    // Validate — gear_id eq/not_eq (lines 216-222)
    // ========================================================

    [TestCase("eq")]
    [TestCase("not_eq")]
    public void Validate_GearIdEqNotEq_WithStringValue_NoError(string op)
    {
        var filter = new AndFilter([new CheckFilter("gear_id", op, JsonSerializer.SerializeToElement("b12345"))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [TestCase("eq")]
    [TestCase("not_eq")]
    public void Validate_GearIdEqNotEq_WithNonStringValue_ReturnsInvalidValueError(string op)
    {
        var filter = new AndFilter([new CheckFilter("gear_id", op, JsonSerializer.SerializeToElement(123))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("invalid_value"));
    }

    // ========================================================
    // Validate — int array property (lines 227-233)
    // ========================================================

    [TestCase("workout_type")]
    [TestCase("month")]
    public void Validate_IntArrayProperty_WithArrayValue_NoError(string property)
    {
        var filter = new AndFilter([new CheckFilter(property, "in", JsonSerializer.SerializeToElement(new[] { 1, 2 }))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_IntArrayProperty_WithNonArrayValue_ReturnsInvalidValueError()
    {
        var filter = new AndFilter([new CheckFilter("workout_type", "in", JsonSerializer.SerializeToElement("1"))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("invalid_value"));
    }

    // ========================================================
    // Validate — within_radius with non-object value (lines 240-243)
    // ========================================================

    [Test]
    public void Validate_WithinRadius_WithNonObjectValue_ReturnsInvalidValueError()
    {
        var filter = new AndFilter([new CheckFilter("start_location", "within_radius", JsonSerializer.SerializeToElement("here"))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("invalid_value"));
    }

    [Test]
    public void Validate_EndLocationWithinRadius_WithNonObjectValue_ReturnsInvalidValueError()
    {
        var filter = new AndFilter([new CheckFilter("end_location", "within_radius", JsonSerializer.SerializeToElement(42))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("invalid_value"));
    }

    // ========================================================
    // Validate — is_empty with non-bool value (lines 271-277)
    // ========================================================

    [Test]
    public void Validate_IsEmptyOperator_WithBoolValue_NoError()
    {
        // is_empty is only valid for description
        var filter = new AndFilter([new CheckFilter("description", "is_empty", JsonSerializer.SerializeToElement(false))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_IsEmptyOperator_WithNonBoolValue_ReturnsInvalidValueError()
    {
        var filter = new AndFilter([new CheckFilter("description", "is_empty", JsonSerializer.SerializeToElement(1))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("invalid_value"));
    }

    // ========================================================
    // Validate — string property with non-string value (lines 281-284)
    // ========================================================

    [TestCase("name", "contains")]
    [TestCase("name", "starts_with")]
    [TestCase("description", "contains")]
    [TestCase("timezone", "eq")]
    public void Validate_StringProperty_WithNonStringValue_ReturnsInvalidValueError(string property, string op)
    {
        var filter = new AndFilter([new CheckFilter(property, op, JsonSerializer.SerializeToElement(123))]);

        var result = _sut.Validate(filter, MakeValidEffect());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0].Code, Is.EqualTo("invalid_value"));
    }

    // ========================================================
    // Validate — HasBalancedBraces closing brace with non-negative depth (line 375)
    // ========================================================

    [Test]
    public void Validate_EffectNameWithBalancedBraces_IsValid()
    {
        // "{name}" exercises the closing-brace branch without going negative
        var effect = new RulesetEffect { Name = "{name}" };

        var result = _sut.Validate(MakeValidAndFilter(), effect);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Validate_EffectDescriptionWithBalancedBraces_IsValid()
    {
        var effect = new RulesetEffect { Description = "Completed {distance_km} km today" };

        var result = _sut.Validate(MakeValidAndFilter(), effect);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }
}
