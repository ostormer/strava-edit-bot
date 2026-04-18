using System.Text.Json;
using StravaAPILibrary.Models.Activities;
using StravaAPILibrary.Models.Common;
using StravaAPILibrary.Models.Enums;
using StravaEditBotApi.Models.Rules;
using StravaEditBotApi.Services.Rulesets;

namespace StravaEditBotApi.Tests.Unit.Services;

[TestFixture]
public class FilterEvaluatorTests
{
    private FilterEvaluator _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new FilterEvaluator();

    // ========================================================
    // Factory helpers
    // ========================================================

    private static DetailedActivity MakeActivity(Action<DetailedActivity>? customize = null)
    {
        var activity = new DetailedActivity
        {
            SportType = SportType.Run,
            Distance = 5000f,
            ElapsedTime = 1800,
            MovingTime = 1700,
            TotalElevationGain = 100f,
            ElevHigh = 200f,
            AverageSpeed = 2.78f,
            MaxSpeed = 4.0f,
            StartDateLocal = new DateTime(2026, 4, 15, 7, 30, 0), // Wednesday, April, 07:30
            Timezone = "(GMT+01:00) Europe/Oslo",
            Name = "Morning Run",
            Description = "A great morning run",
            Commute = false,
            Trainer = false,
            Manual = false,
            Private = false,
            AthleteCount = 1,
            GearId = "b1234567890",
            StartLatLng = new LatLng { Latitude = 59.9139f, Longitude = 10.7522f },
            EndLatLng = new LatLng { Latitude = 59.9200f, Longitude = 10.7600f },
            AverageWatts = null,
            DeviceWatts = null,
            WorkoutType = null,
        };
        customize?.Invoke(activity);
        return activity;
    }

    private static CheckFilter Check(string property, string op, object value)
    {
        return new CheckFilter(property, op, JsonSerializer.SerializeToElement(value));
    }

    // ========================================================
    // Group 1: Logical Operators
    // ========================================================

    [Test]
    public void Evaluate_SingleTrueCheck_ReturnsTrue()
    {
        var filter = Check("is_commute", "eq", true);
        var activity = MakeActivity(a => a.Commute = true);

        Assert.That(_sut.Evaluate(filter, activity), Is.True);
    }

    [Test]
    public void Evaluate_SingleFalseCheck_ReturnsFalse()
    {
        var filter = Check("is_commute", "eq", true);
        var activity = MakeActivity(a => a.Commute = false);

        Assert.That(_sut.Evaluate(filter, activity), Is.False);
    }

    [Test]
    public void Evaluate_AndFilter_AllTrue_ReturnsTrue()
    {
        var filter = new AndFilter([
            Check("is_commute", "eq", false),
            Check("is_trainer", "eq", false)
        ]);
        var activity = MakeActivity();

        Assert.That(_sut.Evaluate(filter, activity), Is.True);
    }

    [Test]
    public void Evaluate_AndFilter_OneFalse_ReturnsFalse()
    {
        var filter = new AndFilter([
            Check("is_commute", "eq", false),
            Check("is_commute", "eq", true)
        ]);
        var activity = MakeActivity();

        Assert.That(_sut.Evaluate(filter, activity), Is.False);
    }

    [Test]
    public void Evaluate_AndFilter_Empty_ReturnsTrue()
    {
        var filter = new AndFilter([]);
        var activity = MakeActivity();

        Assert.That(_sut.Evaluate(filter, activity), Is.True);
    }

    [Test]
    public void Evaluate_OrFilter_OneTrue_ReturnsTrue()
    {
        var filter = new OrFilter([
            Check("is_commute", "eq", true),
            Check("is_commute", "eq", false)
        ]);
        var activity = MakeActivity();

        Assert.That(_sut.Evaluate(filter, activity), Is.True);
    }

    [Test]
    public void Evaluate_OrFilter_AllFalse_ReturnsFalse()
    {
        var filter = new OrFilter([
            Check("is_commute", "eq", true),
            Check("is_trainer", "eq", true)
        ]);
        var activity = MakeActivity();

        Assert.That(_sut.Evaluate(filter, activity), Is.False);
    }

    [Test]
    public void Evaluate_OrFilter_Empty_ReturnsFalse()
    {
        var filter = new OrFilter([]);
        var activity = MakeActivity();

        Assert.That(_sut.Evaluate(filter, activity), Is.False);
    }

    [Test]
    public void Evaluate_NotFilter_NegatesTrue_ReturnsFalse()
    {
        var filter = new NotFilter(Check("is_commute", "eq", false));
        var activity = MakeActivity();

        Assert.That(_sut.Evaluate(filter, activity), Is.False);
    }

    [Test]
    public void Evaluate_NotFilter_NegatesFalse_ReturnsTrue()
    {
        var filter = new NotFilter(Check("is_commute", "eq", true));
        var activity = MakeActivity();

        Assert.That(_sut.Evaluate(filter, activity), Is.True);
    }

    [Test]
    public void Evaluate_NestedAndInsideOr_EvaluatesCorrectly()
    {
        // Or([And([commute=false, trainer=true]), commute=false]) — second branch matches
        var filter = new OrFilter([
            new AndFilter([
                Check("is_commute", "eq", false),
                Check("is_trainer", "eq", true)  // trainer=false so And=false
            ]),
            Check("is_commute", "eq", false)  // commute=false so true
        ]);
        var activity = MakeActivity();

        Assert.That(_sut.Evaluate(filter, activity), Is.True);
    }

    [Test]
    public void Evaluate_DeeplyNested_NotAndOr_EvaluatesCorrectly()
    {
        // Not(And([Or([commute=true, trainer=false]), commute=false]))
        // Or([false, true]) = true; And([true, true]) = true; Not(true) = false
        var filter = new NotFilter(
            new AndFilter([
                new OrFilter([
                    Check("is_commute", "eq", true),   // false
                    Check("is_trainer", "eq", false)   // true
                ]),
                Check("is_commute", "eq", false)       // true
            ])
        );
        var activity = MakeActivity();

        Assert.That(_sut.Evaluate(filter, activity), Is.False);
    }

    // ========================================================
    // Group 2: Boolean Properties
    // ========================================================

    [Test]
    public void Evaluate_IsCommute_EqTrue_Matches()
    {
        var activity = MakeActivity(a => a.Commute = true);
        Assert.That(_sut.Evaluate(Check("is_commute", "eq", true), activity), Is.True);
    }

    [Test]
    public void Evaluate_IsCommute_EqFalse_NoMatch()
    {
        var activity = MakeActivity(a => a.Commute = true);
        Assert.That(_sut.Evaluate(Check("is_commute", "eq", false), activity), Is.False);
    }

    [Test]
    public void Evaluate_IsTrainer_EqTrue_Matches()
    {
        var activity = MakeActivity(a => a.Trainer = true);
        Assert.That(_sut.Evaluate(Check("is_trainer", "eq", true), activity), Is.True);
    }

    [Test]
    public void Evaluate_IsManual_EqTrue_Matches()
    {
        var activity = MakeActivity(a => a.Manual = true);
        Assert.That(_sut.Evaluate(Check("is_manual", "eq", true), activity), Is.True);
    }

    [Test]
    public void Evaluate_IsPrivate_EqTrue_Matches()
    {
        var activity = MakeActivity(a => a.Private = true);
        Assert.That(_sut.Evaluate(Check("is_private", "eq", true), activity), Is.True);
    }

    [Test]
    public void Evaluate_HasLocationData_EqTrue_WithLatLng_ReturnsTrue()
    {
        var activity = MakeActivity(); // StartLatLng set by default
        Assert.That(_sut.Evaluate(Check("has_location_data", "eq", true), activity), Is.True);
    }

    [Test]
    public void Evaluate_HasLocationData_EqTrue_NullLatLng_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.StartLatLng = null);
        Assert.That(_sut.Evaluate(Check("has_location_data", "eq", true), activity), Is.False);
    }

    [Test]
    public void Evaluate_HasLocationData_EqFalse_NullLatLng_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.StartLatLng = null);
        Assert.That(_sut.Evaluate(Check("has_location_data", "eq", false), activity), Is.True);
    }

    [Test]
    public void Evaluate_HasPowerMeter_EqTrue_DeviceWattsTrue_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.DeviceWatts = true);
        Assert.That(_sut.Evaluate(Check("has_power_meter", "eq", true), activity), Is.True);
    }

    [Test]
    public void Evaluate_HasPowerMeter_EqTrue_DeviceWattsNull_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.DeviceWatts = null);
        Assert.That(_sut.Evaluate(Check("has_power_meter", "eq", true), activity), Is.False);
    }

    [Test]
    public void Evaluate_HasPowerMeter_EqFalse_DeviceWattsFalse_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.DeviceWatts = false);
        Assert.That(_sut.Evaluate(Check("has_power_meter", "eq", false), activity), Is.True);
    }

    // ========================================================
    // Group 3: Enum/Set Properties (sport_type, workout_type)
    // ========================================================

    [Test]
    public void Evaluate_SportType_In_Matching_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.SportType = SportType.Run);
        Assert.That(_sut.Evaluate(Check("sport_type", "in", new[] { "Run", "TrailRun" }), activity), Is.True);
    }

    [Test]
    public void Evaluate_SportType_In_NonMatching_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.SportType = SportType.Run);
        Assert.That(_sut.Evaluate(Check("sport_type", "in", new[] { "Ride" }), activity), Is.False);
    }

    [Test]
    public void Evaluate_SportType_NotIn_NonMatching_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.SportType = SportType.Run);
        Assert.That(_sut.Evaluate(Check("sport_type", "not_in", new[] { "Ride" }), activity), Is.True);
    }

    [Test]
    public void Evaluate_SportType_NotIn_Matching_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.SportType = SportType.Run);
        Assert.That(_sut.Evaluate(Check("sport_type", "not_in", new[] { "Run" }), activity), Is.False);
    }

    [Test]
    public void Evaluate_SportType_In_InvalidEnum_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.SportType = SportType.Run);
        Assert.That(_sut.Evaluate(Check("sport_type", "in", new[] { "FlyingCarpet" }), activity), Is.False);
    }

    [Test]
    public void Evaluate_WorkoutType_In_Matching_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.WorkoutType = 1);
        Assert.That(_sut.Evaluate(Check("workout_type", "in", new[] { 1, 2 }), activity), Is.True);
    }

    [Test]
    public void Evaluate_WorkoutType_In_NonMatching_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.WorkoutType = 1);
        Assert.That(_sut.Evaluate(Check("workout_type", "in", new[] { 3 }), activity), Is.False);
    }

    [Test]
    public void Evaluate_WorkoutType_In_Null_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.WorkoutType = null);
        Assert.That(_sut.Evaluate(Check("workout_type", "in", new[] { 1 }), activity), Is.False);
    }

    [Test]
    public void Evaluate_WorkoutType_NotIn_Null_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.WorkoutType = null);
        Assert.That(_sut.Evaluate(Check("workout_type", "not_in", new[] { 1 }), activity), Is.True);
    }

    // ========================================================
    // Group 4: Gear ID (eq, not_eq, in, not_in, is_null)
    // ========================================================

    [Test]
    public void Evaluate_GearId_Eq_Matching_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.GearId = "b123");
        Assert.That(_sut.Evaluate(Check("gear_id", "eq", "b123"), activity), Is.True);
    }

    [Test]
    public void Evaluate_GearId_Eq_NonMatching_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.GearId = "b123");
        Assert.That(_sut.Evaluate(Check("gear_id", "eq", "b999"), activity), Is.False);
    }

    [Test]
    public void Evaluate_GearId_Eq_Null_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.GearId = null);
        Assert.That(_sut.Evaluate(Check("gear_id", "eq", "b123"), activity), Is.False);
    }

    [Test]
    public void Evaluate_GearId_NotEq_NonMatching_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.GearId = "b123");
        Assert.That(_sut.Evaluate(Check("gear_id", "not_eq", "b999"), activity), Is.True);
    }

    [Test]
    public void Evaluate_GearId_NotEq_Matching_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.GearId = "b123");
        Assert.That(_sut.Evaluate(Check("gear_id", "not_eq", "b123"), activity), Is.False);
    }

    [Test]
    public void Evaluate_GearId_In_Matching_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.GearId = "b123");
        Assert.That(_sut.Evaluate(Check("gear_id", "in", new[] { "b123", "b456" }), activity), Is.True);
    }

    [Test]
    public void Evaluate_GearId_In_NonMatching_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.GearId = "b123");
        Assert.That(_sut.Evaluate(Check("gear_id", "in", new[] { "b999" }), activity), Is.False);
    }

    [Test]
    public void Evaluate_GearId_NotIn_NonMatching_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.GearId = "b123");
        Assert.That(_sut.Evaluate(Check("gear_id", "not_in", new[] { "b999" }), activity), Is.True);
    }

    [Test]
    public void Evaluate_GearId_IsNull_True_Null_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.GearId = null);
        Assert.That(_sut.Evaluate(Check("gear_id", "is_null", true), activity), Is.True);
    }

    [Test]
    public void Evaluate_GearId_IsNull_True_Present_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.GearId = "b123");
        Assert.That(_sut.Evaluate(Check("gear_id", "is_null", true), activity), Is.False);
    }

    [Test]
    public void Evaluate_GearId_IsNull_False_Present_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.GearId = "b123");
        Assert.That(_sut.Evaluate(Check("gear_id", "is_null", false), activity), Is.True);
    }

    // ========================================================
    // Group 5: Numeric Comparisons (gt, lt, gte, lte)
    // ========================================================

    [Test]
    public void Evaluate_DistanceMeters_Gt_Greater_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.Distance = 5000f);
        Assert.That(_sut.Evaluate(Check("distance_meters", "gt", 4000), activity), Is.True);
    }

    [Test]
    public void Evaluate_DistanceMeters_Gt_Equal_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.Distance = 5000f);
        Assert.That(_sut.Evaluate(Check("distance_meters", "gt", 5000), activity), Is.False);
    }

    [Test]
    public void Evaluate_DistanceMeters_Gte_Equal_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.Distance = 5000f);
        Assert.That(_sut.Evaluate(Check("distance_meters", "gte", 5000), activity), Is.True);
    }

    [Test]
    public void Evaluate_DistanceMeters_Lt_Less_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.Distance = 5000f);
        Assert.That(_sut.Evaluate(Check("distance_meters", "lt", 6000), activity), Is.True);
    }

    [Test]
    public void Evaluate_DistanceMeters_Lte_Equal_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.Distance = 5000f);
        Assert.That(_sut.Evaluate(Check("distance_meters", "lte", 5000), activity), Is.True);
    }

    [Test]
    public void Evaluate_ElapsedTimeSeconds_Gt_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.ElapsedTime = 1800);
        Assert.That(_sut.Evaluate(Check("elapsed_time_seconds", "gt", 1000), activity), Is.True);
    }

    [Test]
    public void Evaluate_MovingTimeSeconds_Lt_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.MovingTime = 1700);
        Assert.That(_sut.Evaluate(Check("moving_time_seconds", "lt", 2000), activity), Is.True);
    }

    [Test]
    public void Evaluate_TotalElevationGain_Gte_Equal_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.TotalElevationGain = 100f);
        Assert.That(_sut.Evaluate(Check("total_elevation_gain", "gte", 100), activity), Is.True);
    }

    [Test]
    public void Evaluate_ElevHigh_Lte_Equal_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.ElevHigh = 200f);
        Assert.That(_sut.Evaluate(Check("elev_high", "lte", 200), activity), Is.True);
    }

    [Test]
    public void Evaluate_AverageSpeed_Gt_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.AverageSpeed = 2.78f);
        Assert.That(_sut.Evaluate(Check("average_speed", "gt", 2.5), activity), Is.True);
    }

    [Test]
    public void Evaluate_MaxSpeed_Gt_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.MaxSpeed = 4.0f);
        Assert.That(_sut.Evaluate(Check("max_speed", "gt", 3.5), activity), Is.True);
    }

    [Test]
    public void Evaluate_AverageWatts_Gt_HasWatts_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.AverageWatts = 185f);
        Assert.That(_sut.Evaluate(Check("average_watts", "gt", 100), activity), Is.True);
    }

    [Test]
    public void Evaluate_AverageWatts_Gt_NullWatts_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.AverageWatts = null);
        Assert.That(_sut.Evaluate(Check("average_watts", "gt", 100), activity), Is.False);
    }

    [Test]
    public void Evaluate_AthleteCount_Gt_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.AthleteCount = 3);
        Assert.That(_sut.Evaluate(Check("athlete_count", "gt", 1), activity), Is.True);
    }

    // ========================================================
    // Group 6: Computed Numeric Properties
    // ========================================================

    [Test]
    public void Evaluate_StoppedTimeSeconds_Gt_ReturnsTrue()
    {
        // elapsed=1800, moving=1700 → stopped=100 > 50
        var activity = MakeActivity(a => { a.ElapsedTime = 1800; a.MovingTime = 1700; });
        Assert.That(_sut.Evaluate(Check("stopped_time_seconds", "gt", 50), activity), Is.True);
    }

    [Test]
    public void Evaluate_StoppedTimeSeconds_Lt_ReturnsFalse()
    {
        // stopped=100, not < 50
        var activity = MakeActivity(a => { a.ElapsedTime = 1800; a.MovingTime = 1700; });
        Assert.That(_sut.Evaluate(Check("stopped_time_seconds", "lt", 50), activity), Is.False);
    }

    [Test]
    public void Evaluate_ElevationPerKm_Gt_ReturnsTrue()
    {
        // gain=100, dist=5000 → 100/(5000/1000)=20 m/km > 15
        var activity = MakeActivity(a => { a.TotalElevationGain = 100f; a.Distance = 5000f; });
        Assert.That(_sut.Evaluate(Check("elevation_per_km", "gt", 15), activity), Is.True);
    }

    [Test]
    public void Evaluate_ElevationPerKm_ZeroDistance_ReturnsFalse()
    {
        var activity = MakeActivity(a => { a.TotalElevationGain = 100f; a.Distance = 0f; });
        Assert.That(_sut.Evaluate(Check("elevation_per_km", "gt", 0), activity), Is.False);
    }

    [Test]
    public void Evaluate_ElevationPerKm_ZeroGainZeroDistance_ReturnsFalse()
    {
        var activity = MakeActivity(a => { a.TotalElevationGain = 0f; a.Distance = 0f; });
        Assert.That(_sut.Evaluate(Check("elevation_per_km", "gt", 0), activity), Is.False);
    }

    // ========================================================
    // Group 7: Location (within_radius / Haversine)
    // ========================================================

    [Test]
    public void Evaluate_StartLocation_WithinRadius_Inside_ReturnsTrue()
    {
        // Activity start = Oslo city centre. Filter centre = same point, 500m radius.
        var activity = MakeActivity(a => a.StartLatLng = new LatLng { Latitude = 59.9139f, Longitude = 10.7522f });
        var value = new { lat = 59.9139, lng = 10.7522, radius_meters = 500 };
        Assert.That(_sut.Evaluate(Check("start_location", "within_radius", value), activity), Is.True);
    }

    [Test]
    public void Evaluate_StartLocation_WithinRadius_Outside_ReturnsFalse()
    {
        // Activity ~10km from filter centre — Bergen vs Oslo
        var activity = MakeActivity(a => a.StartLatLng = new LatLng { Latitude = 60.3913f, Longitude = 5.3221f });
        var value = new { lat = 59.9139, lng = 10.7522, radius_meters = 500 };
        Assert.That(_sut.Evaluate(Check("start_location", "within_radius", value), activity), Is.False);
    }

    [Test]
    public void Evaluate_StartLocation_WithinRadius_Boundary_ReturnsTrue()
    {
        // Points: (0, 0) and (0, 0.001). Haversine ≈ 111.2m. Radius = 200m → inside.
        var activity = MakeActivity(a => a.StartLatLng = new LatLng { Latitude = 0f, Longitude = 0.001f });
        var value = new { lat = 0.0, lng = 0.0, radius_meters = 200 };
        Assert.That(_sut.Evaluate(Check("start_location", "within_radius", value), activity), Is.True);
    }

    [Test]
    public void Evaluate_StartLocation_NullLatLng_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.StartLatLng = null);
        var value = new { lat = 59.9139, lng = 10.7522, radius_meters = 500 };
        Assert.That(_sut.Evaluate(Check("start_location", "within_radius", value), activity), Is.False);
    }

    [Test]
    public void Evaluate_EndLocation_WithinRadius_Inside_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.EndLatLng = new LatLng { Latitude = 59.9200f, Longitude = 10.7600f });
        var value = new { lat = 59.9200, lng = 10.7600, radius_meters = 500 };
        Assert.That(_sut.Evaluate(Check("end_location", "within_radius", value), activity), Is.True);
    }

    [Test]
    public void Evaluate_EndLocation_NullLatLng_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.EndLatLng = null);
        var value = new { lat = 59.9200, lng = 10.7600, radius_meters = 500 };
        Assert.That(_sut.Evaluate(Check("end_location", "within_radius", value), activity), Is.False);
    }

    // ========================================================
    // Group 8: Time Properties
    // ========================================================

    [Test]
    public void Evaluate_StartTime_After_ActivityAfterTime_ReturnsTrue()
    {
        // StartDateLocal = 07:30, filter after "07:00" → true
        var activity = MakeActivity(a => a.StartDateLocal = new DateTime(2026, 4, 15, 7, 30, 0));
        Assert.That(_sut.Evaluate(Check("start_time", "after", "07:00"), activity), Is.True);
    }

    [Test]
    public void Evaluate_StartTime_After_ActivityBeforeTime_ReturnsFalse()
    {
        // StartDateLocal = 07:30, filter after "08:00" → false
        var activity = MakeActivity(a => a.StartDateLocal = new DateTime(2026, 4, 15, 7, 30, 0));
        Assert.That(_sut.Evaluate(Check("start_time", "after", "08:00"), activity), Is.False);
    }

    [Test]
    public void Evaluate_StartTime_Before_ActivityBeforeTime_ReturnsTrue()
    {
        // StartDateLocal = 07:30, filter before "08:00" → true
        var activity = MakeActivity(a => a.StartDateLocal = new DateTime(2026, 4, 15, 7, 30, 0));
        Assert.That(_sut.Evaluate(Check("start_time", "before", "08:00"), activity), Is.True);
    }

    [Test]
    public void Evaluate_StartTime_Before_ActivityAfterTime_ReturnsFalse()
    {
        // StartDateLocal = 07:30, filter before "07:00" → false
        var activity = MakeActivity(a => a.StartDateLocal = new DateTime(2026, 4, 15, 7, 30, 0));
        Assert.That(_sut.Evaluate(Check("start_time", "before", "07:00"), activity), Is.False);
    }

    [Test]
    public void Evaluate_StartTime_After_ExactMatch_ReturnsFalse()
    {
        // "after" is strict — exact match is not after
        var activity = MakeActivity(a => a.StartDateLocal = new DateTime(2026, 4, 15, 7, 30, 0));
        Assert.That(_sut.Evaluate(Check("start_time", "after", "07:30"), activity), Is.False);
    }

    [Test]
    public void Evaluate_DayOfWeek_In_MatchingDay_ReturnsTrue()
    {
        // 2026-04-15 = Wednesday
        var activity = MakeActivity(a => a.StartDateLocal = new DateTime(2026, 4, 15, 7, 30, 0));
        Assert.That(_sut.Evaluate(Check("day_of_week", "in", new[] { "Wednesday" }), activity), Is.True);
    }

    [Test]
    public void Evaluate_DayOfWeek_In_NonMatchingDay_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.StartDateLocal = new DateTime(2026, 4, 15, 7, 30, 0));
        Assert.That(_sut.Evaluate(Check("day_of_week", "in", new[] { "Monday" }), activity), Is.False);
    }

    [Test]
    public void Evaluate_DayOfWeek_NotIn_NonMatchingDay_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.StartDateLocal = new DateTime(2026, 4, 15, 7, 30, 0));
        Assert.That(_sut.Evaluate(Check("day_of_week", "not_in", new[] { "Monday" }), activity), Is.True);
    }

    [Test]
    public void Evaluate_Month_In_MatchingMonth_ReturnsTrue()
    {
        // April = month 4
        var activity = MakeActivity(a => a.StartDateLocal = new DateTime(2026, 4, 15, 7, 30, 0));
        Assert.That(_sut.Evaluate(Check("month", "in", new[] { 4 }), activity), Is.True);
    }

    [Test]
    public void Evaluate_Month_In_NonMatchingMonth_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.StartDateLocal = new DateTime(2026, 4, 15, 7, 30, 0));
        Assert.That(_sut.Evaluate(Check("month", "in", new[] { 1, 2 }), activity), Is.False);
    }

    [Test]
    public void Evaluate_Month_NotIn_NonMatchingMonth_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.StartDateLocal = new DateTime(2026, 4, 15, 7, 30, 0));
        Assert.That(_sut.Evaluate(Check("month", "not_in", new[] { 1, 2 }), activity), Is.True);
    }

    // ========================================================
    // Group 9: String Properties
    // ========================================================

    [Test]
    public void Evaluate_Timezone_Eq_Match_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.Timezone = "(GMT+01:00) Europe/Oslo");
        Assert.That(_sut.Evaluate(Check("timezone", "eq", "(GMT+01:00) Europe/Oslo"), activity), Is.True);
    }

    [Test]
    public void Evaluate_Timezone_Eq_NoMatch_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.Timezone = "(GMT+01:00) Europe/Oslo");
        Assert.That(_sut.Evaluate(Check("timezone", "eq", "(GMT+09:00) Asia/Tokyo"), activity), Is.False);
    }

    [Test]
    public void Evaluate_Timezone_Contains_Match_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.Timezone = "(GMT+01:00) Europe/Oslo");
        Assert.That(_sut.Evaluate(Check("timezone", "contains", "Oslo"), activity), Is.True);
    }

    [Test]
    public void Evaluate_Timezone_Contains_NoMatch_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.Timezone = "(GMT+01:00) Europe/Oslo");
        Assert.That(_sut.Evaluate(Check("timezone", "contains", "Tokyo"), activity), Is.False);
    }

    [Test]
    public void Evaluate_Name_Contains_Match_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.Name = "Morning Run");
        Assert.That(_sut.Evaluate(Check("name", "contains", "Morning"), activity), Is.True);
    }

    [Test]
    public void Evaluate_Name_Contains_CaseInsensitive_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.Name = "Morning Run");
        Assert.That(_sut.Evaluate(Check("name", "contains", "morning"), activity), Is.True);
    }

    [Test]
    public void Evaluate_Name_StartsWith_Match_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.Name = "Morning Run");
        Assert.That(_sut.Evaluate(Check("name", "starts_with", "Morning"), activity), Is.True);
    }

    [Test]
    public void Evaluate_Name_StartsWith_CaseInsensitive_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.Name = "Morning Run");
        Assert.That(_sut.Evaluate(Check("name", "starts_with", "morning"), activity), Is.True);
    }

    [Test]
    public void Evaluate_Name_StartsWith_NoMatch_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.Name = "Morning Run");
        Assert.That(_sut.Evaluate(Check("name", "starts_with", "Evening"), activity), Is.False);
    }

    [Test]
    public void Evaluate_Name_MatchesRegex_Match_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.Name = "Morning Run");
        Assert.That(_sut.Evaluate(Check("name", "matches_regex", "^Morning.*Run$"), activity), Is.True);
    }

    [Test]
    public void Evaluate_Name_MatchesRegex_NoMatch_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.Name = "Morning Run");
        Assert.That(_sut.Evaluate(Check("name", "matches_regex", "^Evening"), activity), Is.False);
    }

    [Test]
    public void Evaluate_Description_Contains_Match_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.Description = "A great morning run");
        Assert.That(_sut.Evaluate(Check("description", "contains", "great"), activity), Is.True);
    }

    [Test]
    public void Evaluate_Description_Contains_NullDescription_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.Description = null);
        Assert.That(_sut.Evaluate(Check("description", "contains", "great"), activity), Is.False);
    }

    [Test]
    public void Evaluate_Description_IsEmpty_True_NullDescription_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.Description = null);
        Assert.That(_sut.Evaluate(Check("description", "is_empty", true), activity), Is.True);
    }

    [Test]
    public void Evaluate_Description_IsEmpty_True_EmptyString_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.Description = "");
        Assert.That(_sut.Evaluate(Check("description", "is_empty", true), activity), Is.True);
    }

    [Test]
    public void Evaluate_Description_IsEmpty_True_NonEmpty_ReturnsFalse()
    {
        var activity = MakeActivity(a => a.Description = "some text");
        Assert.That(_sut.Evaluate(Check("description", "is_empty", true), activity), Is.False);
    }

    [Test]
    public void Evaluate_Description_IsEmpty_False_NonEmpty_ReturnsTrue()
    {
        var activity = MakeActivity(a => a.Description = "some text");
        Assert.That(_sut.Evaluate(Check("description", "is_empty", false), activity), Is.True);
    }

    // ========================================================
    // Group 10: Edge Cases
    // ========================================================

    [Test]
    public void Evaluate_CheckFilter_NullProperty_ReturnsFalse()
    {
        var filter = new CheckFilter(null, "eq", JsonSerializer.SerializeToElement(true));
        var activity = MakeActivity();
        Assert.That(_sut.Evaluate(filter, activity), Is.False);
    }

    [Test]
    public void Evaluate_CheckFilter_NullOperator_ReturnsFalse()
    {
        var filter = new CheckFilter("is_commute", null, JsonSerializer.SerializeToElement(true));
        var activity = MakeActivity();
        Assert.That(_sut.Evaluate(filter, activity), Is.False);
    }

    [Test]
    public void Evaluate_CheckFilter_NullValue_ReturnsFalse()
    {
        var filter = new CheckFilter("is_commute", "eq", null);
        var activity = MakeActivity();
        Assert.That(_sut.Evaluate(filter, activity), Is.False);
    }

    [Test]
    public void Evaluate_CheckFilter_UnknownProperty_ReturnsFalse()
    {
        var activity = MakeActivity();
        Assert.That(_sut.Evaluate(Check("magic_field", "eq", true), activity), Is.False);
    }

    [Test]
    public void Evaluate_CheckFilter_UnknownOperator_ReturnsFalse()
    {
        var activity = MakeActivity();
        Assert.That(_sut.Evaluate(Check("distance_meters", "magic_op", 1000), activity), Is.False);
    }

    [Test]
    public void Evaluate_MatchesRegex_CatastrophicBacktracking_ReturnsFalse()
    {
        // Pattern causes catastrophic backtracking — should timeout and return false
        var activity = MakeActivity(a => a.Name = new string('a', 25) + "X");
        Assert.That(_sut.Evaluate(Check("name", "matches_regex", "(a+)+$"), activity), Is.False);
    }
}
