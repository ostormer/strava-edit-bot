# Filter, Effect & Custom Variable — C# Type Design

C# types for JSON serialization of the `Filter`, `Effect`, and custom template variable columns. These are POCOs — not EF entities — stored as JSON in `nvarchar(max)` columns.

---

## FilterExpression

Uses `System.Text.Json` polymorphic serialization via `[JsonPolymorphic]` + `[JsonDerivedType]`. The `type` discriminator maps to four concrete classes.

```csharp
using System.Text.Json.Serialization;

namespace StravaEditBotApi.Models.Rules;

/// <summary>
/// Base type for filter expression tree nodes.
/// Deserialized polymorphically from the "type" JSON discriminator.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AndFilter), "and")]
[JsonDerivedType(typeof(OrFilter), "or")]
[JsonDerivedType(typeof(NotFilter), "not")]
[JsonDerivedType(typeof(CheckFilter), "check")]
public abstract record FilterExpression;

/// <summary>
/// All child conditions must be true.
/// </summary>
public record AndFilter(
    [property: JsonPropertyName("conditions")]
    List<FilterExpression> Conditions
) : FilterExpression;

/// <summary>
/// At least one child condition must be true.
/// </summary>
public record OrFilter(
    [property: JsonPropertyName("conditions")]
    List<FilterExpression> Conditions
) : FilterExpression;

/// <summary>
/// Negates the child condition.
/// </summary>
public record NotFilter(
    [property: JsonPropertyName("condition")]
    FilterExpression Condition
) : FilterExpression;

/// <summary>
/// Leaf node: checks a single activity property against a value.
/// All fields are nullable to support saving incomplete/draft rulesets.
/// A fully filled CheckFilter is required for the ruleset to be valid.
/// The Value is a JsonElement because its shape varies by property/operator.
/// </summary>
public record CheckFilter(
    [property: JsonPropertyName("property")]
    string? Property,

    [property: JsonPropertyName("operator")]
    string? Operator,

    [property: JsonPropertyName("value")]
    JsonElement? Value
) : FilterExpression;
```

### CheckFilter — Property/Operator reference

**Core activity properties:**

| Property | Valid operators | Value (`JsonElement` shape) |
|---|---|---|
| `sport_type` | `in`, `not_in` | `string[]` — SportType enum names |
| `workout_type` | `in`, `not_in` | `int[]` — 0=default, 1=race, 2=long run, 3=workout, 11=race(ride), 12=workout(ride) |
| `gear_id` | `eq`, `not_eq`, `in`, `not_in`, `is_null` | `string` or `string[]` (`is_null` takes `bool`) |

**Location:**

| Property | Valid operators | Value (`JsonElement` shape) |
|---|---|---|
| `start_location` | `within_radius` | `{ "lat": number, "lng": number, "radius_meters": number }` |
| `end_location` | `within_radius` | `{ "lat": number, "lng": number, "radius_meters": number }` |
| `has_location_data` | `eq` | `bool` |
| `timezone` | `eq`, `contains` | `string` |

**Time:**

| Property | Valid operators | Value (`JsonElement` shape) |
|---|---|---|
| `start_time` | `after`, `before` | `string` — `"HH:mm"` 24h format |
| `day_of_week` | `in`, `not_in` | `string[]` — `"Monday"` ... `"Sunday"` |
| `month` | `in`, `not_in` | `int[]` — 1–12 |

**Distance & duration:**

| Property | Valid operators | Value (`JsonElement` shape) |
|---|---|---|
| `distance_meters` | `gt`, `lt`, `gte`, `lte` | `number` |
| `elapsed_time_seconds` | `gt`, `lt`, `gte`, `lte` | `number` |
| `moving_time_seconds` | `gt`, `lt`, `gte`, `lte` | `number` |
| `stopped_time_seconds` | `gt`, `lt`, `gte`, `lte` | `number` — computed: elapsed − moving |

**Elevation:**

| Property | Valid operators | Value (`JsonElement` shape) |
|---|---|---|
| `total_elevation_gain` | `gt`, `lt`, `gte`, `lte` | `number` — meters |
| `elev_high` | `gt`, `lt`, `gte`, `lte` | `number` — meters |
| `elevation_per_km` | `gt`, `lt`, `gte`, `lte` | `number` — computed: gain / (distance / 1000) |

**Speed & power:**

| Property | Valid operators | Value (`JsonElement` shape) |
|---|---|---|
| `average_speed` | `gt`, `lt`, `gte`, `lte` | `number` — m/s |
| `max_speed` | `gt`, `lt`, `gte`, `lte` | `number` — m/s |
| `average_watts` | `gt`, `lt`, `gte`, `lte` | `number` |
| `has_power_meter` | `eq` | `bool` |

**Flags:**

| Property | Valid operators | Value (`JsonElement` shape) |
|---|---|---|
| `is_commute` | `eq` | `bool` |
| `is_trainer` | `eq` | `bool` |
| `is_manual` | `eq` | `bool` |
| `is_private` | `eq` | `bool` |

**Text:**

| Property | Valid operators | Value (`JsonElement` shape) |
|---|---|---|
| `name` | `contains`, `starts_with`, `matches_regex` | `string` |
| `description` | `contains`, `starts_with`, `matches_regex`, `is_empty` | `string` (`is_empty` takes `bool`) |

**Social/group:**

| Property | Valid operators | Value (`JsonElement` shape) |
|---|---|---|
| `athlete_count` | `gt`, `lt`, `gte`, `lte` | `int` |

> `activity_type` is excluded — use `sport_type` only (Strava's own recommendation).
> `average_speed` added — needed for custom variable use cases (pace labels etc.)

### JSON example

```json
{
  "type": "and",
  "conditions": [
    {
      "type": "check",
      "property": "sport_type",
      "operator": "in",
      "value": ["Run", "TrailRun"]
    },
    {
      "type": "check",
      "property": "start_location",
      "operator": "within_radius",
      "value": { "lat": 59.9139, "lng": 10.7522, "radius_meters": 500 }
    }
  ]
}
```

### Deserialization

```csharp
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var filter = JsonSerializer.Deserialize<FilterExpression>(jsonString, options);
```

### Validation rules (FluentValidation or manual)

- Max nesting depth: 10 levels
- `CheckFilter.Property`, `Operator`, and `Value` are nullable (allows saving drafts) — but all three must be non-null for `IsValid = true`
- `CheckFilter.Property` must be a known property name
- `CheckFilter.Operator` must be valid for the given property
- `CheckFilter.Value` shape must match the property/operator pair
- `AndFilter`/`OrFilter` must have at least 1 condition
- `regex` patterns in `matches_regex` must be valid and have a timeout cap

See [data-model.md — Validation](data-model.md#validation) for the full validation check table and runtime behavior.

---

## RulesetEffect

A flat POCO where `null` fields mean "don't change this field". String fields support `{variable}` interpolation.

```csharp
using System.Text.Json.Serialization;

namespace StravaEditBotApi.Models.Rules;

/// <summary>
/// Defines which activity fields to edit and what values to set.
/// Null fields are not sent to the Strava API.
/// String fields support {variable} template interpolation.
/// </summary>
public record RulesetEffect
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("sport_type")]
    public string? SportType { get; init; }

    [JsonPropertyName("gear_id")]
    public string? GearId { get; init; }

    [JsonPropertyName("commute")]
    public bool? Commute { get; init; }

    [JsonPropertyName("trainer")]
    public bool? Trainer { get; init; }

    [JsonPropertyName("hide_from_home")]
    public bool? HideFromHome { get; init; }
}
```

### JSON example

```json
{
  "name": "Morning {sport_type} — {pace_label}",
  "description": "{distance_km}km in {elapsed_time_human}",
  "commute": true
}
```

### Mapping to Strava API

`EffectApplicator` maps a resolved `RulesetEffect` to `UpdateActivityAsync` parameters:

| Effect field | API parameter |
|---|---|
| `Name` | `name` |
| `Description` | `description` |
| `SportType` | `sportType` |
| `GearId` | `gearId` |
| `Commute` | `isCommute` |
| `Trainer` | `isTrainer` |
| `HideFromHome` | _(not yet in API helper — add to `UpdateActivityAsync`)_ |

---

## Custom Template Variables

### Concept

A user-defined variable is a named mapping from activity data to a string output. It works like a switch/case: an ordered list of cases, each with a filter condition and an output string. The first case whose condition matches wins. A default fallback is required.

Example — a `{pace_label}` variable:

| Case | Condition | Output |
|---|---|---|
| 1 | `average_speed` > 4.17 (15 km/h) | `"Fast"` |
| 2 | `average_speed` > 2.78 (10 km/h) | `"Moderate"` |
| _(default)_ | — | `"Slow"` |

Conditions reuse the same `FilterExpression` type from the ruleset filter system — so a case can use AND/OR/NOT logic if needed.

### C# types

```csharp
using System.Text.Json.Serialization;

namespace StravaEditBotApi.Models.Rules;

/// <summary>
/// A user-defined template variable.
/// Evaluated at runtime: cases are checked in order, first match wins.
/// </summary>
public record CustomVariableDefinition
{
    /// <summary>
    /// Variable name (without braces). Used as {name} in effect templates.
    /// Must match [a-z][a-z0-9_]*, max 50 chars.
    /// Must not collide with built-in variable names.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description of what this variable does.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Ordered list of conditional cases. First matching case wins.
    /// </summary>
    [JsonPropertyName("cases")]
    public required List<VariableCase> Cases { get; init; }

    /// <summary>
    /// Output value when no case matches. Required.
    /// </summary>
    [JsonPropertyName("default_value")]
    public required string DefaultValue { get; init; }
}

/// <summary>
/// A single case in a custom variable: "if condition, then output".
/// </summary>
public record VariableCase
{
    /// <summary>
    /// The condition to evaluate. Uses the same FilterExpression tree as ruleset filters.
    /// </summary>
    [JsonPropertyName("condition")]
    public required FilterExpression Condition { get; init; }

    /// <summary>
    /// The string value to output if the condition matches.
    /// May itself contain {variable} references (including built-ins),
    /// but NOT other custom variables (no recursive resolution).
    /// </summary>
    [JsonPropertyName("output")]
    public required string Output { get; init; }
}
```

### JSON example — user-defined `{pace_label}`

```json
{
  "name": "pace_label",
  "description": "Labels activity as Fast/Moderate/Slow based on average speed",
  "cases": [
    {
      "condition": {
        "type": "check",
        "property": "average_speed",
        "operator": "gt",
        "value": 4.17
      },
      "output": "Fast"
    },
    {
      "condition": {
        "type": "check",
        "property": "average_speed",
        "operator": "gt",
        "value": 2.78
      },
      "output": "Moderate"
    }
  ],
  "default_value": "Slow"
}
```

### Another example — `{time_of_day}` in Norwegian

```json
{
  "name": "time_of_day",
  "description": "Tid på dagen basert på starttidspunkt",
  "cases": [
    {
      "condition": {
        "type": "check",
        "property": "start_time",
        "operator": "before",
        "value": "06:00"
      },
      "output": "Natt"
    },
    {
      "condition": {
        "type": "check",
        "property": "start_time",
        "operator": "before",
        "value": "12:00"
      },
      "output": "Morgen"
    },
    {
      "condition": {
        "type": "check",
        "property": "start_time",
        "operator": "before",
        "value": "18:00"
      },
      "output": "Ettermiddag"
    }
  ],
  "default_value": "Kveld"
}
```

### Entity — CustomVariable

| Column | Type | Notes |
|---|---|---|
| `Id` | `int` | PK, auto-increment |
| `UserId` | `string` | FK → `AppUser.Id`, required |
| `Name` | `string` | Variable name, max 50 chars, `[a-z][a-z0-9_]*` |
| `Description` | `string?` | Human-readable description |
| `Definition` | `string` (JSON) | Serialized `CustomVariableDefinition` (cases + default) |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

**Constraints:**
- Unique index on `(UserId, Name)` — no duplicate variable names per user
- `Name` must not collide with built-in variable names (validated in application layer)

**Navigation:**
- `User` → `AppUser`

### Templates & custom variables

When a ruleset is shared as a template, any custom variables referenced by the effect's template strings are bundled into the template. This requires a new column on `RulesetTemplate`:

| Column | Type | Notes |
|---|---|---|
| `BundledVariables` | `string?` (JSON) | `List<CustomVariableDefinition>` — snapshot of variable definitions used by this template's effect |

When a user instantiates a template:
1. Create the `Ruleset` from the template's filter + effect (existing behavior)
2. For each bundled variable, check if the user already has a variable with that name:
   - If **no** → create a new `CustomVariable` for the user from the bundled definition
   - If **yes** → skip (user's existing definition takes precedence, they can edit it)
3. Inform the user which variables were created and which were skipped

### Resolution order during effect application

1. Parse template string for `{variable_name}` references
2. For each variable:
   - Check built-in variables first (e.g., `{distance_km}`, `{sport_type}`)
   - If not a built-in, look up user's `CustomVariable` by name
   - Evaluate the custom variable's cases against the activity
   - First matching case → use its output; no match → use default
3. Custom variable outputs may contain built-in `{variable}` references → resolve those in a second pass
4. No recursive custom variable references (custom vars cannot reference other custom vars — prevents cycles)
5. Unresolved variables are left as literal text (e.g., `{unknown_var}` stays as-is)

### Validation rules

- Variable name: `^[a-z][a-z0-9_]{0,49}$`
- Name must not match any built-in variable name
- At least 0 cases (a variable with only a default is valid — acts as a user-scoped constant)
- Max 20 cases per variable
- Max 50 custom variables per user
- Case conditions: same validation as ruleset filters (max depth 10, valid properties/operators)
- Output strings: max 500 chars each
- Default value: required, max 500 chars
- No `{custom_var}` references in output strings (only built-in vars allowed)

---

## Built-in variables — complete list

For reference, these are reserved names that custom variables cannot use:

**Activity identity:**

| Name | Output example | Source |
|---|---|---|
| `original_name` | `Morning Run` | Activity name before edit |
| `sport_type` | `Run` | `DetailedActivity.SportType` |
| `gear_id` | `b12345` | `DetailedActivity.GearId`, empty string if none |
| `workout_type` | `1` | `DetailedActivity.WorkoutType`, empty string if null |

**Distance:**

| Name | Output example | Source |
|---|---|---|
| `distance_km` | `5.2` | `Distance / 1000`, 1 decimal |
| `distance_mi` | `3.2` | `Distance / 1609.34`, 1 decimal |
| `distance_m` | `5230` | Raw meters, integer |

**Duration:**

| Name | Output example | Source |
|---|---|---|
| `elapsed_time_human` | `1h 45m 30s` | Formatted `ElapsedTime` |
| `moving_time_human` | `1h 42m 15s` | Formatted `MovingTime` |
| `stopped_time_human` | `3m 15s` | Formatted `ElapsedTime - MovingTime` |
| `elapsed_time_minutes` | `105` | `ElapsedTime / 60`, integer |
| `moving_time_minutes` | `102` | `MovingTime / 60`, integer |

**Elevation:**

| Name | Output example | Source |
|---|---|---|
| `elevation_gain_m` | `120` | Integer meters |
| `elevation_gain_ft` | `394` | Integer feet |
| `elev_high_m` | `1450` | Peak elevation, integer meters |
| `elevation_per_km` | `23.5` | Computed: gain / km, 1 decimal |

**Speed & pace:**

| Name | Output example | Source |
|---|---|---|
| `average_speed_kmh` | `12.5` | km/h, 1 decimal |
| `average_speed_mph` | `7.8` | mph, 1 decimal |
| `max_speed_kmh` | `25.3` | km/h, 1 decimal |
| `average_pace_min_km` | `4:48` | min:sec per km |
| `average_pace_min_mi` | `7:44` | min:sec per mile |

**Power (rides):**

| Name | Output example | Source |
|---|---|---|
| `average_watts` | `185` | Integer, empty string if no data |
| `calories` | `350` | Integer, empty string if null |
| `kilojoules` | `620` | Integer, empty string if null |

**Time & date:**

| Name | Output example | Source |
|---|---|---|
| `start_time` | `07:30` | Local time HH:mm |
| `start_date` | `2026-04-16` | Local date YYYY-MM-DD |
| `day_of_week` | `Wednesday` | Full English day name |
| `month_name` | `April` | Full English month name |
| `timezone` | `(GMT+01:00) Europe/Oslo` | Raw timezone string |

**Social:**

| Name | Output example | Source |
|---|---|---|
| `athlete_count` | `3` | Number of athletes on the activity |

---

## Validation Types

C# types returned by the `IRulesetValidator` service. These are not persisted — computed on demand.

```csharp
namespace StravaEditBotApi.Models.Rules;

/// <summary>
/// Result of validating a ruleset's filter and effect.
/// Returned by the validation endpoint and included in create/update responses.
/// </summary>
public record RulesetValidationResult(
    bool IsValid,
    List<RulesetValidationError> Errors
);

/// <summary>
/// A single validation error with a path pointing to the problematic node.
/// </summary>
public record RulesetValidationError(
    /// <summary>
    /// JSON-path-style pointer to the error location.
    /// Examples: "filter", "filter.conditions[2].value", "effect.name"
    /// </summary>
    string Path,

    /// <summary>
    /// Machine-readable error code.
    /// Examples: "filter_required", "incomplete_check", "invalid_operator"
    /// </summary>
    string Code,

    /// <summary>
    /// Human-readable description of the problem.
    /// </summary>
    string Message
);
```

See [data-model.md — Validation](data-model.md#validation) for the full list of checks and error codes.

---

## Sharing Sanitization

When a ruleset is shared as a template, certain `CheckFilter` values are nulled out to remove user-specific data. The check structure (property + operator) is preserved so recipients understand the ruleset's intent and can fill in their own values.

**Sanitized properties:** `start_location`, `end_location`, `gear_id`

See [data-model.md — Sharing Sanitization](data-model.md#sharing-sanitization) for full details and behavior.

---

## EF Core JSON column configuration

```csharp
// In AppDbContext.OnModelCreating or entity type configuration

// Ruleset
builder.Entity<Ruleset>(entity =>
{
    entity.Property(r => r.Filter)
        .HasColumnType("nvarchar(max)")
        .HasConversion(
            v => JsonSerializer.Serialize(v, JsonOptions),
            v => JsonSerializer.Deserialize<FilterExpression>(v, JsonOptions)!
        );

    entity.Property(r => r.Effect)
        .HasColumnType("nvarchar(max)")
        .HasConversion(
            v => JsonSerializer.Serialize(v, JsonOptions),
            v => JsonSerializer.Deserialize<RulesetEffect>(v, JsonOptions)!
        );
});

// CustomVariable
builder.Entity<CustomVariable>(entity =>
{
    entity.Property(cv => cv.Definition)
        .HasColumnType("nvarchar(max)")
        .HasConversion(
            v => JsonSerializer.Serialize(v, JsonOptions),
            v => JsonSerializer.Deserialize<CustomVariableDefinition>(v, JsonOptions)!
        );

    entity.HasIndex(cv => new { cv.UserId, cv.Name }).IsUnique();
});
```

Where `JsonOptions` is a shared instance configured with:
```csharp
private static readonly JsonSerializerOptions JsonOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
};
```

---

## Summary of schema changes vs. data-model.md

| Change | Detail |
|---|---|
| `Ruleset.Filter` | C# type is `FilterExpression` (not raw string) |
| `Ruleset.Effect` | C# type is `RulesetEffect` (not raw string) |
| New entity: `CustomVariable` | Per-user named variables with conditional case logic |
| `RulesetTemplate.BundledVariables` | New JSON column: `List<CustomVariableDefinition>?` |
| `activity_type` check | Removed from filter schema |
| `activity_type` built-in variable | Removed |
| `{name}` variable | Renamed to `{original_name}` |
| `average_speed` check property | Added to filter schema (needed for pace-based variables) |
