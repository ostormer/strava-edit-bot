# StravaEditBotApi.Tests — Agent Guide

Test suite for the API. See `StravaEditBotApi/AGENTS.md` for the code under test.

---

## Stack

**Use**: NUnit + NSubstitute + NUnit assertions (`Assert.That`)

**Do not use**: xUnit, Moq, or FluentAssertions

---

## Naming convention

`MethodName_Scenario_ExpectedBehaviour`

---

## Structure

```
Unit/
  Controllers/    # NSubstitute mocks of services; no DB
  Services/       # EF InMemory database; no mocks
  Validators/     # FluentValidation rules in isolation
Integration/
  WebAppFactory.cs                  # boots the full app with InMemory DB
  TestAuthHandler.cs                # overrides auth scheme to auto-authenticate
  ActivitiesIntegrationTests.cs
  AuthIntegrationTests.cs
```

---

## Unit test patterns

### Controller tests — mock the service

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

### Service tests — EF InMemory, no mocks

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

### NSubstitute quick reference

```csharp
sub.GetAsync(1).Returns(someObject);
sub.GetByIdAsync(999).Returns((Activity?)null);
sub.Received(1).CreateAsync(Arg.Any<CreateActivityDto>());
sub.DidNotReceive().DeleteAsync(999);
```

---

## Integration tests

**WebAppFactory setup**: EF Core 8+ registers four service types — remove ALL before swapping in InMemory, otherwise the swap silently fails:
- `IDbContextOptionsConfiguration<AppDbContext>`
- `DbContextOptions<AppDbContext>`
- `DbContextOptions`
- `AppDbContext`

**Auth**: environment is pinned to `Development` so `DevBypassAuthenticationHandler` is registered; `TestAuthHandler` then overrides the default scheme to auto-authenticate test requests.

**DB isolation**: `[OneTimeSetUp]` boots the factory once per fixture. `[SetUp]` clears rows between tests (do not recreate the factory each test).

**Auth tests**: clear both `db.Users` and `db.RefreshTokens` in `[SetUp]`.

**Cookie inspection**: to assert on `Set-Cookie` headers, disable the HTTP client's cookie jar:
```csharp
CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false })
```

**Jwt:Secret**: not in `appsettings.json` (it's env-var / user-secrets only). `WebAppFactory` injects it via `ConfigureAppConfiguration` with `AddInMemoryCollection`.

---

## Key commands

```bash
dotnet test
dotnet test --filter "FullyQualifiedName~Integration"
dotnet test --filter "FullyQualifiedName~Unit"
```
