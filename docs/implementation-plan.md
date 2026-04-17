# Implementation Plan — Ruleset Engine

Phased plan for building out the core feature of StravaEditBot. Each phase is independently deployable.

---

## Phase 0: Cleanup — Remove test scaffolding

Remove the placeholder Activity system that was only there for testing.

**Delete source files:**
- `StravaEditBotApi/Models/Activity.cs`
- `StravaEditBotApi/Services/ActivityService.cs` + `IActivityService.cs`
- `StravaEditBotApi/Controllers/ActivitiesController.cs`
- `StravaEditBotApi/DTOs/CreateActivityDto.cs`, `UpdateActivityDto.cs`
- `StravaEditBotApi/Validators/CreateActivityDtoValidator.cs`, `UpdateActivityDtoValidator.cs`
- `StravaEditBotApi/Constants/SportTypes.cs`

**Delete test files:**
- `StravaEditBotApi.Tests/Unit/Services/ActivityServiceTests.cs`
- `StravaEditBotApi.Tests/Unit/Controllers/ActivitiesControllerTests.cs`
- `StravaEditBotApi.Tests/Unit/Validators/CreateActivityDtoValidatorTests.cs`
- `StravaEditBotApi.Tests/Integration/ActivitiesIntegrationTests.cs`

**Delete Bruno API collection:**
- `bruno/activities/` (all 6 `.bru` files — create, get-all, get-by-id, update, delete, test-error)

**Modify:**
- `AppDbContext.cs` — remove `DbSet<Activity>` property and unused `using` if applicable
- `Program.cs` — remove `IActivityService` DI registration (keep `using StravaEditBotApi.Services;` — other services use it)

**Create EF migration:** `RemoveActivityTable` to drop the `Activities` table. Do NOT delete the existing `InitialCreate` migration — it is historical.

**Update docs:** `StravaEditBotApi/CLAUDE.md` and `StravaEditBotApi.Tests/CLAUDE.md` to remove references to the Activity system.

---

## Phase 1: Data model & CRUD API

Build the database layer and REST API for managing rulesets.

### 1.1 — Entities & DB

- Create `Ruleset` entity (see [data-model.md](data-model.md))
  - `Filter` and `Effect` are nullable — supports saving incomplete/draft rulesets
  - `IsValid` bool column — recomputed on every save by the validator
- Create `RulesetTemplate` entity
- Create `RulesetRun` entity
- Create `CustomVariable` entity
- Create C# POCOs for JSON serialization (see [filter-effect-types.md](../docs/filter-effect-types.md)):
  - `FilterExpression` (polymorphic: `AndFilter`, `OrFilter`, `NotFilter`, `CheckFilter`)
  - `CheckFilter` fields are nullable (Property, Operator, Value) — allows saving partially-built filters
  - `RulesetEffect` (nullable fields for each editable property)
  - `CustomVariableDefinition` / `VariableCase` (ordered case list with filter conditions)
  - `RulesetValidationResult` / `RulesetValidationError` — validation return types (not persisted)
- Update `AppDbContext`: add `DbSet<Ruleset>`, `DbSet<RulesetTemplate>`, `DbSet<RulesetRun>`, `DbSet<CustomVariable>`
- Configure EF: JSON column conversions, indexes, relationships, cascade delete rules
- Create & apply migration
- Seed predefined templates (see data-model.md seeding section)

### 1.2 — DTOs & Validation

- `CreateRulesetDto` — Name, Filter (JSON), Effect (JSON), IsEnabled
- `UpdateRulesetDto` — All fields optional
- `ReorderRulesetsDto` — Ordered list of ruleset IDs (sets priorities)
- `RulesetResponseDto` — Full ruleset for API responses
- `RulesetRunResponseDto` — Run log entry
- `RulesetTemplateResponseDto` — Template for marketplace/sharing
- `CreateTemplateFromRulesetDto` — Name, Description, IsPublic
- `ValidateRulesetDto` — Filter (JSON, nullable), Effect (JSON, nullable) — body-only validation without saving
- `CreateCustomVariableDto` — Name, Description, Definition (JSON)
- `UpdateCustomVariableDto` — Description, Definition (JSON)
- `CustomVariableResponseDto` — Full variable for API responses
- Note: `RulesetResponseDto` includes `IsValid` bool and `ValidationErrors` list so frontend can show inline feedback
- FluentValidation validators:
  - Validate filter JSON structure (valid operators, known properties)
  - Validate effect JSON (only known fields, valid sport_type values)
  - Validate template strings have balanced braces
  - Validate custom variable definitions (name pattern, max cases, no name collisions with built-ins)

### 1.3 — Services

- `IRulesetService` / `RulesetService`:
  - `GetUserRulesetsAsync(userId)` — ordered by priority
  - `GetByIdAsync(userId, rulesetId)` — with ownership check
  - `CreateAsync(userId, dto)` — assigns next priority
  - `UpdateAsync(userId, rulesetId, dto)`
  - `DeleteAsync(userId, rulesetId)` — reorders remaining priorities
  - `ReorderAsync(userId, orderedIds)` — bulk priority update
  - `ToggleEnabledAsync(userId, rulesetId)`
- `IRulesetTemplateService` / `RulesetTemplateService`:
  - `GetPublicTemplatesAsync()` — marketplace listing
  - `GetByShareTokenAsync(token)` — link sharing
  - `CreateFromRulesetAsync(userId, rulesetId, dto)` — snapshot
  - `InstantiateAsync(userId, templateId)` — create ruleset from template
  - `IncrementUsageCountAsync(templateId)` — called on instantiation
- `IRulesetValidator` / `RulesetValidator`:
  - `RulesetValidationResult Validate(FilterExpression? filter, RulesetEffect? effect, List<CustomVariable>? userVariables)`
  - Checks filter completeness, property/operator validity, value shapes, effect non-empty, template string syntax, regex compilation, nesting depth
  - Returns structured errors with JSON-path-style pointers (e.g., `filter.conditions[2].value`)
  - Called on every ruleset create/update to compute `IsValid`
  - See [data-model.md — Validation](../docs/data-model.md#validation) for full check table
- `IFilterSanitizer` / `FilterSanitizer`:
  - `(FilterExpression, List<string>) SanitizeForSharing(FilterExpression filter)`
  - Walks filter tree, nulls out `value` on `start_location`, `end_location`, `gear_id` checks
  - Returns sanitized filter + list of sanitized property names (for user feedback)
  - Called by template creation endpoint
  - See [data-model.md — Sharing Sanitization](../docs/data-model.md#sharing-sanitization)
- `ICustomVariableService` / `CustomVariableService`:
  - `GetUserVariablesAsync(userId)` — list all
  - `GetByIdAsync(userId, variableId)` — with ownership check
  - `CreateAsync(userId, dto)` — validates name uniqueness and no built-in collision
  - `UpdateAsync(userId, variableId, dto)`
  - `DeleteAsync(userId, variableId)` — warn if variable is referenced in active rulesets

### 1.4 — Controllers

- `RulesetsController` (`/api/rulesets`):
  - `GET /` — list user's rulesets (ordered)
  - `GET /{id}` — single ruleset
  - `POST /` — create
  - `PUT /{id}` — update
  - `DELETE /{id}` — delete
  - `PUT /reorder` — reorder priorities
  - `PATCH /{id}/toggle` — enable/disable
  - `POST /{id}/share` — create template from ruleset (sanitizes PII from filter)
  - `POST /validate` — validate filter + effect without saving (real-time frontend feedback)
- `RulesetTemplatesController` (`/api/templates`):
  - `GET /` — list public templates (marketplace)
  - `GET /shared/{shareToken}` — get template by share link
  - `POST /{id}/use` — instantiate template as user's ruleset
- `RulesetRunsController` (`/api/runs`):
  - `GET /` — paginated run history for current user
  - `GET /{id}` — single run detail
  - **Note:** These endpoints will return empty results until Phase 3 generates run data. Deploying them early is fine — the frontend can show an empty state.
- `CustomVariablesController` (`/api/variables`):
  - `GET /` — list user's custom variables
  - `GET /{id}` — single variable
  - `POST /` — create
  - `PUT /{id}` — update
  - `DELETE /{id}` — delete
  - **Note:** Custom variables are only resolved at runtime in Phase 2 (template variable resolution). The CRUD API is in Phase 1 so users can define variables before the engine exists. Could optionally defer to Phase 2 if Phase 1 scope needs trimming.

---

## Phase 2: Filter evaluation engine

Build the runtime engine that evaluates filter expressions against Strava activity data.

**Note:** `SportTypes.cs` (deleted in Phase 0) contained a hardcoded sport type list used for validation. Phase 2's `sport_type` filter evaluator and Phase 1's effect validator both need valid sport types. Source these from the Strava API's `SportType` enum in `StravaAPILibrary` or define a new comprehensive constant.

### 2.1 — Filter evaluator

- `IFilterEvaluator` / `FilterEvaluator`:
  - `bool Evaluate(FilterExpression filter, DetailedActivity activity)`
  - Recursively evaluates the expression tree
  - `And` → all children must be true
  - `Or` → at least one child must be true
  - `Not` → negate child
  - `Check` → dispatch to property-specific evaluator

- Property evaluators (private methods or strategy pattern):
  - `sport_type`: enum comparison against `in`/`not_in` list
  - `workout_type`: integer set membership
  - `gear_id`: string equality, set membership, null check
  - `start_location` / `end_location`: Haversine distance calculation against `within_radius`
  - `has_location_data`: null check on `StartLatLng`
  - `timezone`: string equality/contains
  - `start_time`: parse `HH:mm`, compare with activity's `StartDateLocal`
  - `day_of_week`: compare `StartDateLocal.DayOfWeek`
  - `month`: compare `StartDateLocal.Month`
  - `distance_meters` / `elapsed_time_seconds` / `moving_time_seconds`: numeric comparison
  - `stopped_time_seconds`: computed (elapsed − moving), numeric comparison
  - `total_elevation_gain` / `elev_high`: numeric comparison
  - `elevation_per_km`: computed (gain / km), numeric comparison — guard against zero distance
  - `average_speed` / `max_speed`: numeric comparison (m/s)
  - `average_watts`: numeric comparison (nullable — false if no data)
  - `has_power_meter`: maps to `DeviceWatts` field
  - `is_commute` / `is_trainer` / `is_manual` / `is_private`: boolean equality
  - `name` / `description`: string operations (contains, starts_with, regex)
  - `description` additionally supports `is_empty` (null or empty string)
  - `athlete_count`: numeric comparison

### 2.2 — Effect applicator & template variable resolution

- `IEffectApplicator` / `EffectApplicator`:
  - `UpdatableActivity Apply(RulesetEffect effect, DetailedActivity activity, List<CustomVariable> userVariables)`
  - For each non-null field in the effect, resolve its value
  - Template string resolution (two-pass):
    1. Resolve `{variable}` references → check built-in map first, then user's custom variables
    2. For custom variable outputs that themselves contain `{built_in}` references → resolve those in a second pass
  - Custom variable resolution: evaluate cases in order using `FilterEvaluator`, first match wins, else default
  - No recursive custom-to-custom references (prevents cycles)
  - Build built-in variable map from `DetailedActivity` properties
  - Return the `UpdatableActivity` to send to Strava API
  - **Prerequisite:** Verify `StravaAPILibrary` has an `UpdateActivityAsync` method that accepts the fields we need to update (name, description, sport_type, gear_id, commute, trainer, hide_from_home). If not, extend the library first.

- `ITemplateVariableResolver` / `TemplateVariableResolver`:
  - `string Resolve(string template, DetailedActivity activity, List<CustomVariable> userVariables)`
  - Shared between effect applicator and any future preview functionality
  - Regex `\{(\w+)\}` → lookup chain: built-in → custom → leave as-is

### 2.3 — Strava token refresh

- Before making API calls, check `AppUser.StravaTokenExpiresAt`
- If expired, use `StravaAuthService` to refresh the token
- Update the stored tokens on the `AppUser`
- This is needed for both fetching the activity and updating it

---

## Phase 3: Webhook integration — The core loop

Wire the filter engine into the existing webhook pipeline.

### 3.1 — Ruleset execution service

- `IRulesetExecutionService` / `RulesetExecutionService`:
  - `ProcessActivityAsync(long stravaAthleteId, long stravaActivityId)`
  - Steps:
    1. Look up `AppUser` by `StravaAthleteId`
    2. Refresh Strava token if needed
    3. Fetch activity from Strava API (`GetActivityByIdAsync`)
    4. Deserialize into `DetailedActivity`
    5. Load user's enabled rulesets, ordered by priority
    6. Evaluate each ruleset's filter against the activity
    7. First match wins → apply effect
    8. Call Strava `UpdateActivityAsync` with the changes
    9. Log `RulesetRun` (success, failure, or no-match)

### 3.2 — Update WebhookService

- In `ProcessEventAsync`, for `("activity", "create")`:
  - Call `RulesetExecutionService.ProcessActivityAsync(ownerId, objectId)`
  - Handle errors gracefully — log `RulesetRun` with `Failed` status
- Consider also handling `("activity", "update")` events (optional, to re-evaluate if someone manually edits and the bot should re-apply)
  - **Safeguard:** never re-process an activity the bot itself just updated (prevent infinite loops). Track this via the `RulesetRun` table — if the most recent run for this activity was `Applied` within the last 60 seconds, skip.

### 3.3 — Retry & resilience

- Strava API has rate limits (100 requests per 15 min, 1000 per day per app)
- Add basic retry with exponential backoff for transient failures (429, 5xx)
- If rate limited, delay processing but don't lose the event (channel is in-memory; consider a persistent queue in the future)
- Log rate limit hits in `RulesetRun` as `Failed` with descriptive error
- **Risk:** In-memory channel loses events on app restart. At minimum, log all incoming webhook events before queuing so unprocessed events are traceable. Phase 5.4 addresses this with a persistent queue.

---

## Phase 4: Frontend — Ruleset management UI

### 4.1 — Ruleset list page

- Drag-and-drop reorderable list of user's rulesets
- Each card shows: name, enabled/disabled toggle, summary of filter & effect
- Actions: edit, delete, share
- "Create new" button + "Browse templates" button

### 4.2 — Ruleset editor

- Form with:
  - Name, description
  - **Filter builder:** visual UI for building the expression tree
    - Add condition → pick property → pick operator → enter value
    - Group conditions with AND/OR
    - NOT wrapper for negation
    - Nested groups for complex logic
  - **Effect editor:**
    - Toggle which fields to set
    - Text inputs with template variable autocomplete for name/description
    - Dropdowns for sport_type, gear_id
    - Checkboxes for commute, trainer, hide_from_home
  - Preview section: "When this rule matches, the activity will be edited to: ..."

### 4.3 — Template marketplace

- Grid/list of public templates with name, description, usage count
- "Use this template" button → creates a ruleset, redirects to editor for customization
- Share dialog: generates link for private templates

### 4.4 — Run history

- Paginated table of recent runs
- Columns: date, Strava activity link, matched ruleset (or "No match"), status, fields changed
- Filter by status, date range

---

## Phase 5: Polish & production readiness

### 5.1 — Security & authorization

- All ruleset endpoints scoped to authenticated user's data
- Template share tokens should be cryptographically random (e.g., `RandomNumberGenerator.GetBytes`)
- Validate that filter JSON can't contain excessively deep nesting (DoS prevention) — cap at 10 levels
- Rate limit ruleset creation (prevent abuse)

### 5.2 — Strava token encryption

- Address the known issue: encrypt `StravaAccessToken` and `StravaRefreshToken` at rest
- Use ASP.NET Core Data Protection API or a symmetric key from config

### 5.3 — Observability

- Structured logging throughout the execution pipeline
- Application Insights or similar for monitoring
- Dashboard: rulesets evaluated/day, success rate, Strava API call volume

### 5.4 — Persistent event queue (future)

- Replace the in-memory `Channel` with a durable queue (Azure Service Bus or a database-backed queue)
- Prevents event loss on app restart
- Enables horizontal scaling

---

## Dependency graph

```
Phase 0 (cleanup)
    │
    ▼
Phase 1 (data model + CRUD API)
    │
    ├──▶ Phase 2 (filter engine)  ───┐
    │                                │
    └──▶ Phase 4.1-4.3 (frontend)    │
              │                      │
              ▼                      ▼
         Phase 4.2 (editor)    Phase 3 (webhook integration)
              │                      │
              ▼                      ▼
         Phase 4.4 (run history) ◀───┘
              │
              ▼
         Phase 5 (polish)
```

Phase 2 can start after Phase 1.1 (entities) — it only needs the POCO types, not the full CRUD API. Frontend work (Phase 4.1-4.3) can start as soon as Phase 1 API is ready. Phase 3 requires both Phase 1 and Phase 2. Phase 4.4 (run history) requires Phase 3 to generate data.

---

## API Route Summary

| Method | Route | Description | Phase |
|---|---|---|---|
| `GET` | `/api/rulesets` | List user's rulesets | 1 |
| `GET` | `/api/rulesets/{id}` | Get single ruleset | 1 |
| `POST` | `/api/rulesets` | Create ruleset | 1 |
| `PUT` | `/api/rulesets/{id}` | Update ruleset | 1 |
| `DELETE` | `/api/rulesets/{id}` | Delete ruleset | 1 |
| `PUT` | `/api/rulesets/reorder` | Reorder priorities | 1 |
| `PATCH` | `/api/rulesets/{id}/toggle` | Toggle enabled | 1 |
| `POST` | `/api/rulesets/{id}/share` | Create template from ruleset | 1 |
| `POST` | `/api/rulesets/validate` | Validate filter + effect without saving | 1 |
| `GET` | `/api/templates` | List public templates | 1 |
| `GET` | `/api/templates/shared/{token}` | Get template by share link | 1 |
| `POST` | `/api/templates/{id}/use` | Create ruleset from template | 1 |
| `GET` | `/api/runs` | Paginated run history | 1 |
| `GET` | `/api/runs/{id}` | Single run detail | 1 |
| `GET` | `/api/variables` | List user's custom variables | 1 |
| `GET` | `/api/variables/{id}` | Get single variable | 1 |
| `POST` | `/api/variables` | Create custom variable | 1 |
| `PUT` | `/api/variables/{id}` | Update custom variable | 1 |
| `DELETE` | `/api/variables/{id}` | Delete custom variable | 1 |
