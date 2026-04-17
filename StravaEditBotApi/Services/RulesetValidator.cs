using System.Text.RegularExpressions;
using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.Services;

public class RulesetValidator : IRulesetValidator
{
    private const int MaxDepth = 10;

    // Known check properties and their valid operators
    private static readonly Dictionary<string, HashSet<string>> _validOperators = new()
    {
        ["sport_type"] = ["in", "not_in"],
        ["workout_type"] = ["in", "not_in"],
        ["gear_id"] = ["eq", "not_eq", "in", "not_in", "is_null"],
        ["start_location"] = ["within_radius"],
        ["end_location"] = ["within_radius"],
        ["has_location_data"] = ["eq"],
        ["timezone"] = ["eq", "contains"],
        ["start_time"] = ["after", "before"],
        ["day_of_week"] = ["in", "not_in"],
        ["month"] = ["in", "not_in"],
        ["distance_meters"] = ["gt", "lt", "gte", "lte"],
        ["elapsed_time_seconds"] = ["gt", "lt", "gte", "lte"],
        ["moving_time_seconds"] = ["gt", "lt", "gte", "lte"],
        ["stopped_time_seconds"] = ["gt", "lt", "gte", "lte"],
        ["total_elevation_gain"] = ["gt", "lt", "gte", "lte"],
        ["elev_high"] = ["gt", "lt", "gte", "lte"],
        ["elevation_per_km"] = ["gt", "lt", "gte", "lte"],
        ["average_speed"] = ["gt", "lt", "gte", "lte"],
        ["max_speed"] = ["gt", "lt", "gte", "lte"],
        ["average_watts"] = ["gt", "lt", "gte", "lte"],
        ["has_power_meter"] = ["eq"],
        ["is_commute"] = ["eq"],
        ["is_trainer"] = ["eq"],
        ["is_manual"] = ["eq"],
        ["is_private"] = ["eq"],
        ["name"] = ["contains", "starts_with", "matches_regex"],
        ["description"] = ["contains", "starts_with", "matches_regex", "is_empty"],
        ["athlete_count"] = ["gt", "lt", "gte", "lte"],
    };

    private static readonly HashSet<string> _boolProperties =
    [
        "has_location_data", "has_power_meter", "is_commute", "is_trainer", "is_manual", "is_private"
    ];

    private static readonly HashSet<string> _numericProperties =
    [
        "distance_meters", "elapsed_time_seconds", "moving_time_seconds", "stopped_time_seconds",
        "total_elevation_gain", "elev_high", "elevation_per_km", "average_speed", "max_speed",
        "average_watts", "athlete_count"
    ];

    private static readonly HashSet<string> _stringArrayProperties =
    [
        "sport_type", "gear_id", "day_of_week"
    ];

    private static readonly HashSet<string> _intArrayProperties =
    [
        "workout_type", "month"
    ];

    public RulesetValidationResult Validate(FilterExpression? filter, RulesetEffect? effect)
    {
        var errors = new List<RulesetValidationError>();

        if (filter is null)
        {
            errors.Add(new RulesetValidationError("filter", "filter_required", "A filter is required."));
        }
        else
        {
            ValidateFilter(filter, "filter", 0, errors);
        }

        if (effect is null)
        {
            errors.Add(new RulesetValidationError("effect", "effect_required", "An effect is required."));
        }
        else
        {
            ValidateEffect(effect, errors);
        }

        return new RulesetValidationResult(errors.Count == 0, errors);
    }

    private static void ValidateFilter(FilterExpression node, string path, int depth, List<RulesetValidationError> errors)
    {
        if (depth > MaxDepth)
        {
            errors.Add(new RulesetValidationError(path, "max_depth_exceeded",
                $"Filter exceeds maximum nesting depth of {MaxDepth}."));
            return;
        }

        switch (node)
        {
            case AndFilter and:
                if (and.Conditions.Count == 0)
                {
                    errors.Add(new RulesetValidationError(path, "filter_empty",
                        "An 'and' filter must have at least one condition."));
                }
                for (int i = 0; i < and.Conditions.Count; i++)
                {
                    ValidateFilter(and.Conditions[i], $"{path}.conditions[{i}]", depth + 1, errors);
                }
                break;

            case OrFilter or:
                if (or.Conditions.Count == 0)
                {
                    errors.Add(new RulesetValidationError(path, "filter_empty",
                        "An 'or' filter must have at least one condition."));
                }
                for (int i = 0; i < or.Conditions.Count; i++)
                {
                    ValidateFilter(or.Conditions[i], $"{path}.conditions[{i}]", depth + 1, errors);
                }
                break;

            case NotFilter not:
                ValidateFilter(not.Condition, $"{path}.condition", depth + 1, errors);
                break;

            case CheckFilter check:
                ValidateCheck(check, path, errors);
                break;
        }
    }

    private static void ValidateCheck(CheckFilter check, string path, List<RulesetValidationError> errors)
    {
        if (check.Property is null)
        {
            errors.Add(new RulesetValidationError($"{path}.property", "incomplete_check",
                "Property is required."));
            return;
        }

        if (!_validOperators.TryGetValue(check.Property, out HashSet<string>? allowedOps))
        {
            errors.Add(new RulesetValidationError($"{path}.property", "unknown_property",
                $"Unknown property '{check.Property}'."));
            return;
        }

        if (check.Operator is null)
        {
            errors.Add(new RulesetValidationError($"{path}.operator", "incomplete_check",
                "Operator is required."));
            return;
        }

        if (!allowedOps.Contains(check.Operator))
        {
            errors.Add(new RulesetValidationError($"{path}.operator", "invalid_operator",
                $"Operator '{check.Operator}' is not valid for property '{check.Property}'."));
            return;
        }

        if (check.Value is null)
        {
            errors.Add(new RulesetValidationError($"{path}.value", "incomplete_check",
                "Value is required."));
            return;
        }

        ValidateCheckValue(check, path, errors);
    }

    private static void ValidateCheckValue(CheckFilter check, string path, List<RulesetValidationError> errors)
    {
        string property = check.Property!;
        string op = check.Operator!;
        var value = check.Value!.Value;

        // Boolean properties
        if (_boolProperties.Contains(property) || op == "is_null")
        {
            if (value.ValueKind is not (System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False))
            {
                errors.Add(new RulesetValidationError($"{path}.value", "invalid_value",
                    $"Value for '{property}' with operator '{op}' must be a boolean."));
            }
            return;
        }

        // Numeric properties
        if (_numericProperties.Contains(property))
        {
            if (value.ValueKind != System.Text.Json.JsonValueKind.Number)
            {
                errors.Add(new RulesetValidationError($"{path}.value", "invalid_value",
                    $"Value for '{property}' must be a number."));
            }
            return;
        }

        // String array properties with in/not_in
        if (_stringArrayProperties.Contains(property) && (op == "in" || op == "not_in"))
        {
            if (value.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                errors.Add(new RulesetValidationError($"{path}.value", "invalid_value",
                    $"Value for '{property}' with operator '{op}' must be a string array."));
            }
            return;
        }

        // gear_id with eq/not_eq
        if (property == "gear_id" && (op == "eq" || op == "not_eq"))
        {
            if (value.ValueKind != System.Text.Json.JsonValueKind.String)
            {
                errors.Add(new RulesetValidationError($"{path}.value", "invalid_value",
                    "Value for 'gear_id' with operator 'eq'/'not_eq' must be a string."));
            }
            return;
        }

        // Int array properties
        if (_intArrayProperties.Contains(property))
        {
            if (value.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                errors.Add(new RulesetValidationError($"{path}.value", "invalid_value",
                    $"Value for '{property}' must be an integer array."));
            }
            return;
        }

        // Location within_radius
        if ((property == "start_location" || property == "end_location") && op == "within_radius")
        {
            if (value.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                errors.Add(new RulesetValidationError($"{path}.value", "invalid_value",
                    $"Value for '{property}' must be an object with 'lat', 'lng', and 'radius_meters'."));
                return;
            }
            if (!value.TryGetProperty("lat", out _) ||
                !value.TryGetProperty("lng", out _) ||
                !value.TryGetProperty("radius_meters", out _))
            {
                errors.Add(new RulesetValidationError($"{path}.value", "invalid_value",
                    $"Value for '{property}' must include 'lat', 'lng', and 'radius_meters'."));
            }
            return;
        }

        // start_time string (HH:mm)
        if (property == "start_time")
        {
            if (value.ValueKind != System.Text.Json.JsonValueKind.String ||
                !IsValidTimeString(value.GetString()!))
            {
                errors.Add(new RulesetValidationError($"{path}.value", "invalid_value",
                    "Value for 'start_time' must be a string in HH:mm format."));
            }
            return;
        }

        // timezone, name, description string operators
        if (property is "timezone" or "name" or "description")
        {
            if (op == "is_empty")
            {
                if (value.ValueKind is not (System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False))
                {
                    errors.Add(new RulesetValidationError($"{path}.value", "invalid_value",
                        "Value for 'is_empty' operator must be a boolean."));
                }
                return;
            }

            if (value.ValueKind != System.Text.Json.JsonValueKind.String)
            {
                errors.Add(new RulesetValidationError($"{path}.value", "invalid_value",
                    $"Value for '{property}' with operator '{op}' must be a string."));
                return;
            }

            if (op == "matches_regex")
            {
                string? pattern = value.GetString();
                if (!IsValidRegex(pattern))
                {
                    errors.Add(new RulesetValidationError($"{path}.value", "invalid_regex",
                        $"Value for 'matches_regex' is not a valid regular expression."));
                }
            }
        }
    }

    private static bool IsValidTimeString(string value)
    {
        if (value.Length != 5 || value[2] != ':')
        {
            return false;
        }

        return int.TryParse(value.AsSpan(0, 2), out int hour) &&
               int.TryParse(value.AsSpan(3, 2), out int minute) &&
               hour is >= 0 and <= 23 &&
               minute is >= 0 and <= 59;
    }

    private static bool IsValidRegex(string? pattern)
    {
        if (pattern is null)
        {
            return false;
        }

        try
        {
            _ = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (RegexMatchTimeoutException)
        { // TODO: Consider reporting this as a separate "regex_too_complex" error instead of just invalid.
            return false;
        }
    }

    private static void ValidateEffect(RulesetEffect effect, List<RulesetValidationError> errors)
    {
        bool hasAnyField = effect.Name is not null ||
                           effect.Description is not null ||
                           effect.SportType is not null ||
                           effect.GearId is not null ||
                           effect.Commute is not null ||
                           effect.Trainer is not null ||
                           effect.HideFromHome is not null;

        if (!hasAnyField)
        {
            errors.Add(new RulesetValidationError("effect", "effect_empty",
                "Effect must set at least one field."));
        }

        if (effect.Name is not null && !HasBalancedBraces(effect.Name))
        {
            errors.Add(new RulesetValidationError("effect.name", "unbalanced_braces",
                "Template string has unbalanced braces."));
        }

        if (effect.Description is not null && !HasBalancedBraces(effect.Description))
        {
            errors.Add(new RulesetValidationError("effect.description", "unbalanced_braces",
                "Template string has unbalanced braces."));
        }
    }

    private static bool HasBalancedBraces(string template)
    {
        int depth = 0;
        foreach (char c in template)
        {
            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth < 0)
                {
                    return false;
                }
            }
        }

        return depth == 0;
    }
}
