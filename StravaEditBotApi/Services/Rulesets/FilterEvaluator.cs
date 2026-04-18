using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using StravaAPILibrary.Models.Activities;
using StravaAPILibrary.Models.Common;
using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.Services.Rulesets;

public class FilterEvaluator : IFilterEvaluator
{
    // ──────────────────────────────────────────────────────────
    // Property registries — adding a new property = one line
    // ──────────────────────────────────────────────────────────

    private static readonly Dictionary<string, Func<DetailedActivity, bool?>> _boolProperties = new()
    {
        ["is_commute"]        = a => a.Commute,
        ["is_trainer"]        = a => a.Trainer,
        ["is_manual"]         = a => a.Manual,
        ["is_private"]        = a => a.Private,
        ["has_location_data"] = a => a.StartLatLng is not null,
        ["has_power_meter"]   = a => a.DeviceWatts == true,
    };

    private static readonly Dictionary<string, Func<DetailedActivity, double?>> _numericProperties = new()
    {
        ["distance_meters"]      = a => (double?)a.Distance,
        ["elapsed_time_seconds"] = a => (double?)a.ElapsedTime,
        ["moving_time_seconds"]  = a => (double?)a.MovingTime,
        ["stopped_time_seconds"] = a => (double?)(a.ElapsedTime - a.MovingTime),
        ["total_elevation_gain"] = a => (double?)a.TotalElevationGain,
        ["elev_high"]            = a => (double?)a.ElevHigh,
        ["elevation_per_km"]     = a => a.Distance > 0
                                        ? (double?)(a.TotalElevationGain / (a.Distance / 1000.0))
                                        : null,
        ["average_speed"]        = a => (double?)a.AverageSpeed,
        ["max_speed"]            = a => (double?)a.MaxSpeed,
        ["average_watts"]        = a => (double?)a.AverageWatts,
        ["athlete_count"]        = a => (double?)a.AthleteCount,
    };

    // Each property maps to a string representation of the activity's value.
    // null = activity has no value (e.g. workout_type is null).
    // Filter values (string[] or int[]) are normalised to string[] via ExtractStringArray.
    private static readonly Dictionary<string, Func<DetailedActivity, string?>> _stringSetProperties = new()
    {
        ["sport_type"]   = a => a.SportType.ToString(),
        ["workout_type"] = a => a.WorkoutType?.ToString(),
        ["day_of_week"]  = a => a.StartDateLocal.DayOfWeek.ToString(),
        ["month"]        = a => a.StartDateLocal.Month.ToString(),
    };

    private static readonly Dictionary<string, Func<DetailedActivity, string?>> _stringProperties = new()
    {
        ["name"]        = a => a.Name,
        ["description"] = a => a.Description,
        ["timezone"]    = a => a.Timezone,
    };

    // String identity properties that support eq / not_eq / in / not_in / is_null
    private static readonly Dictionary<string, Func<DetailedActivity, string?>> _stringIdProperties = new()
    {
        ["gear_id"] = a => a.GearId,
    };

    private static readonly Dictionary<string, Func<DetailedActivity, LatLng?>> _locationProperties = new()
    {
        ["start_location"] = a => a.StartLatLng,
        ["end_location"]   = a => a.EndLatLng,
    };

    // ──────────────────────────────────────────────────────────
    // Public entry point
    // ──────────────────────────────────────────────────────────

    public bool Evaluate(FilterExpression filter, DetailedActivity activity)
    {
        return filter switch
        {
            AndFilter and    => and.Conditions.All(c => Evaluate(c, activity)),
            OrFilter or      => or.Conditions.Any(c => Evaluate(c, activity)),
            NotFilter not    => !Evaluate(not.Condition, activity),
            CheckFilter check => EvaluateCheck(check, activity),
            _                => false,
        };
    }

    // ──────────────────────────────────────────────────────────
    // Check dispatch
    // ──────────────────────────────────────────────────────────

    private static bool EvaluateCheck(CheckFilter check, DetailedActivity activity)
    {
        if (check.Property is null || check.Operator is null || check.Value is null)
        {
            return false;
        }

        if (_boolProperties.TryGetValue(check.Property, out var getBool))
        {
            return EvaluateBoolOp(check, getBool(activity));
        }

        if (_numericProperties.TryGetValue(check.Property, out var getNumeric))
        {
            return EvaluateNumericOp(check, getNumeric(activity));
        }

        if (_stringSetProperties.TryGetValue(check.Property, out var getSetValue))
        {
            return EvaluateStringSetOp(check, getSetValue(activity));
        }

        if (_stringIdProperties.TryGetValue(check.Property, out var getStringId))
        {
            return EvaluateStringIdOp(check, getStringId(activity));
        }

        if (_stringProperties.TryGetValue(check.Property, out var getString))
        {
            return EvaluateStringOp(check, getString(activity));
        }

        if (_locationProperties.TryGetValue(check.Property, out var getLocation))
        {
            return EvaluateLocationOp(check, getLocation(activity));
        }

        if (check.Property == "start_time")
        {
            return EvaluateStartTimeOp(check, activity.StartDateLocal);
        }

        return false;
    }

    // ──────────────────────────────────────────────────────────
    // Category evaluators — adding a new operator = one case
    // ──────────────────────────────────────────────────────────

    private static bool EvaluateBoolOp(CheckFilter check, bool? actualValue)
    {
        bool filterValue = check.Value!.Value.ValueKind == JsonValueKind.True;
        return check.Operator switch
        {
            "eq" => actualValue == filterValue,
            _    => false,
        };
    }

    private static bool EvaluateNumericOp(CheckFilter check, double? actualValue)
    {
        if (actualValue is null)
        {
            return false;
        }

        double threshold;
        try
        {
            threshold = check.Value!.Value.GetDouble();
        }
        catch
        {
            return false;
        }

        return check.Operator switch
        {
            "gt"  => actualValue.Value > threshold,
            "lt"  => actualValue.Value < threshold,
            "gte" => actualValue.Value >= threshold,
            "lte" => actualValue.Value <= threshold,
            _     => false,
        };
    }

    private static bool EvaluateStringSetOp(CheckFilter check, string? actualValue)
    {
        string[] filterValues = ExtractStringArray(check.Value!.Value);
        return check.Operator switch
        {
            "in"     => actualValue is not null && filterValues.Contains(actualValue, StringComparer.OrdinalIgnoreCase),
            "not_in" => actualValue is null || !filterValues.Contains(actualValue, StringComparer.OrdinalIgnoreCase),
            _        => false,
        };
    }

    private static bool EvaluateStringIdOp(CheckFilter check, string? actualValue)
    {
        return check.Operator switch
        {
            "eq"     => actualValue is not null
                        && actualValue.Equals(check.Value!.Value.GetString(), StringComparison.Ordinal),
            "not_eq" => actualValue is null
                        || !actualValue.Equals(check.Value!.Value.GetString(), StringComparison.Ordinal),
            "in"     => actualValue is not null
                        && ExtractStringArray(check.Value!.Value).Contains(actualValue, StringComparer.Ordinal),
            "not_in" => actualValue is null
                        || !ExtractStringArray(check.Value!.Value).Contains(actualValue, StringComparer.Ordinal),
            "is_null" => check.Value!.Value.ValueKind == JsonValueKind.True
                         ? actualValue is null
                         : actualValue is not null,
            _ => false,
        };
    }

    private static bool EvaluateStringOp(CheckFilter check, string? actualValue)
    {
        return check.Operator switch
        {
            "eq" => actualValue is not null
                    && actualValue.Equals(check.Value!.Value.GetString(), StringComparison.OrdinalIgnoreCase),
            "contains" => actualValue is not null
                          && actualValue.Contains(
                              check.Value!.Value.GetString() ?? string.Empty,
                              StringComparison.OrdinalIgnoreCase),
            "starts_with" => actualValue is not null
                             && actualValue.StartsWith(
                                 check.Value!.Value.GetString() ?? string.Empty,
                                 StringComparison.OrdinalIgnoreCase),
            "matches_regex" => EvaluateRegex(actualValue, check.Value!.Value.GetString()),
            "is_empty" => check.Value!.Value.ValueKind == JsonValueKind.True
                          ? string.IsNullOrEmpty(actualValue)
                          : !string.IsNullOrEmpty(actualValue),
            _ => false,
        };
    }

    private static bool EvaluateLocationOp(CheckFilter check, LatLng? actualLatLng)
    {
        if (actualLatLng is null || check.Operator != "within_radius")
        {
            return false;
        }

        try
        {
            JsonElement json = check.Value!.Value;
            double lat = json.GetProperty("lat").GetDouble();
            double lng = json.GetProperty("lng").GetDouble();
            double radiusMeters = json.GetProperty("radius_meters").GetDouble();
            double distance = HaversineMeters(actualLatLng.Latitude, actualLatLng.Longitude, (float)lat, (float)lng);
            return distance <= radiusMeters;
        }
        catch
        {
            return false;
        }
    }

    private static bool EvaluateStartTimeOp(CheckFilter check, DateTime startDateLocal)
    {
        string? timeString = check.Value!.Value.GetString();
        if (timeString is null || !TimeSpan.TryParse(timeString, CultureInfo.InvariantCulture, out TimeSpan threshold))
        {
            return false;
        }

        TimeSpan actual = startDateLocal.TimeOfDay;
        return check.Operator switch
        {
            "after"  => actual > threshold,
            "before" => actual < threshold,
            _        => false,
        };
    }

    // ──────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────

    private static bool EvaluateRegex(string? input, string? pattern)
    {
        if (input is null || pattern is null)
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(input, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Converts a JSON array element to string[]. Handles both string and number array elements.
    /// </summary>
    private static string[] ExtractStringArray(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return element.EnumerateArray()
            .Select(e => e.ValueKind == JsonValueKind.String
                ? e.GetString() ?? string.Empty
                : e.ToString())
            .ToArray();
    }

    /// <summary>Haversine distance in meters between two lat/lng points.</summary>
    private static double HaversineMeters(float lat1, float lng1, float lat2, float lng2)
    {
        const double R = 6371000;
        double phi1 = lat1 * Math.PI / 180;
        double phi2 = lat2 * Math.PI / 180;
        double deltaPhi = (lat2 - lat1) * Math.PI / 180;
        double deltaLambda = (lng2 - lng1) * Math.PI / 180;
        double a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2)
                 + Math.Cos(phi1) * Math.Cos(phi2)
                 * Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
