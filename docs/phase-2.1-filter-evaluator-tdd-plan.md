# Phase 2.1 — FilterEvaluator TDD Plan

## Context

Phase 2.1 of the ruleset engine: build `IFilterEvaluator` / `FilterEvaluator` — a pure service that evaluates a `FilterExpression` tree against a `DetailedActivity` and returns `bool`. No DB, no DI deps beyond the class itself. Pure logic.

The property/operator set is large (30+ properties, 20+ operators) and will grow. User requirement: **adding a new property or operator must be a clean, localized change** — not scattered switch cases.

---

## Architecture: Registry-Based Dispatch

Instead of a giant switch in `EvaluateCheck`, use **static dictionaries** mapping property names to value extractors, grouped by value category. Each category has its own operator evaluation method.

### Categories & Registries

```
Category        | Registry type                                          | Operators
----------------|--------------------------------------------------------|---------------------------
Bool            | Dictionary<string, Func<DetailedActivity, bool?>>      | eq
Numeric         | Dictionary<string, Func<DetailedActivity, double?>>    | gt, lt, gte, lte
StringSet       | Dictionary<string, Func<DetailedActivity, string[]>>   | in, not_in (sport_type, workout_type, day_of_week, month — all resolve to string[])
String          | Dictionary<string, Func<DetailedActivity, string?>>    | eq, contains, starts_with, matches_regex, is_empty
StringId        | Dictionary<string, Func<DetailedActivity, string?>>    | eq, not_eq, in, not_in, is_null (gear_id)
Location        | Dictionary<string, Func<DetailedActivity, LatLng?>>    | within_radius
Time            | Dictionary<string, Func<DetailedActivity, TimeOnly>>   | after, before
```

**Adding a new property** = add one line to the appropriate dictionary.
**Adding a new operator** = add one case to the category's `Evaluate*Op` method.
**Adding a new category** = add a new dictionary + evaluator method + wire into `EvaluateCheck`.

### Dispatch Flow

```
Evaluate(FilterExpression, DetailedActivity)
  └─ pattern match: And/Or/Not → recurse
  └─ Check → EvaluateCheck(CheckFilter, DetailedActivity)
       └─ Try each registry in order:
            BoolProperties.TryGet → EvaluateBoolOp
            NumericProperties.TryGet → EvaluateNumericOp
            StringSetProperties.TryGet → EvaluateStringSetOp
            StringProperties.TryGet → EvaluateStringOp
            StringIdProperties.TryGet → EvaluateStringIdOp
            LocationProperties.TryGet → EvaluateLocationOp
            TimeProperties.TryGet → EvaluateTimeOp
            None found → return false
```

### Computed Properties

Computed values (stopped_time, elevation_per_km) are just lambda entries in the numeric dictionary:

```csharp
["stopped_time_seconds"] = a => a.ElapsedTime - a.MovingTime,
["elevation_per_km"] = a => a.Distance > 0 ? a.TotalElevationGain / (a.Distance / 1000.0) : null,
```

---

## Files

| File | Action |
|------|--------|
| `StravaEditBotApi/Services/Rulesets/IFilterEvaluator.cs` | **Create** — interface |
| `StravaEditBotApi/Services/Rulesets/FilterEvaluator.cs` | **Create** — implementation |
| `StravaEditBotApi.Tests/Unit/Services/FilterEvaluatorTests.cs` | **Create** — ~102 tests |
| `StravaEditBotApi/Program.cs` | **Modify** — add DI registration |

### Reference (read-only)
- `StravaEditBotApi/Models/Rules/FilterExpression.cs` — And/Or/Not/CheckFilter records
- `StravaAPILibrary/Models/Activities/DetailedActivity.cs` — activity model
- `StravaAPILibrary/Models/Common/LatLng.cs` — `{ Latitude: float, Longitude: float }`
- `StravaAPILibrary/Models/Enums/SportType.cs` — enum values

---

## Design Decisions

1. **Unknown property/operator → return `false`** (not throw). Validation catches these at save time; at runtime, silent no-match is safer than crashing webhook pipeline.
2. **String comparisons (contains, starts_with, timezone eq) → case-insensitive** (`OrdinalIgnoreCase`). Regex stays case-sensitive (user can add `(?i)`).
3. **Regex timeout: 1 second** — matches `RulesetValidator.IsValidRegex`. On `RegexMatchTimeoutException`, return `false`.
4. **`start_time` after/before: strict** — `after "07:30"` at exactly 07:30 → false.
5. **`And([])` → true** (vacuous truth), **`Or([])` → false** (vacuous any).

---

## TDD Test Groups (implementation order)

Each group: write RED tests first → GREEN minimal code → REFACTOR.

### Group 1: Logical Operators — 12 tests
*Drives: recursive `Evaluate` + minimal `EvaluateCheck` for `is_commute`*

| Test | Filter | Activity | Expected |
|------|--------|----------|----------|
| `Evaluate_SingleTrueCheck_ReturnsTrue` | is_commute eq true | Commute=true | true |
| `Evaluate_SingleFalseCheck_ReturnsFalse` | is_commute eq true | Commute=false | false |
| `Evaluate_AndFilter_AllTrue_ReturnsTrue` | And([true, true]) | — | true |
| `Evaluate_AndFilter_OneFalse_ReturnsFalse` | And([true, false]) | — | false |
| `Evaluate_AndFilter_Empty_ReturnsTrue` | And([]) | — | true |
| `Evaluate_OrFilter_OneTrue_ReturnsTrue` | Or([false, true]) | — | true |
| `Evaluate_OrFilter_AllFalse_ReturnsFalse` | Or([false, false]) | — | false |
| `Evaluate_OrFilter_Empty_ReturnsFalse` | Or([]) | — | false |
| `Evaluate_NotFilter_NegatesTrue_ReturnsFalse` | Not(true) | — | false |
| `Evaluate_NotFilter_NegatesFalse_ReturnsTrue` | Not(false) | — | true |
| `Evaluate_NestedAndInsideOr_EvaluatesCorrectly` | Or([And([t,f]), t]) | — | true |
| `Evaluate_DeeplyNested_NotAndOr_EvaluatesCorrectly` | Not(And([Or([f,t]), t])) | — | false |

### Group 2: Boolean Properties — 11 tests
*Drives: `BoolProperties` dictionary + `EvaluateBoolOp`*

| Test | Property | Activity Setup | Expected |
|------|----------|----------------|----------|
| `Evaluate_IsCommute_EqTrue_Matches` | is_commute eq true | Commute=true | true |
| `Evaluate_IsCommute_EqFalse_NoMatch` | is_commute eq false | Commute=true | false |
| `Evaluate_IsTrainer_EqTrue_Matches` | is_trainer eq true | Trainer=true | true |
| `Evaluate_IsManual_EqTrue_Matches` | is_manual eq true | Manual=true | true |
| `Evaluate_IsPrivate_EqTrue_Matches` | is_private eq true | Private=true | true |
| `Evaluate_HasLocationData_EqTrue_WithLatLng` | has_location_data eq true | StartLatLng set | true |
| `Evaluate_HasLocationData_EqTrue_NullLatLng` | has_location_data eq true | StartLatLng=null | false |
| `Evaluate_HasLocationData_EqFalse_NullLatLng` | has_location_data eq false | StartLatLng=null | true |
| `Evaluate_HasPowerMeter_EqTrue_DeviceWattsTrue` | has_power_meter eq true | DeviceWatts=true | true |
| `Evaluate_HasPowerMeter_EqTrue_DeviceWattsNull` | has_power_meter eq true | DeviceWatts=null | false |
| `Evaluate_HasPowerMeter_EqFalse_DeviceWattsFalse` | has_power_meter eq false | DeviceWatts=false | true |

### Group 3: Enum/Set Properties — 9 tests
*Drives: `StringSetProperties` dictionary + `EvaluateStringSetOp` (in/not_in)*

| Test | Property | Operator | Value | Activity | Expected |
|------|----------|----------|-------|----------|----------|
| `Evaluate_SportType_In_Matching` | sport_type | in | ["Run","TrailRun"] | SportType=Run | true |
| `Evaluate_SportType_In_NonMatching` | sport_type | in | ["Ride"] | SportType=Run | false |
| `Evaluate_SportType_NotIn_NonMatching` | sport_type | not_in | ["Ride"] | SportType=Run | true |
| `Evaluate_SportType_NotIn_Matching` | sport_type | not_in | ["Run"] | SportType=Run | false |
| `Evaluate_SportType_In_InvalidEnum` | sport_type | in | ["FlyingCarpet"] | SportType=Run | false |
| `Evaluate_WorkoutType_In_Matching` | workout_type | in | [1,2] | WorkoutType=1 | true |
| `Evaluate_WorkoutType_In_NonMatching` | workout_type | in | [3] | WorkoutType=1 | false |
| `Evaluate_WorkoutType_In_Null` | workout_type | in | [1] | WorkoutType=null | false |
| `Evaluate_WorkoutType_NotIn_Null` | workout_type | not_in | [1] | WorkoutType=null | true |

Note: `StringSetProperties` maps convert activity values to `string[]` for uniform `in`/`not_in` evaluation:
- `sport_type` → `[activity.SportType.ToString()]`
- `workout_type` → `activity.WorkoutType?.ToString()` → single-element array or empty
- `day_of_week` → `[activity.StartDateLocal.DayOfWeek.ToString()]`
- `month` → `[activity.StartDateLocal.Month.ToString()]`

### Group 4: Gear ID — 11 tests
*Drives: `StringIdProperties` dictionary + `EvaluateStringIdOp` (eq/not_eq/in/not_in/is_null)*

| Test | Operator | Value | GearId | Expected |
|------|----------|-------|--------|----------|
| `Evaluate_GearId_Eq_Matching` | eq | "b123" | "b123" | true |
| `Evaluate_GearId_Eq_NonMatching` | eq | "b999" | "b123" | false |
| `Evaluate_GearId_Eq_Null` | eq | "b123" | null | false |
| `Evaluate_GearId_NotEq_NonMatching` | not_eq | "b999" | "b123" | true |
| `Evaluate_GearId_NotEq_Matching` | not_eq | "b123" | "b123" | false |
| `Evaluate_GearId_In_Matching` | in | ["b123","b456"] | "b123" | true |
| `Evaluate_GearId_In_NonMatching` | in | ["b999"] | "b123" | false |
| `Evaluate_GearId_NotIn_NonMatching` | not_in | ["b999"] | "b123" | true |
| `Evaluate_GearId_IsNull_True_Null` | is_null | true | null | true |
| `Evaluate_GearId_IsNull_True_Present` | is_null | true | "b123" | false |
| `Evaluate_GearId_IsNull_False_Present` | is_null | false | "b123" | true |

### Group 5: Numeric Comparisons — 14 tests
*Drives: `NumericProperties` dictionary + `EvaluateNumericOp` (gt/lt/gte/lte)*

| Test | Property | Op | Threshold | Actual | Expected |
|------|----------|----|-----------|--------|----------|
| `Evaluate_DistanceMeters_Gt_Greater` | distance_meters | gt | 4000 | 5000 | true |
| `Evaluate_DistanceMeters_Gt_Equal` | distance_meters | gt | 5000 | 5000 | false |
| `Evaluate_DistanceMeters_Gte_Equal` | distance_meters | gte | 5000 | 5000 | true |
| `Evaluate_DistanceMeters_Lt_Less` | distance_meters | lt | 6000 | 5000 | true |
| `Evaluate_DistanceMeters_Lte_Equal` | distance_meters | lte | 5000 | 5000 | true |
| `Evaluate_ElapsedTimeSeconds_Gt` | elapsed_time_seconds | gt | 1000 | 1800 | true |
| `Evaluate_MovingTimeSeconds_Lt` | moving_time_seconds | lt | 2000 | 1700 | true |
| `Evaluate_TotalElevationGain_Gte` | total_elevation_gain | gte | 100 | 100 | true |
| `Evaluate_ElevHigh_Lte` | elev_high | lte | 200 | 200 | true |
| `Evaluate_AverageSpeed_Gt` | average_speed | gt | 2.5 | 2.78 | true |
| `Evaluate_MaxSpeed_Gt` | max_speed | gt | 3.5 | 4.0 | true |
| `Evaluate_AverageWatts_Gt_HasWatts` | average_watts | gt | 100 | 185 | true |
| `Evaluate_AverageWatts_Gt_Null` | average_watts | gt | 100 | null | false |
| `Evaluate_AthleteCount_Gt` | athlete_count | gt | 1 | 3 | true |

### Group 6: Computed Numeric — 5 tests
*Drives: computed lambdas in `NumericProperties` dictionary*

| Test | Property | Op | Threshold | Setup | Expected |
|------|----------|----|-----------|-------|----------|
| `Evaluate_StoppedTime_Gt` | stopped_time_seconds | gt | 50 | Elapsed=1800,Moving=1700 | true |
| `Evaluate_StoppedTime_Lt` | stopped_time_seconds | lt | 50 | stopped=100 | false |
| `Evaluate_ElevationPerKm_Gt` | elevation_per_km | gt | 15 | gain=100,dist=5000 (=20) | true |
| `Evaluate_ElevationPerKm_ZeroDistance` | elevation_per_km | gt | 0 | dist=0 | false |
| `Evaluate_ElevationPerKm_ZeroGainZeroDist` | elevation_per_km | gt | 0 | gain=0,dist=0 | false |

### Group 7: Location — 6 tests
*Drives: `LocationProperties` dictionary + `EvaluateLocationOp` + `HaversineMeters`*

| Test | Property | Setup | Expected |
|------|----------|-------|----------|
| `Evaluate_StartLocation_WithinRadius_Inside` | start_location | Same point, 500m radius | true |
| `Evaluate_StartLocation_WithinRadius_Outside` | start_location | ~10km apart, 500m radius | false |
| `Evaluate_StartLocation_WithinRadius_Boundary` | start_location | Distance ≈ radius | true |
| `Evaluate_StartLocation_NullLatLng` | start_location | StartLatLng=null | false |
| `Evaluate_EndLocation_WithinRadius_Inside` | end_location | EndLatLng matches | true |
| `Evaluate_EndLocation_NullLatLng` | end_location | EndLatLng=null | false |

### Group 8: Time Properties — 11 tests
*Drives: `TimeProperties` dictionary + `EvaluateTimeOp` + `EvaluateStringSetOp` for day_of_week/month*

| Test | Property | Op | Value | Activity | Expected |
|------|----------|----|-------|----------|----------|
| `Evaluate_StartTime_After_Match` | start_time | after | "07:00" | 07:30 | true |
| `Evaluate_StartTime_After_NoMatch` | start_time | after | "08:00" | 07:30 | false |
| `Evaluate_StartTime_Before_Match` | start_time | before | "08:00" | 07:30 | true |
| `Evaluate_StartTime_Before_NoMatch` | start_time | before | "07:00" | 07:30 | false |
| `Evaluate_StartTime_After_Exact` | start_time | after | "07:30" | 07:30 | false |
| `Evaluate_DayOfWeek_In_Match` | day_of_week | in | ["Wednesday"] | Wed | true |
| `Evaluate_DayOfWeek_In_NoMatch` | day_of_week | in | ["Monday"] | Wed | false |
| `Evaluate_DayOfWeek_NotIn_NoMatch` | day_of_week | not_in | ["Monday"] | Wed | true |
| `Evaluate_Month_In_Match` | month | in | [4] | April | true |
| `Evaluate_Month_In_NoMatch` | month | in | [1,2] | April | false |
| `Evaluate_Month_NotIn_NoMatch` | month | not_in | [1,2] | April | true |

### Group 9: String Properties — 17 tests
*Drives: `StringProperties` dictionary + `EvaluateStringOp` (contains/starts_with/matches_regex/eq/is_empty)*

| Test | Property | Op | Value | Activity | Expected |
|------|----------|----|-------|----------|----------|
| `Evaluate_Timezone_Eq_Match` | timezone | eq | "(GMT+01:00) Europe/Oslo" | "(GMT+01:00) Europe/Oslo" | true |
| `Evaluate_Timezone_Eq_NoMatch` | timezone | eq | "(GMT+09:00) Asia/Tokyo" | "(GMT+01:00) Europe/Oslo" | false |
| `Evaluate_Timezone_Contains_Match` | timezone | contains | "Oslo" | tz contains Oslo | true |
| `Evaluate_Timezone_Contains_NoMatch` | timezone | contains | "Tokyo" | — | false |
| `Evaluate_Name_Contains_Match` | name | contains | "Morning" | "Morning Run" | true |
| `Evaluate_Name_Contains_CaseInsensitive` | name | contains | "morning" | "Morning Run" | true |
| `Evaluate_Name_StartsWith_Match` | name | starts_with | "Morning" | "Morning Run" | true |
| `Evaluate_Name_StartsWith_CaseInsensitive` | name | starts_with | "morning" | "Morning Run" | true |
| `Evaluate_Name_StartsWith_NoMatch` | name | starts_with | "Evening" | "Morning Run" | false |
| `Evaluate_Name_MatchesRegex_Match` | name | matches_regex | "^Morning.*Run$" | "Morning Run" | true |
| `Evaluate_Name_MatchesRegex_NoMatch` | name | matches_regex | "^Evening" | "Morning Run" | false |
| `Evaluate_Description_Contains_Match` | description | contains | "great" | "A great run" | true |
| `Evaluate_Description_Contains_Null` | description | contains | "great" | null | false |
| `Evaluate_Description_IsEmpty_True_Null` | description | is_empty | true | null | true |
| `Evaluate_Description_IsEmpty_True_Empty` | description | is_empty | true | "" | true |
| `Evaluate_Description_IsEmpty_True_NonEmpty` | description | is_empty | true | "text" | false |
| `Evaluate_Description_IsEmpty_False_NonEmpty` | description | is_empty | false | "text" | true |

### Group 10: Edge Cases — 6 tests
*Drives: null guards, unknown property/operator handling, regex timeout*

| Test | Scenario | Expected |
|------|----------|----------|
| `Evaluate_CheckFilter_NullProperty_ReturnsFalse` | Property=null | false |
| `Evaluate_CheckFilter_NullOperator_ReturnsFalse` | Operator=null | false |
| `Evaluate_CheckFilter_NullValue_ReturnsFalse` | Value=null | false |
| `Evaluate_CheckFilter_UnknownProperty_ReturnsFalse` | "magic_field" | false |
| `Evaluate_CheckFilter_UnknownOperator_ReturnsFalse` | distance_meters + "magic_op" | false |
| `Evaluate_MatchesRegex_Timeout_ReturnsFalse` | Catastrophic backtracking `(a+)+$` | false |

---

## Test Helpers

```csharp
// Default activity with sensible values for most tests
private static DetailedActivity MakeActivity(Action<DetailedActivity>? customize = null)

// Concise CheckFilter construction
private static CheckFilter Check(string property, string op, object value)
    // wraps JsonSerializer.SerializeToElement(value)
```

Default activity values:
- SportType=Run, Distance=5000, ElapsedTime=1800, MovingTime=1700
- TotalElevationGain=100, ElevHigh=200, AverageSpeed=2.78, MaxSpeed=4.0
- StartDateLocal=2026-04-15 07:30 (Wednesday, April)
- Timezone="(GMT+01:00) Europe/Oslo"
- Name="Morning Run", Description="A great morning run"
- Commute=false, Trainer=false, Manual=false, Private=false
- AthleteCount=1, GearId="b1234567890"
- StartLatLng=(59.9139, 10.7522), EndLatLng=(59.9200, 10.7600)
- AverageWatts=null, DeviceWatts=null, WorkoutType=null

---

## Total: ~102 tests across 10 groups

| Group | Tests | Focus |
|-------|-------|-------|
| 1. Logical Operators | 12 | And/Or/Not recursion |
| 2. Boolean Properties | 11 | is_commute, is_trainer, etc |
| 3. Enum/Set Properties | 9 | sport_type, workout_type |
| 4. Gear ID | 11 | eq/not_eq/in/not_in/is_null |
| 5. Numeric Comparisons | 14 | gt/lt/gte/lte all numeric fields |
| 6. Computed Numeric | 5 | stopped_time, elevation_per_km |
| 7. Location | 6 | Haversine + within_radius |
| 8. Time Properties | 11 | start_time, day_of_week, month |
| 9. String Properties | 17 | contains, starts_with, regex, is_empty |
| 10. Edge Cases | 6 | Nulls, unknowns, regex timeout |

---

## Verification

```bash
# Run only FilterEvaluator tests
dotnet test --filter "FullyQualifiedName~FilterEvaluatorTests"

# Run all tests (ensure no regressions)
dotnet test
```
