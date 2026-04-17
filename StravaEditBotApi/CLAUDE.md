# StravaEditBotApi — Agent Guide

ASP.NET Core Web API. See root `CLAUDE.md` for project context and stack overview.

---

## Project structure

```
Controllers/
  AuthController.cs                   # /api/auth — /strava/callback, /refresh, /logout
  UsersController.cs                  # /api/users — /me
  WebhookController.cs                # /api/webhook — GET (handshake), POST (events)
  RulesetsController.cs               # /api/rulesets — CRUD, reorder, toggle, share, validate
  RulesetTemplatesController.cs       # /api/templates — public marketplace, share link, instantiate
  RulesetRunsController.cs            # /api/runs — paginated run history (populated by Phase 3)
  CustomVariablesController.cs        # /api/variables — CRUD for user-defined template variables
Services/
  Auth/
    ITokenService / TokenService               # JWT minting + refresh token crypto
    IStravaAuthService / StravaAuthService     # Strava OAuth token exchange
  Webhook/
    IWebhookService / WebhookService           # Strava webhook event processing
    WebhookBackgroundService                   # hosted service draining the webhook channel
  Rulesets/
    IRulesetValidator / RulesetValidator       # validates filter+effect, returns structured errors
    IFilterSanitizer / FilterSanitizer         # nulls out PII/user-specific values for sharing
    IRulesetService / RulesetService           # ruleset CRUD, priority management, sharing
    IRulesetTemplateService / RulesetTemplateService  # template marketplace, instantiation
    ICustomVariableService / CustomVariableService    # custom variable CRUD
DTOs/
  Auth/       AuthResponseDto, StravaCallbackDto, UserDto
  Webhook/    StravaWebhookEventDto
  Rulesets/   CreateRulesetDto, UpdateRulesetDto, ReorderRulesetsDto,
              RulesetResponseDto, ValidateRulesetDto
  Templates/  CreateTemplateFromRulesetDto, RulesetTemplateResponseDto,
              ShareRulesetResponseDto
  Runs/       RulesetRunResponseDto
  Variables/  CreateCustomVariableDto, UpdateCustomVariableDto, CustomVariableResponseDto
Models/
  AppUser.cs          # extends IdentityUser, has nav props to Rulesets/Runs/Templates/Variables
  RefreshToken.cs     # TokenHash (SHA-256), ExpiresAt, RevokedAt, IsActive
  Ruleset.cs          # user-owned automation rule with priority ordering
  RulesetTemplate.cs  # shareable/system-predefined rule definitions
  RulesetRun.cs       # log entry per webhook-processed activity
  CustomVariable.cs   # user-defined template variables with case logic
  Rules/
    FilterExpression.cs          # polymorphic tree: AndFilter, OrFilter, NotFilter, CheckFilter
    RulesetEffect.cs             # nullable-field effect POCO
    CustomVariableDefinition.cs  # cases + default_value
    ValidationTypes.cs           # RulesetValidationResult, RulesetValidationError
Validators/         # FluentValidation AbstractValidator<T> — one per DTO
Middleware/
  GlobalExceptionHandler.cs
  DevBypassAuthenticationHandler.cs   # auto-authenticates every request in Development
Data/
  AppDbContext.cs   # extends IdentityDbContext<AppUser>, configures JSON columns + value comparers
  DbSeeder.cs       # upserts system templates by SeedKey on startup
Migrations/         # EF Core migrations — do not edit by hand
```

---

## Architecture principles

- **Thin controllers**: HTTP concerns only — routing, status codes, model mapping. Logic lives in services.
- **Interface-driven services**: every service has `IXxxService`. Register and inject via interface.
- **Primary constructor injection**: `public class Foo(IBar bar) { }`
- **Async all the way**: all I/O is `async Task<T>`. Never `.Result` or `.Wait()`.
- **DTOs separate from entities**: use records for all DTOs. Never return EF entities from controllers.

---

## Auth

**In Development**, `DevBypassAuthenticationHandler` auto-authenticates every request — no token needed. All other environments enforce real JWT validation.

**User identity**: `UserManager<AppUser>` manages creation and password verification. Uses `AddIdentityCore` (not `AddIdentity`) to avoid cookie auth as default scheme, which conflicts with JWT bearer.

**Tokens**:
- Access token: JWT, 15 min lifetime, returned as `{ "accessToken": "..." }`. Client sends as `Authorization: Bearer <token>`.
- Refresh token: 64 random bytes, sent and stored as HttpOnly cookie `refreshToken`. DB stores SHA-256 hash only — raw token never persisted. 7-day lifetime, rotated on every use.

**Config keys**:
- `Jwt:Issuer`, `Jwt:Audience` — in `appsettings.json`
- `Jwt:Secret` — user secrets locally, Azure App Service env var in production. Never in a file.
- `Cors:AllowedOrigins` — comma-separated frontend origins. `appsettings.json` for dev (`http://localhost:5173`); overridden by `Cors__AllowedOrigins` env var in Azure (set by Bicep from SWA hostname).

**Azure auth**: Managed Identity (Entra ID) handles App Service → Azure SQL only. Unrelated to user auth.

---

## Ruleset engine

### Data model

Rulesets have `Filter` (`FilterExpression?`) and `Effect` (`RulesetEffect?`) stored as JSON in `nvarchar(max)` via EF `HasConversion`. Both nullable — null = draft/not-yet-configured.

`IsValid` (bool, default false) recomputed on every save by `IRulesetValidator`. Only valid rulesets evaluated at runtime.

`Priority` (int) unique per user. Lower = evaluated first. Managed by service layer — never set manually.

### CheckFilter nullable fields

`CheckFilter.Property`, `Operator`, `Value` all nullable to allow saving partial filters. Any null field → `IsValid = false`. Frontend should display incomplete checks as editable placeholders.

### EF JSON columns

`Filter`, `Effect`, `CustomVariable.Definition`, `RulesetTemplate.BundledVariables`, `RulesetRun.FieldsChanged` stored as JSON. Shared `JsonOptions` uses `SnakeCaseLower` naming and `WhenWritingNull` ignore.

Collection-type JSON columns (`FieldsChanged`, `BundledVariables`) have explicit `ValueComparer` configs (serialise-and-compare) so EF Core detects mutations correctly.

### FK cascade constraint

`RulesetRun.RulesetId` FK is `NO ACTION` (not `SET NULL`) — SQL Server disallows `SET NULL` here because both `AppUser→Rulesets` and `AppUser→RulesetRuns` already cascade on user delete. `RulesetService.DeleteAsync` handles nullification explicitly with `ExecuteUpdateAsync` before removing the ruleset.

### Validation

`IRulesetValidator.Validate(filter, effect)` returns `RulesetValidationResult(IsValid, List<RulesetValidationError>)`. Errors carry JSON-path-style `Path` (e.g., `filter.conditions[2].value`), machine-readable `Code`, human-readable `Message`.

Called on every ruleset create/update (updates `IsValid`). Also exposed at `POST /api/rulesets/validate` for real-time frontend feedback.

### Sharing sanitization

`IFilterSanitizer.SanitizeForSharing(filter)` walks tree, nulls `CheckFilter.Value` for `start_location`, `end_location`, `gear_id` checks. Returns sanitized filter plus list of sanitized property names for API response. Templates from sanitized rulesets start with `IsValid = false`, prompting users to fill own values.

### DbSeeder

`DbSeeder.SeedAsync(db)` runs on startup, upserts system templates by `RulesetTemplate.SeedKey`. New keys inserted, existing updated (name, description, filter, effect), removed keys deleted. User-created templates (`SeedKey == null`) never touched.

---

## C# conventions

Enforced by `.editorconfig`. **Do not write code that violates these rules.**

### Braces and layout

- **Always use braces** on `if`, `else`, `for`, `foreach`, `while` — even single-line bodies.
- **Allman brace style**: opening brace on own line for every block.
- **One statement per line**.
- `using` directives go **outside** namespace.
- **File-scoped namespaces**: `namespace StravaEditBotApi.Controllers;`

```csharp
// CORRECT
if (value is null)
{
    return NotFound();
}

// WRONG — no braces
if (value is null)
    return NotFound();

// WRONG — K&R brace style
if (value is null) {
    return NotFound();
}
```

### `var` usage

- **Do NOT use `var`** for built-in / primitive types (`int`, `string`, `bool`, `double`). Write type explicitly.
- **Use `var`** when type obvious from right-hand side (`new`, cast, factory method).

```csharp
string name = "Alice";          // CORRECT — primitive, explicit type
var activity = new Activity();  // CORRECT — type obvious from new
var name = "Alice";             // WRONG
```

### Naming

| Symbol | Convention | Example |
|---|---|---|
| Private / internal fields | `_camelCase` | `_tokenService` |
| Constants | `PascalCase` | `MaxRetries` |
| Types (class, struct, enum) | `PascalCase` | `ActivityService` |
| Interfaces | `IPascalCase` | `IActivityService` |
| Methods, properties, events | `PascalCase` | `GetByIdAsync` |
| Async methods | ends with `Async` | `CreateActivityAsync` |
| Local variables / parameters | `camelCase` | `activityId` |

### Null checks and pattern matching

- Prefer `is null` / `is not null` over `== null` / `!= null`.
- Prefer pattern matching over `as`-then-null-check or explicit casts.
- Nullable enabled — annotate `T?` and handle nulls explicitly.

```csharp
if (activity is null) { ... }    // CORRECT
if (activity == null) { ... }    // WRONG
```

### Other

- Use language keywords, not BCL names: `string` not `String`, `int` not `Int32`.
- Prefer expression bodies for properties; block bodies for methods.
- No `this.` qualification.
- Prefer `readonly` fields where value never changes after construction.
- Records for DTOs: `public record CreateActivityDto(string Name, ...)`

---

## Key commands

```bash
docker compose up -d                                          # start SQL Server
dotnet build
dotnet run --project StravaEditBotApi                        # http://localhost:5247
dotnet ef migrations add <Name> --project StravaEditBotApi
dotnet ef database update --project StravaEditBotApi
```