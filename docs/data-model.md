# Data Model — Ruleset Engine

This document defines the database entities for the StravaEditBot ruleset engine. The old `Activity` entity/table is replaced by this model.

---

## Entity Relationship Overview

```
AppUser 1──* Ruleset *──? RulesetTemplate
   │            │
   │            │ (each run logs)
   │            ▼
   │      RulesetRun
   │
   └──* CustomVariable
```

```
RulesetTemplate (standalone, shareable, bundles variable definitions)
    │
    └── can be instantiated as ──▶ Ruleset (owned by user)
                                   + CustomVariable(s) created for user
```

> **Detailed type definitions** for Filter, Effect, and Custom Variables are in [filter-effect-types.md](filter-effect-types.md).

---

## Entities

### 1. Ruleset

A user-owned automation rule. When a Strava activity is uploaded, the app evaluates rulesets in priority order and applies the first match.

| Column | Type | Notes |
|---|---|---|
| `Id` | `int` | PK, auto-increment |
| `UserId` | `string` | FK → `AppUser.Id`, required |
| `Name` | `string` | User-facing label, max 200 chars |
| `Description` | `string?` | Optional longer description |
| `Priority` | `int` | Lower = higher priority. Unique per user. Determines evaluation order. |
| `IsEnabled` | `bool` | Default `true`. Disabled rulesets are skipped during evaluation. |
| `Filter` | `string` (JSON) | JSON expression tree defining when this rule matches. See [Filter Schema](#filter-schema). |
| `Effect` | `string` (JSON) | JSON object defining what to change on the activity. See [Effect Schema](#effect-schema). |
| `CreatedFromTemplateId` | `int?` | FK → `RulesetTemplate.Id`, nullable. Tracks origin but no ongoing link. |
| `CreatedAt` | `DateTime` | UTC timestamp |
| `UpdatedAt` | `DateTime` | UTC timestamp |

**Constraints:**
- Unique index on `(UserId, Priority)` — no two rulesets share the same priority for a user
- Index on `UserId` for fast lookup

**Navigation:**
- `User` → `AppUser`
- `CreatedFromTemplate` → `RulesetTemplate?`
- `Runs` → `ICollection<RulesetRun>`

---

### 2. RulesetTemplate

A shareable, standalone definition of a rule. Can be user-created (shared via link) or system-predefined (seeded at startup). Templates are snapshots — editing the original ruleset does not change the template.

| Column | Type | Notes |
|---|---|---|
| `Id` | `int` | PK, auto-increment |
| `Name` | `string` | Display name, max 200 chars |
| `Description` | `string?` | What this template does, shown in marketplace |
| `Filter` | `string` (JSON) | Same schema as `Ruleset.Filter` |
| `Effect` | `string` (JSON) | Same schema as `Ruleset.Effect` |
| `CreatedByUserId` | `string?` | FK → `AppUser.Id`, nullable. NULL for system-predefined templates. |
| `IsPublic` | `bool` | If `true`, visible in the public marketplace. If `false`, only accessible via direct link. |
| `ShareToken` | `string?` | Unique URL-safe token for link sharing. Generated on creation. |
| `UsageCount` | `int` | How many users have created a ruleset from this template. Denormalized for display. |
| `BundledVariables` | `string?` (JSON) | `List<CustomVariableDefinition>` — snapshot of custom variable definitions used by this template's effect. NULL if no custom variables are referenced. |
| `CreatedAt` | `DateTime` | UTC timestamp |

**Constraints:**
- Unique index on `ShareToken` (where not null)

**Navigation:**
- `CreatedByUser` → `AppUser?`
- `Rulesets` → `ICollection<Ruleset>` (rulesets created from this template)

---

### 3. CustomVariable

A user-defined template variable. Evaluated at runtime as an ordered case list: first matching case wins, with a required default fallback. See [filter-effect-types.md](filter-effect-types.md) for the full `CustomVariableDefinition` type and examples.

| Column | Type | Notes |
|---|---|---|
| `Id` | `int` | PK, auto-increment |
| `UserId` | `string` | FK → `AppUser.Id`, required |
| `Name` | `string` | Variable name, max 50 chars, pattern `[a-z][a-z0-9_]*` |
| `Description` | `string?` | Human-readable description |
| `Definition` | `string` (JSON) | Serialized `CustomVariableDefinition` (cases + default_value) |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

**Constraints:**
- Unique index on `(UserId, Name)` — no duplicate variable names per user
- `Name` must not collide with built-in variable names (application-layer validation)
- Max 50 custom variables per user
- Max 20 cases per variable

**Navigation:**
- `User` → `AppUser`

---

### 4. RulesetRun

A log entry for each activity processed by the webhook, regardless of whether a ruleset matched. This gives users visibility into what the bot is doing.

| Column | Type | Notes |
|---|---|---|
| `Id` | `long` | PK, auto-increment |
| `UserId` | `string` | FK → `AppUser.Id` |
| `StravaActivityId` | `long` | The Strava activity ID that was evaluated |
| `RulesetId` | `int?` | FK → `Ruleset.Id`, nullable. NULL if no ruleset matched. |
| `Status` | `string` | Enum stored as string: `NoMatch`, `Applied`, `Failed`, `Skipped` |
| `ErrorMessage` | `string?` | Error details if `Status == Failed` |
| `FieldsChanged` | `string?` (JSON) | JSON object listing which fields were changed and to what values. NULL if no match. |
| `ProcessedAt` | `DateTime` | UTC timestamp when the webhook event was processed |
| `StravaEventTime` | `DateTime` | UTC timestamp from the Strava webhook event |

**Status values:**
- `NoMatch` — Activity was evaluated but no ruleset's filter matched
- `Applied` — A ruleset matched and the activity was successfully updated
- `Failed` — A ruleset matched but the Strava API update failed
- `Skipped` — Processing was skipped (e.g., user has no enabled rulesets, token expired)

**Constraints:**
- Index on `(UserId, ProcessedAt)` for paginated history queries
- Index on `StravaActivityId` for deduplication

**Navigation:**
- `User` → `AppUser`
- `Ruleset` → `Ruleset?`

---

## Filter Schema

The `Filter` column stores a JSON expression tree. The tree supports three logical operators (`and`, `or`, `not`) and leaf-level condition checks.

### Structure

```jsonc
// Logical operators combine children
{
  "type": "and",            // "and" | "or"
  "conditions": [ ... ]     // Array of nested conditions or operators
}

{
  "type": "not",
  "condition": { ... }      // Single nested condition or operator
}

// Leaf conditions check a property of the activity
{
  "type": "check",
  "property": "sport_type",
  "operator": "in",
  "value": ["Run", "TrailRun"]
}
```

### Available checks

Each check has a `property`, `operator`, and `value`. The available combinations:

**Core activity properties:**

| Property | Operators | Value type | Description |
|---|---|---|---|
| `sport_type` | `in`, `not_in` | `string[]` (SportType enum names) | Activity sport type |
| `workout_type` | `in`, `not_in` | `int[]` (0=default, 1=race, 2=long run, 3=workout, 11=race ride, 12=workout ride) | Strava workout type |
| `gear_id` | `eq`, `not_eq`, `in`, `not_in`, `is_null` | `string` or `string[]` (`is_null` takes `bool`) | Gear used for the activity |

**Location:**

| Property | Operators | Value type | Description |
|---|---|---|---|
| `start_location` | `within_radius` | `{ lat: number, lng: number, radius_meters: number }` | Start point within radius of coordinates |
| `end_location` | `within_radius` | `{ lat: number, lng: number, radius_meters: number }` | End point within radius of coordinates |
| `has_location_data` | `eq` | `bool` | Whether start/end lat/lng are present |
| `timezone` | `eq`, `contains` | `string` | Activity timezone (e.g. "(GMT+01:00) Europe/Oslo") |

**Time:**

| Property | Operators | Value type | Description |
|---|---|---|---|
| `start_time` | `after`, `before` | `string` (HH:mm, 24h format) | Local time of day the activity started |
| `day_of_week` | `in`, `not_in` | `string[]` ("Monday"..."Sunday") | Day of week of the activity |
| `month` | `in`, `not_in` | `int[]` (1–12) | Month of the activity start date |

**Distance & duration:**

| Property | Operators | Value type | Description |
|---|---|---|---|
| `distance_meters` | `gt`, `lt`, `gte`, `lte` | `number` | Total distance in meters |
| `elapsed_time_seconds` | `gt`, `lt`, `gte`, `lte` | `number` | Total elapsed time in seconds |
| `moving_time_seconds` | `gt`, `lt`, `gte`, `lte` | `number` | Moving time in seconds |
| `stopped_time_seconds` | `gt`, `lt`, `gte`, `lte` | `number` | Computed: elapsed − moving time. Useful for commute heuristics. |

**Elevation:**

| Property | Operators | Value type | Description |
|---|---|---|---|
| `total_elevation_gain` | `gt`, `lt`, `gte`, `lte` | `number` | Elevation gain in meters |
| `elev_high` | `gt`, `lt`, `gte`, `lte` | `number` | Peak elevation in meters |
| `elevation_per_km` | `gt`, `lt`, `gte`, `lte` | `number` | Computed: total_elevation_gain / (distance / 1000). Hilliness metric. |

**Speed & power:**

| Property | Operators | Value type | Description |
|---|---|---|---|
| `average_speed` | `gt`, `lt`, `gte`, `lte` | `number` | Average speed in m/s |
| `max_speed` | `gt`, `lt`, `gte`, `lte` | `number` | Max speed in m/s |
| `average_watts` | `gt`, `lt`, `gte`, `lte` | `number` | Average power output in watts (rides with power data) |
| `has_power_meter` | `eq` | `bool` | Whether power data is from a real device (vs estimated) |

**Flags:**

| Property | Operators | Value type | Description |
|---|---|---|---|
| `is_commute` | `eq` | `bool` | Whether the activity is flagged as commute |
| `is_trainer` | `eq` | `bool` | Whether recorded on a trainer |
| `is_manual` | `eq` | `bool` | Whether the activity was manually created (no GPS/device) |
| `is_private` | `eq` | `bool` | Whether the activity is private |

**Text:**

| Property | Operators | Value type | Description |
|---|---|---|---|
| `name` | `contains`, `starts_with`, `matches_regex` | `string` | Activity name text matching |
| `description` | `contains`, `starts_with`, `matches_regex`, `is_empty` | `string` (`is_empty` takes `bool`) | Activity description text matching |

**Social/group:**

| Property | Operators | Value type | Description |
|---|---|---|---|
| `athlete_count` | `gt`, `lt`, `gte`, `lte` | `int` | Number of athletes on the activity. >1 = group activity. |

### Example

"Runs that start within 500m of my house, on weekday mornings":

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
    },
    {
      "type": "check",
      "property": "day_of_week",
      "operator": "not_in",
      "value": ["Saturday", "Sunday"]
    },
    {
      "type": "check",
      "property": "start_time",
      "operator": "before",
      "value": "12:00"
    }
  ]
}
```

---

## Effect Schema

The `Effect` column stores a JSON object defining which activity fields to set. Only fields present in the object are changed. Values can be static or template strings with `{variable}` interpolation.

### Structure

```jsonc
{
  "name": "Morning {sport_type} from home",        // Template string
  "description": "{distance_km}km in {elapsed_time_human}",
  "sport_type": "Run",                              // Static value
  "gear_id": "b12345",
  "commute": true,
  "trainer": false,
  "hide_from_home": false
}
```

### Editable fields

These map to the Strava Update Activity API (`UpdatableActivity`):

| Field | Type | Notes |
|---|---|---|
| `name` | `string` | Activity title. Supports template variables. |
| `description` | `string` | Activity description. Supports template variables. |
| `sport_type` | `string` | Must be a valid `SportType` enum value |
| `gear_id` | `string` | Strava gear ID. Use `"none"` to clear. |
| `commute` | `bool` | Flag as commute |
| `trainer` | `bool` | Flag as trainer activity |
| `hide_from_home` | `bool` | Hide from home feed |

### Template variables

Available in `name` and `description` fields. Resolved at runtime from the fetched activity data. See [filter-effect-types.md](filter-effect-types.md) for the complete reference and custom variable system.

#### Built-in variables

The full reference is in [filter-effect-types.md](filter-effect-types.md). Summary of available variables:

| Category | Variables |
|---|---|
| Activity identity | `{original_name}`, `{sport_type}`, `{gear_id}`, `{workout_type}` |
| Distance | `{distance_km}`, `{distance_mi}`, `{distance_m}` |
| Duration | `{elapsed_time_human}`, `{moving_time_human}`, `{stopped_time_human}`, `{elapsed_time_minutes}`, `{moving_time_minutes}` |
| Elevation | `{elevation_gain_m}`, `{elevation_gain_ft}`, `{elev_high_m}`, `{elevation_per_km}` |
| Speed & pace | `{average_speed_kmh}`, `{average_speed_mph}`, `{max_speed_kmh}`, `{average_pace_min_km}`, `{average_pace_min_mi}` |
| Power (rides) | `{average_watts}`, `{calories}`, `{kilojoules}` |
| Time & date | `{start_time}`, `{start_date}`, `{day_of_week}`, `{month_name}`, `{timezone}` |
| Social | `{athlete_count}` |

#### Custom variables

Users can define their own template variables (e.g., `{pace_label}`, `{time_of_day}`) as ordered case lists with filter conditions. These are stored in the `CustomVariable` table. See [filter-effect-types.md](filter-effect-types.md) for full type definitions, examples, and resolution rules.

Variables that cannot be resolved are left as-is in the string.

---

## Database Seeding — Predefined Templates

The app seeds these templates on first run (via EF Core `HasData` or a startup service):

### 1. Morning Commute Ride
- **Filter:** sport_type in [Ride, EBikeRide] AND start_time before 09:00 AND day_of_week not_in [Saturday, Sunday]
- **Effect:** commute = true, name = "Bike commute to work"

### 2. Evening Run Namer
- **Filter:** sport_type in [Run, TrailRun] AND start_time after 17:00
- **Effect:** name = "Evening {sport_type} — {distance_km}km"

### 3. Weekend Long Run
- **Filter:** sport_type in [Run, TrailRun] AND day_of_week in [Saturday, Sunday] AND distance_meters gt 15000
- **Effect:** name = "Long run — {distance_km}km, {elevation_gain_m}m gain"

### 4. Trainer Ride Labeler
- **Filter:** is_trainer eq true AND sport_type in [Ride, VirtualRide]
- **Effect:** name = "Indoor ride — {elapsed_time_human}", hide_from_home = true

### 5. Lunchtime Walk
- **Filter:** sport_type in [Walk] AND start_time after 11:00 AND start_time before 14:00
- **Effect:** name = "Lunch walk", commute = false

---

## Changes from Current Schema

| Current | New |
|---|---|
| `Activity` entity + table | **Remove entirely.** Was a test scaffold. |
| `ActivityService`, `ActivityController` | **Remove.** Replace with `RulesetService`, `RulesetController`, `RulesetRunController`. |
| `CreateActivityDto`, `UpdateActivityDto`, validators | **Remove.** Replace with DTOs for ruleset CRUD. |
| `SportTypes` constant (hardcoded 6 values) | **Remove.** Use `SportType` enum from StravaAPILibrary. |
| `AppUser` | **Keep.** Add navigation properties to `Rulesets`, `RulesetRuns`, `RulesetTemplates`, `CustomVariables`. |
| `RefreshToken` | **Keep as-is.** |
| _(new)_ | **Add** `CustomVariable` entity + table for user-defined template variables. |

---

## EF Core Configuration Notes

- `Filter` and `Effect` JSON columns: use `HasColumnType("nvarchar(max)")` with `HasConversion` to serialize/deserialize to C# types (`FilterExpression`, `RulesetEffect` — see [filter-effect-types.md](filter-effect-types.md))
- `CustomVariable.Definition` similarly stored as JSON → `CustomVariableDefinition`
- `RulesetTemplate.BundledVariables` stored as JSON → `List<CustomVariableDefinition>`
- `RulesetRun.FieldsChanged` similarly stored as JSON
- Priority reordering: when a user reorders rulesets, update priorities in a single transaction
- Soft delete is not needed — hard delete rulesets, but `RulesetRun` entries referencing deleted rulesets keep the `RulesetId` as NULL (set null on delete)
