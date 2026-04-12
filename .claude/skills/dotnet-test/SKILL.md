---
name: dotnet-test
description: Write tests for the .NET API (StravaEditBotApi) following the established testing standards (NUnit, NSubstitute, NUnit assertions). Use ONLY for backend C# tests — controllers, services, validators, or integration tests against the ASP.NET Core API. Do NOT trigger for frontend/React/TypeScript/Vitest tests in strava-edit-bot-ui.
disable-model-invocation: false
argument-hint: "<what to test>"
---

Write tests for $ARGUMENTS.

## Stack — non-negotiable

| Role | Library |
|---|---|
| Framework | NUnit — `[TestFixture]`, `[Test]`, `[TestCase]`, `[SetUp]`, `[TearDown]`, `[OneTimeSetUp]`, `[OneTimeTearDown]` |
| Mocking | NSubstitute — `Substitute.For<T>()`, `.Returns()`, `.Received()`, `Arg.Any<T>()` |
| Assertions | NUnit only — `Assert.That(x, Is.EqualTo(y))` |
| Validator helpers | FluentValidation.TestHelper — `TestValidate`, `ShouldHaveValidationErrorFor`, `ShouldNotHaveValidationErrorFor`, `ShouldNotHaveAnyValidationErrors` |

**Never use**: xUnit, Moq, FluentAssertions.

---

## Naming and placement

**Method name**: `MethodName_Scenario_ExpectedBehaviour` — e.g. `CreateAsync_ValidDto_ReturnsActivityWithId`

| Subject | Location |
|---|---|
| Validators | `StravaEditBotApi.Tests/Unit/Validators/` |
| Services | `StravaEditBotApi.Tests/Unit/Services/` |
| Controllers | `StravaEditBotApi.Tests/Unit/Controllers/` |
| Full HTTP stack | `StravaEditBotApi.Tests/Integration/` |

Organize tests within a fixture by method using section comments:
```csharp
// ========================================================
// CreateAsync
// ========================================================
```

---

## Factory helpers (`Make*` and `Seed*`)

Every fixture should have static `Make*` helpers that return a valid object with all fields set to sensible defaults. Parameters are nullable with `null` as default so callers only override what's relevant to their test.

```csharp
private static CreateActivityDto MakeCreateDto(
    string? name = null,
    string? description = null,
    string? activitySport = null,
    DateTime? startTime = null,
    double? distance = null,
    TimeSpan? elapsedTime = null)
{
    return new CreateActivityDto(
        Name: name ?? "Morning Run",
        Description: description ?? "A nice run",
        ActivitySport: activitySport ?? "Run",
        StartTime: startTime ?? DateTime.UtcNow.AddHours(-1),
        Distance: distance ?? 5.0,
        ElapsedTime: elapsedTime ?? TimeSpan.FromMinutes(30)
    );
}
```

For service and controller tests that need a pre-seeded entity, add an async `Seed*` helper:
```csharp
private async Task<Activity> SeedActivityAsync(string name = "Seeded Run")
{
    return await _sut.CreateAsync(MakeCreateDto(name: name));
}
```

---

## Validator tests

Instantiate the validator directly. Always include a happy-path test (`ShouldNotHaveAnyValidationErrors()`). Use `[TestCase]` for multiple values of the same rule. Optionally assert on the error message content.

```csharp
[TestFixture]
public class CreateActivityDtoValidatorTests
{
    private CreateActivityDtoValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateActivityDtoValidator();

    [Test]
    public void ValidDto_PassesValidation()
    {
        _validator.TestValidate(MakeValidDto()).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Name_Empty_ShouldFail()
    {
        var result = _validator.TestValidate(MakeValidDto(name: ""));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [TestCase("Run")]
    [TestCase("Ride")]
    public void ActivitySport_ValidValue_ShouldPass(string sport)
    {
        var result = _validator.TestValidate(MakeValidDto(activitySport: sport));
        result.ShouldNotHaveValidationErrorFor(x => x.ActivitySport);
    }

    // Assert on the message itself when the wording matters:
    [Test]
    public void ActivitySport_Invalid_ShouldReturnMeaningfulMessage()
    {
        var result = _validator.TestValidate(MakeValidDto(activitySport: "Surfing"));
        result.ShouldHaveValidationErrorFor(x => x.ActivitySport)
            .When(e => e.ErrorMessage.Contains("must be one of"), "Expected 'must be one of'");
    }

    private static CreateActivityDto MakeValidDto(
        string? name = null,
        string? description = null,
        string? activitySport = null,
        DateTime? startTime = null,
        double? distance = null,
        TimeSpan? elapsedTime = null)
    {
        return new CreateActivityDto(
            Name: name ?? "Morning Run",
            Description: description ?? "A nice run",
            ActivitySport: activitySport ?? "Run",
            StartTime: startTime ?? DateTime.UtcNow.AddHours(-1),
            Distance: distance ?? 5.0,
            ElapsedTime: elapsedTime ?? TimeSpan.FromMinutes(30)
        );
    }
}
```

---

## Service tests

Use EF InMemory with `Guid.NewGuid().ToString()` as the DB name — each test gets its own isolated database. Verify both the **return value** and the **database state** (query `_context` directly to confirm writes; don't trust the return value alone). Dispose context in `[TearDown]`.

```csharp
[TestFixture]
public class ActivityServiceTests
{
    private AppDbContext _context = null!;
    private ActivityService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _sut = new ActivityService(_context);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    [Test]
    public async Task CreateAsync_ValidDto_PersistsToDatabase()
    {
        var created = await _sut.CreateAsync(MakeCreateDto(name: "Persisted Run"));

        var fromDb = await _context.Activities.FindAsync(created.Id);
        Assert.That(fromDb, Is.Not.Null);
        Assert.That(fromDb!.Name, Is.EqualTo("Persisted Run"));
    }

    [Test]
    public async Task DeleteAsync_OnlyRemovesTargetActivity()
    {
        var keep = await SeedActivityAsync("Keep Me");
        var remove = await SeedActivityAsync("Remove Me");

        await _sut.DeleteAsync(remove.Id);

        var remaining = await _context.Activities.ToListAsync();
        Assert.That(remaining, Has.Count.EqualTo(1));
        Assert.That(remaining[0].Name, Is.EqualTo("Keep Me"));
    }

    private async Task<Activity> SeedActivityAsync(string name = "Seeded Run")
        => await _sut.CreateAsync(MakeCreateDto(name: name));

    private static CreateActivityDto MakeCreateDto(string? name = null, /* ... */ )
        => new(Name: name ?? "Morning Run", /* ... */ );
}
```

---

## Controller tests

Mock all service dependencies with NSubstitute. Test HTTP concerns: status codes, response shape, correct service method called. Include entity factory helpers (`MakeActivity`) in addition to DTO helpers.

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

    [Test]
    public async Task Create_ValidDto_ReturnsCreatedAtAction()
    {
        var dto = MakeCreateDto("New Run");
        var created = MakeActivity(7, "New Run");
        _service.CreateAsync(dto).Returns(created);

        var result = await _sut.CreateAsync(dto);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
        var r = (CreatedAtActionResult)result;
        Assert.That(r.StatusCode, Is.EqualTo(201));
        Assert.That(r.ActionName, Is.EqualTo(nameof(ActivitiesController.GetByIdAsync)));
        Assert.That(r.RouteValues!["id"], Is.EqualTo(7));
    }

    [Test]
    public async Task Create_ValidDto_CallsServiceExactlyOnce()
    {
        var dto = MakeCreateDto();
        _service.CreateAsync(dto).Returns(MakeActivity());

        await _sut.CreateAsync(dto);

        await _service.Received(1).CreateAsync(dto);
    }

    private static Activity MakeActivity(int id = 1, string name = "Test Run") =>
        new(name, "desc", "Run", DateTime.UtcNow.AddHours(-1), 5.0, TimeSpan.FromMinutes(30)) { Id = id };

    private static CreateActivityDto MakeCreateDto(string name = "Test Run") =>
        new(Name: name, Description: "desc", ActivitySport: "Run",
            StartTime: DateTime.UtcNow.AddHours(-1), Distance: 5.0,
            ElapsedTime: TimeSpan.FromMinutes(30));
}
```

### Mocking classes with constructor args (e.g. `UserManager<AppUser>`)

`UserManager<T>` is a class, not an interface — NSubstitute can substitute it but you must provide the required constructor arguments (pass `null` for optional ones):

```csharp
var store = Substitute.For<IUserStore<AppUser>>();
_userManager = Substitute.For<UserManager<AppUser>>(
    store, null, null, null, null, null, null, null, null);
```

### Controllers that mix mocks and a real DB

Some controllers (e.g. `AuthController`) take both service mocks and `AppDbContext`. Combine EF InMemory with NSubstitute:

```csharp
[SetUp]
public void SetUp()
{
    _tokenService = Substitute.For<ITokenService>();
    var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
    _db = new AppDbContext(dbOptions);
    _sut = new AuthController(_userManager, _tokenService, _db, _env);
    _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
}
```

Use private setup helpers to reduce noise for multi-step mock configuration:
```csharp
private void SetupTokenService(
    string accessToken = "access-token",
    string rawRefresh = "raw-refresh",
    string refreshHash = "refresh-hash")
{
    _tokenService.GenerateAccessToken(Arg.Any<AppUser>()).Returns(accessToken);
    _tokenService.GenerateRefreshToken().Returns(rawRefresh);
    _tokenService.HashToken(rawRefresh).Returns(refreshHash);
}
```

### Mocking `IConfiguration` indexer

```csharp
_config = Substitute.For<IConfiguration>();
_config["Jwt:Secret"].Returns("test-secret-at-least-32-chars-long!!");
_config["Jwt:Issuer"].Returns("TestIssuer");
```

---

## Integration tests

### WebAppFactory — EF Core 8+ gotcha

EF Core 8+ registers **four** service descriptors for `AddDbContext`. If you only remove some of them the real DB provider leaks through and causes an "multiple providers" error. Remove all four before registering InMemory:

```csharp
Remove<IDbContextOptionsConfiguration<AppDbContext>>(services);
Remove<DbContextOptions<AppDbContext>>(services);
Remove<DbContextOptions>(services);
Remove<AppDbContext>(services);

services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("IntegrationTestDb"));
```

Also:
- `builder.UseEnvironment("Development")` — required so `IsDevelopment()` is true in `Program.cs`
- Inject `Jwt:Secret` via `ConfigureAppConfiguration` (it's not in `appsettings.json`):

```csharp
builder.ConfigureAppConfiguration((_, config) =>
    config.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Jwt:Secret"] = "integration-test-secret-key-must-be-at-least-32-chars-long!",
    }));
```

### Test fixture lifecycle

Boot the factory once and clear rows between tests — don't recreate the factory for each test:

```csharp
[OneTimeSetUp]
public void OneTimeSetUp()
{
    _factory = new WebAppFactory();
    _client = _factory.CreateClient();
}

[SetUp]
public void SetUp()
{
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Activities.RemoveRange(db.Activities);
    db.SaveChanges();
}

[OneTimeTearDown]
public void OneTimeTearDown()
{
    _client.Dispose();
    _factory.Dispose();
}
```

For auth tests, clear both `RefreshTokens` **and** `Users` in `[SetUp]` (order matters — tokens reference users).

### Verifying DB state from integration tests

Query through a scoped service, not the shared `_client`:

```csharp
using var scope = _factory.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var user = db.Users.SingleOrDefault(u => u.Email == dto.Email);
Assert.That(user, Is.Not.Null);
```

### Inspecting `Set-Cookie` headers

The default `HttpClient` silently handles cookies. To see `Set-Cookie` headers directly:

```csharp
using var rawClient = _factory.CreateClient(
    new WebApplicationFactoryClientOptions { HandleCookies = false });

var response = await rawClient.PostAsJsonAsync("/api/auth/register", dto);
var cookie = response.Headers.GetValues("Set-Cookie")
    .FirstOrDefault(v => v.Contains("refreshToken"));

Assert.That(cookie, Is.Not.Null);
Assert.That(cookie, Does.Contain("httponly").IgnoreCase);
```

---

## NSubstitute quick reference

```csharp
sub.GetAsync(1).Returns(someObject);
sub.GetByIdAsync(99).Returns((Activity?)null);
sub.DoAsync(Arg.Any<int>()).Returns(true);
await sub.Received(1).CreateAsync(dto);
sub.DidNotReceive().DeleteAsync(99);
```

---

## Common assertion patterns

```csharp
Assert.That(result.Id, Is.GreaterThan(0));
Assert.That(list, Has.Count.EqualTo(3));
Assert.That(token, Is.Not.Null.And.Not.Empty);
Assert.That(token.Split('.'), Has.Length.EqualTo(3));       // JWT structure
Assert.That(hash, Does.Match("^[0-9A-F]+$"));               // regex
Assert.That(setCookie, Does.Contain("httponly").IgnoreCase);
Assert.That(jwt.ValidTo, Is.GreaterThan(before).And.LessThan(after));
Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
Assert.That(collection, Is.Empty);
```

---

After writing all tests, run `dotnet test` and confirm everything passes before finishing.
