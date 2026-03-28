---
name: test
description: Write tests for this project following the established testing standards (NUnit, NSubstitute, NUnit assertions). Use when asked to add, write, or create tests.
disable-model-invocation: false
argument-hint: "<what to test>"
---

Write tests for $ARGUMENTS.

## Stack — non-negotiable

| Role | Library |
|---|---|
| Framework | NUnit — `[TestFixture]`, `[Test]`, `[SetUp]`, `[TearDown]`, `[OneTimeSetUp]` |
| Mocking | NSubstitute — `Substitute.For<T>()`, `.Returns()`, `.Received()`, `Arg.Any<T>()` |
| Assertions | NUnit only — `Assert.That(x, Is.EqualTo(y))` |

**Never use**: xUnit, Moq, FluentAssertions.

## Naming

`MethodName_Scenario_ExpectedBehaviour` — e.g. `CreateAsync_ValidDto_ReturnsActivityWithId`

## Test placement

| Subject | Location |
|---|---|
| Validators | `StravaEditBotApi.Tests/Unit/Validators/` |
| Services | `StravaEditBotApi.Tests/Unit/Services/` |
| Controllers | `StravaEditBotApi.Tests/Unit/Controllers/` |
| Full HTTP stack | `StravaEditBotApi.Tests/Integration/` |

---

## Validator tests

Instantiate the validator directly. Use `FluentValidation.TestHelper` methods (`ShouldHaveValidationErrorFor`, `ShouldNotHaveValidationErrorFor`). Write a `MakeValidDto(...)` helper with optional overrides so each test only changes one field.

```csharp
[TestFixture]
public class CreateActivityDtoValidatorTests
{
    private CreateActivityDtoValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateActivityDtoValidator();
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

    private static CreateActivityDto MakeValidDto(
        string name = "Morning Run",
        string activitySport = "Run"
        /* add other fields with sensible defaults */
    )
    {
        return new(name, null, activitySport, DateTime.UtcNow.AddHours(-1), 5.0, TimeSpan.FromMinutes(30));
    }
}
```

## Service tests

Use EF InMemory. Use `Guid.NewGuid().ToString()` as the DB name — this gives every test its own isolated database. Dispose the context in `[TearDown]`. Verify both the return value **and** the database state.

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
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task CreateAsync_ValidDto_PersistsToDatabase()
    {
        var created = await _sut.CreateAsync(MakeCreateDto());

        var fromDb = await _context.Activities.FindAsync(created.Id);
        Assert.That(fromDb, Is.Not.Null);
        Assert.That(fromDb!.Name, Is.EqualTo(created.Name));
    }

    private static CreateActivityDto MakeCreateDto(string name = "Morning Run")
    {
        return new(name, null, "Run", DateTime.UtcNow.AddHours(-1), 5.0, TimeSpan.FromMinutes(30));
    }
}
```

## Controller tests

Mock all dependencies with NSubstitute. Test HTTP concerns only: status codes, response shape, correct service method was called.

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
    public async Task GetAll_EmptyList_ReturnsOkWithEmptyCollection()
    {
        _service.GetAllAsync().Returns(new List<Activity>());

        var result = await _sut.GetAllAsync();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.AssignableTo<IEnumerable<Activity>>());
    }
}
```

## Integration tests

Use the existing `WebAppFactory`. Boot once with `[OneTimeSetUp]`, clear DB rows in `[SetUp]`. Test via `HttpClient` over the full ASP.NET pipeline.

```csharp
[TestFixture]
public class ActivitiesIntegrationTests
{
    private WebAppFactory _factory = null!;
    private HttpClient _client = null!;

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

    [Test]
    public async Task Post_ValidActivity_Returns201WithLocationHeader()
    {
        var response = await _client.PostAsJsonAsync("/api/activities", ValidDto());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(response.Headers.Location, Is.Not.Null);
    }
}
```

## NSubstitute quick reference

```csharp
sub.GetAsync(1).Returns(someObject);
sub.GetByIdAsync(99).Returns((Activity?)null);
sub.DoAsync(Arg.Any<int>()).Returns(true);
await sub.Received(1).CreateAsync(dto);
sub.DidNotReceive().DeleteAsync(99);
```

---

After writing all tests, run `dotnet test` and confirm everything passes before finishing.
