# Agent Guide — StravaEditBot

## Project Purpose

A learning project for building .NET backend skills to consultant level. The developer has extensive Python web development experience but is new to .NET and C#. The goal is to learn enterprise .NET practices by building a real application — a bot that edits Strava activities via their API.

## Developer Background

- **Strong**: Python web development (Flask/FastAPI/Django equivalent patterns), REST APIs, PostgreSQL, React
- **Learning**: C#, ASP.NET Core, Entity Framework Core, .NET tooling, enterprise patterns
- **Target**: Senior consultant-level .NET competency — production-quality code, enterprise architecture patterns, CI/CD, and testing discipline

When explaining concepts, Python analogies are welcome and useful. For example:
- `IActivityService` / DI interfaces → similar to Python `Protocol` or abstract base classes
- EF Core `DbContext` → similar to SQLAlchemy `Session`
- `record` DTOs → similar to Python `dataclasses` or Pydantic models
- ASP.NET `IActionResult` → similar to Flask/FastAPI `Response` objects
- `appsettings.json` → similar to `.env` / config files loaded via `python-decouple`

---

## Current Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core Web API (.NET 10) |
| Database | PostgreSQL via Npgsql |
| ORM | Entity Framework Core 10 |
| Validation | FluentValidation 12 (auto-validation via `AddFluentValidationAutoValidation`) |
| API Docs | Swagger / OpenAPI (Swashbuckle + built-in `MapOpenApi`) |
| Error handling | `IExceptionHandler` + `ProblemDetails` middleware |

### Planned additions
- React frontend (to be added later)
- Strava OAuth integration

---

## Project Structure

```
StravaEditBotApi/
  Controllers/       # ASP.NET controllers — thin, delegate to services
  Services/          # Business logic — interface + implementation pattern
  DTOs/              # Request/response shapes (C# records)
  Models/            # EF Core entity classes
  Validators/        # FluentValidation AbstractValidator<T> classes
  Middleware/        # Global exception handler
  Data/              # AppDbContext
  Migrations/        # EF Core migration files — do not edit by hand

StravaEditBotApi.Tests/
  Unit/
    Controllers/     # Controller unit tests (mock the service layer)
    Services/        # Service unit tests (use EF InMemory database)
    Validators/      # Validator unit tests (no mocks needed)
```

---

## Architecture Principles

- **Thin controllers**: Controllers only handle HTTP concerns (routing, status codes, request/response mapping). Business logic lives in services.
- **Interface-driven services**: Every service has an `IXxxService` interface. This is what makes mocking in tests possible and enables DI substitution.
- **Constructor injection**: Use primary constructor syntax for DI (the current codebase pattern). Example: `public class ActivitiesController(IActivityService activityService) : ControllerBase`.
- **DTOs separate from entities**: Never return EF entity objects directly from controllers. Use DTOs (currently `Activity` is returned directly — this is a known area to improve).
- **Async all the way down**: All I/O operations must be `async Task<T>`. Never block with `.Result` or `.Wait()`.

---

## Testing

### Target testing stack (use for all new tests)

| Role | Library |
|---|---|
| Test framework | **NUnit** (`[TestFixture]`, `[Test]`, `[SetUp]`) |
| Mocking | **NSubstitute** (`Substitute.For<IFoo>()`, `.Returns()`, `.Received()`) |
| Assertions | **NUnit built-in** (`Assert.That(x, Is.EqualTo(y))`) |

### Why these choices

- **NUnit over xUnit**: More explicit setup/teardown lifecycle (`[SetUp]`, `[TearDown]`, `[OneTimeSetUp]`), which maps better to enterprise codebases and is common in consultant environments.
- **NSubstitute over Moq**: Moq introduced a [controversial telemetry/SponsorLink dependency in 4.20](https://github.com/moq/moq/issues/1374) that caused significant community backlash. Many enterprise teams have banned it. NSubstitute is the standard replacement — cleaner API, no controversy.
- **NUnit assertions over FluentAssertions**: FluentAssertions [moved to a commercial license in v8](https://github.com/fluentassertions/fluentassertions/discussions/2943) which is incompatible with many open-source and enterprise licensing policies. NUnit's built-in `Assert.That` constraint model is expressive enough for all common assertions.

All tests have been migrated to the target stack. Do not introduce xUnit, Moq, or FluentAssertions.

### Testing patterns

**Controller tests** — mock the service interface with NSubstitute:
```csharp
[TestFixture]
public class ActivitiesControllerTests
{
    // null! tells the compiler "I know this looks null, but [SetUp] always fills it
    // before any test runs." Standard pattern for NUnit test fields.
    private IActivityService _service = null!;
    private ActivitiesController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<IActivityService>();
        _sut = new ActivitiesController(_service, Substitute.For<ILogger<ActivitiesController>>());
    }

    [Test]
    public async Task GetAll_ReturnsOk()
    {
        _service.GetAllAsync().Returns(new List<Activity>());
        var result = await _sut.GetAllAsync();
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }
}
```

**Service tests** — use EF Core InMemory provider, no mocks needed:
```csharp
[TestFixture]
public class ActivityServiceTests
{
    private AppDbContext _context;
    private ActivityService _sut;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique DB per test
            .Options;
        _context = new AppDbContext(options);
        _sut = new ActivityService(_context);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();
}
```

**Validator tests** — instantiate the validator directly, no mocks:
```csharp
[TestFixture]
public class CreateActivityDtoValidatorTests
{
    private CreateActivityDtoValidator _validator;

    [SetUp]
    public void SetUp() => _validator = new CreateActivityDtoValidator();

    [Test]
    public void Validate_ValidDto_Passes()
    {
        var dto = /* ... */;
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.True);
    }
}
```

### Test naming convention

`MethodName_Scenario_ExpectedBehaviour` — e.g. `GetByIdAsync_NonExistentId_ReturnsNull`

### NSubstitute quick reference

```csharp
// Create a substitute
var sub = Substitute.For<IMyService>();

// Set up a return value
sub.GetAsync(1).Returns(Task.FromResult(someObject));
// or for async:
sub.GetAsync(1).Returns(someObject); // NSubstitute handles Task wrapping

// Set up return with null
sub.GetByIdAsync(999).Returns((Activity?)null);

// Verify a call was received
sub.Received(1).CreateAsync(Arg.Any<CreateActivityDto>());
sub.Received().DeleteAsync(42);           // Received() defaults to at least once
sub.DidNotReceive().DeleteAsync(999);
```

---

## C# Conventions

- **Nullable reference types enabled** (`<Nullable>enable</Nullable>`): always annotate nullable returns as `T?` and handle nulls explicitly.
- **File-scoped namespaces**: `namespace StravaEditBotApi.Controllers;` (not `namespace X { }` block style).
- **Records for DTOs**: immutable, positional record syntax — `public record CreateActivityDto(string Name, ...)`.
- **`var` for locals**: use `var` when the type is obvious from the right-hand side.
- **`is null` / `is not null`**: prefer over `== null` for null checks.
- **Primary constructors** for DI (`.NET 8+`): `public class Foo(IBar bar)` with field assignment in body.
- **Async suffix**: keep `Async` suffix on all async methods (`GetAllAsync`, not `GetAll`). The controller has `SuppressAsyncSuffixInActionNames = false` to preserve this.

---

## Key Commands

```bash
# Build
dotnet build

# Run the API (starts on https://localhost:5001)
dotnet run --project StravaEditBotApi

# Run tests
dotnet test

# Add a new EF migration
dotnet ef migrations add <MigrationName> --project StravaEditBotApi

# Apply migrations
dotnet ef database update --project StravaEditBotApi
```

---

## Integration test setup notes

`WebAppFactory` overrides the database by removing EF Core's service registrations and replacing them with InMemory. In EF Core 8+, `AddDbContext` registers **four** service types that must all be removed:
- `IDbContextOptionsConfiguration<AppDbContext>` — the configuration action (the lambda); if left behind, EF applies both Npgsql and InMemory and throws
- `DbContextOptions<AppDbContext>`
- `DbContextOptions` — non-generic alias
- `AppDbContext`

See `StravaEditBotApi.Tests/Integration/WebAppFactory.cs` for the working pattern.

---

## Known Areas for Improvement

These are intentional learning tasks, not bugs to fix immediately:

1. Controllers currently return `Activity` entity objects directly — should use response DTOs
2. No pagination on `GetAllAsync`
3. No authentication/authorisation yet (needed before Strava OAuth integration)
4. No integration tests (tests that spin up the real HTTP stack with `WebApplicationFactory`)
5. ~~Existing tests use xUnit + Moq + FluentAssertions and should be migrated to NUnit + NSubstitute~~ (done)
