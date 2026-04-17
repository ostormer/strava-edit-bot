using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StravaEditBotApi.Controllers;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs.Runs;
using StravaEditBotApi.Models;

namespace StravaEditBotApi.Tests.Unit.Controllers;

[TestFixture]
public class RulesetRunsControllerTests
{
    private AppDbContext _db = null!;
    private RulesetRunsController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _db.Users.Add(new AppUser { Id = "user-1", UserName = "user-1" });
        _db.Users.Add(new AppUser { Id = "user-2", UserName = "user-2" });
        _db.SaveChanges();

        _sut = new RulesetRunsController(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private void SetUserClaims(string? userId)
    {
        var claims = userId is not null
            ? new[] { new Claim(ClaimTypes.NameIdentifier, userId) }
            : Array.Empty<Claim>();

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims))
            }
        };
    }

    private RulesetRun SeedRun(
        string userId = "user-1",
        long stravaActivityId = 111,
        string status = RulesetRunStatus.Applied,
        DateTime? processedAt = null)
    {
        var run = new RulesetRun
        {
            UserId = userId,
            StravaActivityId = stravaActivityId,
            Status = status,
            ProcessedAt = processedAt ?? DateTime.UtcNow,
            StravaEventTime = DateTime.UtcNow
        };
        _db.RulesetRuns.Add(run);
        _db.SaveChanges();
        return run;
    }

    // ========================================================
    // GetRunsAsync
    // ========================================================

    [Test]
    public async Task GetRunsAsync_MissingClaim_ReturnsUnauthorized()
    {
        SetUserClaims(null);

        var result = await _sut.GetRunsAsync(ct: CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task GetRunsAsync_ValidUser_ReturnsOk()
    {
        SetUserClaims("user-1");
        SeedRun("user-1");

        var result = await _sut.GetRunsAsync(ct: CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetRunsAsync_OnlyReturnsCurrentUserRuns()
    {
        SetUserClaims("user-1");
        SeedRun("user-1", stravaActivityId: 100);
        SeedRun("user-1", stravaActivityId: 200);
        SeedRun("user-2", stravaActivityId: 300);

        var result = await _sut.GetRunsAsync(ct: CancellationToken.None);

        var ok = (OkObjectResult)result;
        var runs = ((IEnumerable<RulesetRunResponseDto>)ok.Value!).ToList();
        Assert.That(runs, Has.Count.EqualTo(2));
        Assert.That(runs.All(r => r.StravaActivityId != 300), Is.True);
    }

    [Test]
    public async Task GetRunsAsync_EmptyList_ReturnsOkWithEmptyCollection()
    {
        SetUserClaims("user-1");

        var result = await _sut.GetRunsAsync(ct: CancellationToken.None);

        var ok = (OkObjectResult)result;
        var runs = ((IEnumerable<RulesetRunResponseDto>)ok.Value!).ToList();
        Assert.That(runs, Is.Empty);
    }

    [Test]
    public async Task GetRunsAsync_Pagination_RespectsPageAndPageSize()
    {
        SetUserClaims("user-1");
        for (int i = 0; i < 5; i++)
        {
            SeedRun("user-1", stravaActivityId: i + 1);
        }

        var result = await _sut.GetRunsAsync(page: 2, pageSize: 2, ct: CancellationToken.None);

        var ok = (OkObjectResult)result;
        var runs = ((IEnumerable<RulesetRunResponseDto>)ok.Value!).ToList();
        Assert.That(runs, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetRunsAsync_PageSizeExceedsMax_ClampsTo200()
    {
        SetUserClaims("user-1");
        for (int i = 0; i < 5; i++)
        {
            SeedRun("user-1", stravaActivityId: i + 1);
        }

        // pageSize of 999 should be clamped to 200, returning all 5 runs
        var result = await _sut.GetRunsAsync(pageSize: 999, ct: CancellationToken.None);

        var ok = (OkObjectResult)result;
        var runs = ((IEnumerable<RulesetRunResponseDto>)ok.Value!).ToList();
        Assert.That(runs, Has.Count.EqualTo(5));
    }

    [Test]
    public async Task GetRunsAsync_OrderedByProcessedAtDescending()
    {
        SetUserClaims("user-1");
        var older = SeedRun("user-1", stravaActivityId: 1, processedAt: DateTime.UtcNow.AddMinutes(-10));
        var newer = SeedRun("user-1", stravaActivityId: 2, processedAt: DateTime.UtcNow);

        var result = await _sut.GetRunsAsync(ct: CancellationToken.None);

        var ok = (OkObjectResult)result;
        var runs = ((IEnumerable<RulesetRunResponseDto>)ok.Value!).ToList();
        Assert.That(runs[0].Id, Is.EqualTo(newer.Id));
        Assert.That(runs[1].Id, Is.EqualTo(older.Id));
    }

    [Test]
    public async Task GetRunsAsync_MapsAllFieldsCorrectly()
    {
        SetUserClaims("user-1");
        var now = DateTime.UtcNow;
        var run = new RulesetRun
        {
            UserId = "user-1",
            StravaActivityId = 42,
            RulesetId = 7,
            RulesetName = "My Ruleset",
            Status = RulesetRunStatus.Applied,
            ErrorMessage = null,
            FieldsChanged = new Dictionary<string, string> { { "name", "New Name" } },
            ProcessedAt = now,
            StravaEventTime = now.AddMinutes(-1)
        };
        _db.RulesetRuns.Add(run);
        _db.SaveChanges();

        var result = await _sut.GetRunsAsync(ct: CancellationToken.None);

        var ok = (OkObjectResult)result;
        var dto = ((IEnumerable<RulesetRunResponseDto>)ok.Value!).Single();
        Assert.That(dto.StravaActivityId, Is.EqualTo(42));
        Assert.That(dto.RulesetId, Is.EqualTo(7));
        Assert.That(dto.RulesetName, Is.EqualTo("My Ruleset"));
        Assert.That(dto.Status, Is.EqualTo(RulesetRunStatus.Applied));
    }

    // ========================================================
    // GetRunAsync
    // ========================================================

    [Test]
    public async Task GetRunAsync_MissingClaim_ReturnsUnauthorized()
    {
        SetUserClaims(null);

        var result = await _sut.GetRunAsync(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task GetRunAsync_NotFound_ReturnsNotFound()
    {
        SetUserClaims("user-1");

        var result = await _sut.GetRunAsync(9999, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetRunAsync_RunBelongsToOtherUser_ReturnsNotFound()
    {
        SetUserClaims("user-1");
        var otherRun = SeedRun("user-2", stravaActivityId: 55);

        var result = await _sut.GetRunAsync(otherRun.Id, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetRunAsync_Found_ReturnsOk()
    {
        SetUserClaims("user-1");
        var run = SeedRun("user-1");

        var result = await _sut.GetRunAsync(run.Id, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetRunAsync_Found_ReturnsCorrectDto()
    {
        SetUserClaims("user-1");
        var run = SeedRun("user-1", stravaActivityId: 77);

        var result = await _sut.GetRunAsync(run.Id, CancellationToken.None);

        var ok = (OkObjectResult)result;
        var dto = (RulesetRunResponseDto)ok.Value!;
        Assert.That(dto.Id, Is.EqualTo(run.Id));
        Assert.That(dto.StravaActivityId, Is.EqualTo(77));
    }
}
