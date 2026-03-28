# Agent Guide — StravaEditBot

A bot that edits Strava activities. Learning project — the developer knows Python web dev well; Python analogies are helpful when explaining .NET concepts.

---

## Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core Web API (.NET 10) |
| Database | SQL Server, EF Core 10 |
| Auth — identity | ASP.NET Core Identity (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`) |
| Auth — API tokens | JWT bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Validation | FluentValidation 12, `AddFluentValidationAutoValidation` |
| API Docs | Swagger / OpenAPI (Swashbuckle + `MapOpenApi`) |
| Error handling | `IExceptionHandler` + `ProblemDetails` (RFC 9457) |

**Planned**: React frontend, Strava OAuth integration.

---

## Project Structure

```
StravaEditBotApi/
  Controllers/
    ActivitiesController.cs        # CRUD for activities
    AuthController.cs              # register, login, refresh, logout
  Services/
    IActivityService / ActivityService
    ITokenService / TokenService   # JWT + refresh token crypto
  DTOs/                            # Records: CreateActivityDto, UpdateActivityDto,
                                   # RegisterDto, LoginDto, AuthResponseDto
  Models/
    Activity.cs
    AppUser.cs        # extends IdentityUser
    RefreshToken.cs   # TokenHash (SHA-256), ExpiresAt, RevokedAt, IsActive
  Validators/         # FluentValidation AbstractValidator<T>
  Middleware/
    GlobalExceptionHandler.cs
    DevBypassAuthenticationHandler.cs   # auto-authenticates in Development only
  Data/
    AppDbContext.cs   # extends IdentityDbContext<AppUser>
  Migrations/         # do not edit by hand

StravaEditBotApi.Tests/
  Unit/Controllers / Services / Validators
  Integration/
    WebAppFactory.cs      # UseEnvironment("Development"); swaps SQL Server → InMemory
    TestAuthHandler.cs    # auto-authenticates all requests
```

---

## Architecture Principles

- **Thin controllers**: HTTP concerns only — routing, status codes, mapping. Logic in services.
- **Interface-driven services**: every service has `IXxxService`. Use it for DI and mocking.
- **Primary constructor injection**: `public class Foo(IBar bar) { }`.
- **Async all the way**: all I/O is `async Task<T>`. Never `.Result` or `.Wait()`.
- **DTOs separate from entities**: don't return EF entities from controllers (currently `Activity` is returned directly — known issue).

---

## Auth

**In Development**, `DevBypassAuthenticationHandler` auto-authenticates every request — no token needed. In all other environments, real JWT validation is enforced.

**User identity**: `UserManager<AppUser>` for creating users and checking passwords. Uses `AddIdentityCore` (not `AddIdentity`) to avoid cookie auth becoming the default scheme.

**Tokens**:
- Access token: JWT, 15 min, returned as `{ "accessToken": "..." }`. Client sends as `Authorization: Bearer <token>`.
- Refresh token: 64 random bytes, sent/stored as httpOnly cookie `refreshToken`. DB stores SHA-256 hash only. 7-day lifetime, rotated on every use.

**Config**: `Jwt:Issuer` and `Jwt:Audience` in `appsettings.json`. `Jwt:Secret` in user secrets (local) or Key Vault (Azure) — never in a file.

**Azure**: Managed Identity (Entra ID) is used only for App Service → Azure SQL access. It is not related to user auth.

---

## Testing

**Stack**: NUnit + NSubstitute + NUnit assertions (`Assert.That`). Do not use xUnit, Moq, or FluentAssertions.

**Naming**: `MethodName_Scenario_ExpectedBehaviour`

**Controller tests** — NSubstitute mock of the service:
```csharp
[TestFixture]
public class ActivitiesControllerTests
{
    private IActivityService _service = null!;
    private ActivitiesController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<IActivityService>();
        _sut = new ActivitiesController(_service, Substitute.For<ILogger<ActivitiesController>>());
    }
}
```

**Service tests** — EF InMemory, no mocks:
```csharp
[SetUp]
public void SetUp()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    _context = new AppDbContext(options);
    _sut = new ActivityService(_context);
}
```

**NSubstitute quick ref**:
```csharp
sub.GetAsync(1).Returns(someObject);
sub.GetByIdAsync(999).Returns((Activity?)null);
sub.Received(1).CreateAsync(Arg.Any<CreateActivityDto>());
sub.DidNotReceive().DeleteAsync(999);
```

**Integration tests**: `WebAppFactory` removes all four EF service types before swapping in InMemory (`IDbContextOptionsConfiguration<T>`, `DbContextOptions<T>`, `DbContextOptions`, `AppDbContext`). Environment is pinned to `Development` so `DevBypassAuthenticationHandler` is registered; `TestAuthHandler` then overrides the default scheme.

---

## C# Conventions

These rules are enforced by `.editorconfig`. **Do not write code that violates them.**

### Braces and layout

- **Always use braces** on `if`, `else`, `for`, `foreach`, `while` — even single-line bodies (`csharp_prefer_braces = warning`).
- **Allman brace style**: opening brace on its own line for every block — classes, methods, `if`, `else`, `try`, etc. (`csharp_new_line_before_open_brace = all`).
- **One statement per line** — never `if (x) Foo(); Bar();` on one line (`csharp_preserve_single_line_statements = false`).
- `using` directives go **outside** the namespace (`csharp_using_directive_placement = outside_namespace:warning`).
- **File-scoped namespaces**: `namespace StravaEditBotApi.Controllers;` — no enclosing `{}`.

```csharp
// CORRECT
if (value is null)
{
    return NotFound();
}

// WRONG — no braces
if (value is null)
    return NotFound();

// WRONG — K&R style
if (value is null) {
    return NotFound();
}
```

### `var` usage

- **Do NOT use `var`** for built-in / primitive types (`int`, `string`, `bool`, `double`, …). Write the type explicitly (`csharp_style_var_for_built_in_types = false`).
- **Use `var`** when the type is obvious from the right-hand side (e.g. `new`, cast, factory method) (`csharp_style_var_when_type_is_apparent = true`).
- Elsewhere, `var` is acceptable but explicit types are fine too.

```csharp
// CORRECT
string name = "Alice";
int count = 0;
var activity = new Activity();
var result = await _service.GetByIdAsync(id);

// WRONG
var name = "Alice";
var count = 0;
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

### Type keywords

Use language keywords, not BCL type names (`dotnet_style_predefined_type_for_locals_parameters_members = true`).

```csharp
// CORRECT
string name;
int count;
object obj;

// WRONG
String name;
Int32 count;
Object obj;
```

### Null checks and pattern matching

- Prefer `is null` / `is not null` over `== null` / `!= null`.
- Prefer pattern matching (`is`, `switch` expressions) over `as`-then-null-check or explicit casts.
- Nullable enabled — annotate `T?` and handle nulls explicitly.

```csharp
// CORRECT
if (activity is null) { ... }
if (result is not null) { ... }

// WRONG
if (activity == null) { ... }
```

### Expression-bodied members

Prefer expression bodies for **properties and accessors** (suggestion); avoid them for **methods and constructors**.

```csharp
// CORRECT — property
public string FullName => $"{First} {Last}";

// CORRECT — method stays block-bodied
public async Task<Activity?> GetByIdAsync(int id)
{
    return await _context.Activities.FindAsync(id);
}
```

### Other

- No `this.` qualification on members.
- Prefer `readonly` fields where the value never changes after construction.
- Use null-coalescing (`??`, `??=`) and null-conditional (`?.`) operators where appropriate.
- Records for DTOs: `public record CreateActivityDto(string Name, ...)`
- `Async` suffix on all async methods; controller sets `SuppressAsyncSuffixInActionNames = false`.

---

## Key Commands

```bash
docker compose up -d                                          # start SQL Server
dotnet build
dotnet run --project StravaEditBotApi                        # http://localhost:5247
dotnet test
dotnet ef migrations add <Name> --project StravaEditBotApi
dotnet ef database update --project StravaEditBotApi
```

---

## Known Issues

1. Controllers return `Activity` entity directly — should use response DTOs
2. No pagination on `GetAllAsync`
3. `Activities` not yet scoped to `UserId` — all users see all activities
