using System.Text.Json;
using StravaEditBotApi.Models.Rules;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Tests.Unit.Services;

[TestFixture]
public class FilterSanitizerTests
{
    private FilterSanitizer _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new FilterSanitizer();

    // ========================================================
    // SanitizeForSharing
    // ========================================================

    [Test]
    public void SanitizeForSharing_StartLocationCheck_ValueNulledAndPropertyInList()
    {
        var filter = MakeCheckFilter("start_location", "within_radius",
            JsonSerializer.SerializeToElement(new { lat = 59.9, lng = 10.7, radius_meters = 500 }));

        var (sanitized, properties) = _sut.SanitizeForSharing(filter);

        var check = AssertCheckFilter(sanitized);
        Assert.That(check.Property, Is.EqualTo("start_location"));
        Assert.That(check.Operator, Is.EqualTo("within_radius"));
        Assert.That(check.Value, Is.Null);
        Assert.That(properties, Is.EqualTo(new[] { "start_location" }));
    }

    [Test]
    public void SanitizeForSharing_EndLocationCheck_ValueNulledAndPropertyInList()
    {
        var filter = MakeCheckFilter("end_location", "within_radius",
            JsonSerializer.SerializeToElement(new { lat = 59.9, lng = 10.7, radius_meters = 200 }));

        var (sanitized, properties) = _sut.SanitizeForSharing(filter);

        var check = AssertCheckFilter(sanitized);
        Assert.That(check.Property, Is.EqualTo("end_location"));
        Assert.That(check.Value, Is.Null);
        Assert.That(properties, Is.EqualTo(new[] { "end_location" }));
    }

    [Test]
    public void SanitizeForSharing_GearIdCheck_ValueNulledAndPropertyInList()
    {
        var filter = MakeCheckFilter("gear_id", "eq",
            JsonSerializer.SerializeToElement("b1234567890"));

        var (sanitized, properties) = _sut.SanitizeForSharing(filter);

        var check = AssertCheckFilter(sanitized);
        Assert.That(check.Property, Is.EqualTo("gear_id"));
        Assert.That(check.Value, Is.Null);
        Assert.That(properties, Is.EqualTo(new[] { "gear_id" }));
    }

    [Test]
    public void SanitizeForSharing_NonPiiCheck_UnchangedAndNotInList()
    {
        var originalValue = JsonSerializer.SerializeToElement(new[] { "Run" });
        var filter = MakeCheckFilter("sport_type", "in", originalValue);

        var (sanitized, properties) = _sut.SanitizeForSharing(filter);

        var check = AssertCheckFilter(sanitized);
        Assert.That(check.Property, Is.EqualTo("sport_type"));
        Assert.That(check.Operator, Is.EqualTo("in"));
        Assert.That(check.Value, Is.Not.Null);
        Assert.That(check.Value!.Value.GetRawText(), Is.EqualTo(originalValue.GetRawText()));
        Assert.That(properties, Is.Empty);
    }

    [Test]
    public void SanitizeForSharing_MixedAndFilter_OnlyPiiNulledAndStructurePreserved()
    {
        var filter = new AndFilter([
            MakeCheckFilter("start_location", "within_radius",
                JsonSerializer.SerializeToElement(new { lat = 59.9, lng = 10.7, radius_meters = 500 })),
            MakeCheckFilter("sport_type", "in",
                JsonSerializer.SerializeToElement(new[] { "Run" }))
        ]);

        var (sanitized, properties) = _sut.SanitizeForSharing(filter);

        var and = sanitized as AndFilter;
        Assert.That(and, Is.Not.Null);
        Assert.That(and!.Conditions, Has.Count.EqualTo(2));

        var locationCheck = and.Conditions[0] as CheckFilter;
        Assert.That(locationCheck, Is.Not.Null);
        Assert.That(locationCheck!.Property, Is.EqualTo("start_location"));
        Assert.That(locationCheck.Value, Is.Null);

        var sportCheck = and.Conditions[1] as CheckFilter;
        Assert.That(sportCheck, Is.Not.Null);
        Assert.That(sportCheck!.Property, Is.EqualTo("sport_type"));
        Assert.That(sportCheck.Value, Is.Not.Null);

        Assert.That(properties, Is.EqualTo(new[] { "start_location" }));
    }

    [Test]
    public void SanitizeForSharing_TwoStartLocationChecks_PropertyAppearsOnceInList()
    {
        var filter = new AndFilter([
            MakeCheckFilter("start_location", "within_radius",
                JsonSerializer.SerializeToElement(new { lat = 59.9, lng = 10.7, radius_meters = 500 })),
            MakeCheckFilter("start_location", "within_radius",
                JsonSerializer.SerializeToElement(new { lat = 60.0, lng = 11.0, radius_meters = 1000 }))
        ]);

        var (_, properties) = _sut.SanitizeForSharing(filter);

        Assert.That(properties, Has.Count.EqualTo(1));
        Assert.That(properties[0], Is.EqualTo("start_location"));
    }

    [Test]
    public void SanitizeForSharing_NestedAndContainingOrFilter_PiiNulledAtAllDepths()
    {
        var filter = new AndFilter([
            MakeCheckFilter("sport_type", "in",
                JsonSerializer.SerializeToElement(new[] { "Run" })),
            new OrFilter([
                MakeCheckFilter("start_location", "within_radius",
                    JsonSerializer.SerializeToElement(new { lat = 59.9, lng = 10.7, radius_meters = 500 })),
                MakeCheckFilter("gear_id", "eq",
                    JsonSerializer.SerializeToElement("b1234567890"))
            ])
        ]);

        var (sanitized, properties) = _sut.SanitizeForSharing(filter);

        var and = sanitized as AndFilter;
        Assert.That(and, Is.Not.Null);

        var sportCheck = and!.Conditions[0] as CheckFilter;
        Assert.That(sportCheck!.Value, Is.Not.Null);

        var or = and.Conditions[1] as OrFilter;
        Assert.That(or, Is.Not.Null);

        var locationCheck = or!.Conditions[0] as CheckFilter;
        Assert.That(locationCheck!.Value, Is.Null);

        var gearCheck = or.Conditions[1] as CheckFilter;
        Assert.That(gearCheck!.Value, Is.Null);

        Assert.That(properties, Has.Count.EqualTo(2));
        Assert.That(properties, Does.Contain("start_location"));
        Assert.That(properties, Does.Contain("gear_id"));
    }

    [Test]
    public void SanitizeForSharing_NotFilterWrappingPiiCheck_InnerCheckIsSanitized()
    {
        var filter = new NotFilter(
            MakeCheckFilter("start_location", "within_radius",
                JsonSerializer.SerializeToElement(new { lat = 59.9, lng = 10.7, radius_meters = 500 }))
        );

        var (sanitized, properties) = _sut.SanitizeForSharing(filter);

        var not = sanitized as NotFilter;
        Assert.That(not, Is.Not.Null);

        var check = not!.Condition as CheckFilter;
        Assert.That(check, Is.Not.Null);
        Assert.That(check!.Property, Is.EqualTo("start_location"));
        Assert.That(check.Operator, Is.EqualTo("within_radius"));
        Assert.That(check.Value, Is.Null);

        Assert.That(properties, Is.EqualTo(new[] { "start_location" }));
    }

    [Test]
    public void SanitizeForSharing_NoPiiProperties_EmptyListAndFilterUnchanged()
    {
        var sportValue = JsonSerializer.SerializeToElement(new[] { "Run", "Ride" });
        var distanceValue = JsonSerializer.SerializeToElement(new { gt = 5000 });

        var filter = new AndFilter([
            MakeCheckFilter("sport_type", "in", sportValue),
            MakeCheckFilter("distance", "gt", distanceValue)
        ]);

        var (sanitized, properties) = _sut.SanitizeForSharing(filter);

        Assert.That(properties, Is.Empty);

        var and = sanitized as AndFilter;
        Assert.That(and, Is.Not.Null);
        Assert.That(and!.Conditions, Has.Count.EqualTo(2));

        var sportCheck = and.Conditions[0] as CheckFilter;
        Assert.That(sportCheck!.Value!.Value.GetRawText(), Is.EqualTo(sportValue.GetRawText()));

        var distanceCheck = and.Conditions[1] as CheckFilter;
        Assert.That(distanceCheck!.Value!.Value.GetRawText(), Is.EqualTo(distanceValue.GetRawText()));
    }

    // ========================================================
    // Helpers
    // ========================================================

    private static CheckFilter MakeCheckFilter(
        string? property = null,
        string? op = null,
        JsonElement? value = null)
    {
        return new CheckFilter(
            Property: property ?? "sport_type",
            Operator: op ?? "eq",
            Value: value ?? JsonSerializer.SerializeToElement("Run")
        );
    }

    private static CheckFilter AssertCheckFilter(FilterExpression expression)
    {
        Assert.That(expression, Is.InstanceOf<CheckFilter>());
        return (CheckFilter)expression;
    }
}
