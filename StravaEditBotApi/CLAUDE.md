# StravaEditBotApi — Agent Guide

ASP.NET Core Web API. See the root `AGENTS.md` for project context and stack overview.

---

## Project structure

```
Controllers/
  ActivitiesController.cs             # CRUD for activities
  AuthController.cs                   # /strava/callback, /refresh, /logout
Services/
  IActivityService / ActivityService
  ITokenService / TokenService        # JWT minting + refresh token crypto
  IStravaAuthService / StravaAuthService  # Strava OAuth token exchange
DTOs/                                 # Records: CreateActivityDto, UpdateActivityDto,
                                      #          StravaCallbackDto, AuthResponseDto
Models/
  Activity.cs
  AppUser.cs        # extends IdentityUser
  RefreshToken.cs   # TokenHash (SHA-256), ExpiresAt, RevokedAt, IsActive
Validators/         # FluentValidation AbstractValidator<T> — one per DTO
Middleware/
  GlobalExceptionHandler.cs
  DevBypassAuthenticationHandler.cs   # auto-authenticates every request in Development
Data/
  AppDbContext.cs   # extends IdentityDbContext<AppUser>
Migrations/         # EF Core migrations — do not edit by hand
```

---

## Architecture principles

- **Thin controllers**: HTTP concerns only — routing, status codes, model mapping. All logic lives in services.
- **Interface-driven services**: every service has `IXxxService`. Register and inject via the interface.
- **Primary constructor injection**: `public class Foo(IBar bar) { }`
- **Async all the way**: all I/O is `async Task<T>`. Never `.Result` or `.Wait()`.
- **DTOs separate from entities**: use records for all DTOs. Do not return EF entities from controllers (currently `Activity` is returned directly — known issue).

---

## Auth

**In Development**, `DevBypassAuthenticationHandler` auto-authenticates every request — no token needed. In all other environments, real JWT validation is enforced.

**User identity**: `UserManager<AppUser>` manages creation and password verification. Uses `AddIdentityCore` (not `AddIdentity`) to avoid cookie auth becoming the default scheme, which would conflict with JWT bearer.

**Tokens**:
- Access token: JWT, 15 min lifetime, returned as `{ "accessToken": "..." }`. Client sends as `Authorization: Bearer <token>`.
- Refresh token: 64 random bytes, sent and stored as HttpOnly cookie `refreshToken`. DB stores SHA-256 hash only — the raw token is never persisted. 7-day lifetime, rotated on every use.

**Config keys**:
- `Jwt:Issuer`, `Jwt:Audience` — in `appsettings.json`
- `Jwt:Secret` — in user secrets locally, Azure App Service env var in production. Never in a file.
- `Cors:AllowedOrigins` — comma-separated frontend origins. In `appsettings.json` for dev (`http://localhost:5173`); overridden by `Cors__AllowedOrigins` env var in Azure (set automatically by Bicep from the SWA hostname).

**Azure auth**: Managed Identity (Entra ID) handles App Service → Azure SQL authentication only. It has nothing to do with user auth.

---

## C# conventions

Enforced by `.editorconfig`. **Do not write code that violates these rules.**

### Braces and layout

- **Always use braces** on `if`, `else`, `for`, `foreach`, `while` — even single-line bodies.
- **Allman brace style**: opening brace on its own line for every block.
- **One statement per line**.
- `using` directives go **outside** the namespace.
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

- **Do NOT use `var`** for built-in / primitive types (`int`, `string`, `bool`, `double`). Write the type explicitly.
- **Use `var`** when the type is obvious from the right-hand side (`new`, cast, factory method).

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
- Prefer `readonly` fields where the value never changes after construction.
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
