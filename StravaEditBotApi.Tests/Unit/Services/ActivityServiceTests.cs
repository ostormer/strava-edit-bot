using Microsoft.EntityFrameworkCore;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Models;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Tests.Unit.Services;

[TestFixture]
public class ActivityServiceTests
{
    private AppDbContext _context = null!;
    private ActivityService _sut = null!;

    [SetUp]
    public void Setup()
    {
        // Guid.NewGuid() ensures every test gets its own isolated database.
        // Without this, tests would share state and interfere with each other.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _sut = new ActivityService(_context);
    }

    // Helper - same idea as the validator tests: a valid DTO you can override
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

    private static UpdateActivityDto MakeUpdateDto(
        string? name = null,
        string? description = null,
        string? activitySport = null,
        DateTime? startTime = null,
        double? distance = null,
        TimeSpan? elapsedTime = null)
    {
        return new UpdateActivityDto(
            Name: name,
            Description: description,
            ActivitySport: activitySport,
            StartTime: startTime,
            Distance: distance,
            ElapsedTime: elapsedTime
        );
    }

    // Helper - seeds an activity into the database and returns it.
    // Many tests need an existing activity to work with, so this
    // avoids duplicating setup code across tests.
    private async Task<Activity> SeedActivityAsync(string name = "Seeded Run")
    {
        var dto = MakeCreateDto(name: name);
        return await _sut.CreateAsync(dto);
    }

    // ========================================================
    // CreateAsync
    // ========================================================

    [Test]
    public async Task CreateAsync_ValidDto_ReturnsActivityWithGeneratedId()
    {
        var dto = MakeCreateDto();

        var result = await _sut.CreateAsync(dto);

        Assert.That(result.Id, Is.GreaterThan(0));
        Assert.That(result.Name, Is.EqualTo("Morning Run"));
        Assert.That(result.ActivitySport, Is.EqualTo("Run"));
        Assert.That(result.Distance, Is.EqualTo(5.0));
    }

    [Test]
    public async Task CreateAsync_ValidDto_PersistsToDatabase()
    {
        var dto = MakeCreateDto(name: "Persisted Run");

        var created = await _sut.CreateAsync(dto);

        // Query the database directly — don't trust the return value alone.
        // This catches bugs where the service returns an object but
        // forgets to call SaveChangesAsync.
        var fromDb = await _context.Activities.FindAsync(created.Id);
        Assert.That(fromDb, Is.Not.Null);
        Assert.That(fromDb!.Name, Is.EqualTo("Persisted Run"));
    }

    [Test]
    public async Task CreateAsync_MultipleCalls_GeneratesUniqueIds()
    {
        var first = await _sut.CreateAsync(MakeCreateDto(name: "First"));
        var second = await _sut.CreateAsync(MakeCreateDto(name: "Second"));

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }

    [Test]
    public async Task CreateAsync_MapsAllFieldsCorrectly()
    {
        var startTime = DateTime.UtcNow.AddHours(-2);
        var elapsed = TimeSpan.FromMinutes(45);

        var dto = MakeCreateDto(
            name: "Full Map Test",
            description: "Testing all fields",
            activitySport: "Ride",
            startTime: startTime,
            distance: 25.5,
            elapsedTime: elapsed);

        var result = await _sut.CreateAsync(dto);

        // Verify every field was mapped from DTO to entity
        Assert.That(result.Name, Is.EqualTo("Full Map Test"));
        Assert.That(result.Description, Is.EqualTo("Testing all fields"));
        Assert.That(result.ActivitySport, Is.EqualTo("Ride"));
        Assert.That(result.StartTime, Is.EqualTo(startTime));
        Assert.That(result.Distance, Is.EqualTo(25.5));
        Assert.That(result.ElapsedTime, Is.EqualTo(elapsed));
    }

    // ========================================================
    // GetAllAsync
    // ========================================================

    [Test]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyCollection()
    {
        var result = await _sut.GetAllAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetAllAsync_WithActivities_ReturnsAll()
    {
        await SeedActivityAsync("Run 1");
        await SeedActivityAsync("Run 2");
        await SeedActivityAsync("Run 3");

        var result = await _sut.GetAllAsync();

        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result.Any(a => a.Name == "Run 1"), Is.True);
        Assert.That(result.Any(a => a.Name == "Run 2"), Is.True);
        Assert.That(result.Any(a => a.Name == "Run 3"), Is.True);
    }

    // ========================================================
    // GetByIdAsync
    // ========================================================

    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsActivity()
    {
        var seeded = await SeedActivityAsync("Find Me");

        var result = await _sut.GetByIdAsync(seeded.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Find Me"));
    }

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(999);

        Assert.That(result, Is.Null);
    }

    // ========================================================
    // UpdateAsync
    // ========================================================

    [Test]
    public async Task UpdateAsync_ExistingId_ReturnsTrueAndUpdatesFields()
    {
        var seeded = await SeedActivityAsync("Original Name");
        var updateDto = MakeUpdateDto(
            name: "Updated Name",
            activitySport: "Ride",
            distance: 42.0);

        bool result = await _sut.UpdateAsync(seeded.Id, updateDto);

        // Verify return value
        Assert.That(result, Is.True);

        // Verify the database was actually updated
        var fromDb = await _context.Activities.FindAsync(seeded.Id);
        Assert.That(fromDb, Is.Not.Null);
        Assert.That(fromDb!.Name, Is.EqualTo("Updated Name"));
        Assert.That(fromDb.ActivitySport, Is.EqualTo("Ride"));
        Assert.That(fromDb.Distance, Is.EqualTo(42.0));
    }

    [Test]
    public async Task UpdateAsync_ExistingId_DoesNotChangeId()
    {
        var seeded = await SeedActivityAsync();
        int originalId = seeded.Id;
        var updateDto = MakeUpdateDto(name: "New Name");

        await _sut.UpdateAsync(originalId, updateDto);

        var fromDb = await _context.Activities.FindAsync(originalId);
        Assert.That(fromDb, Is.Not.Null);
        Assert.That(fromDb!.Id, Is.EqualTo(originalId));
    }

    [Test]
    public async Task UpdateAsync_NonExistentId_ReturnsFalse()
    {
        var updateDto = MakeUpdateDto(name: "Won't Update");

        bool result = await _sut.UpdateAsync(999, updateDto);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task UpdateAsync_NonExistentId_DoesNotCreateActivity()
    {
        var updateDto = MakeUpdateDto(name: "Ghost");

        await _sut.UpdateAsync(999, updateDto);

        var all = await _context.Activities.ToListAsync();
        Assert.That(all, Is.Empty);
    }

    // ========================================================
    // DeleteAsync
    // ========================================================

    [Test]
    public async Task DeleteAsync_ExistingId_ReturnsTrueAndRemoves()
    {
        var seeded = await SeedActivityAsync();

        bool result = await _sut.DeleteAsync(seeded.Id);

        Assert.That(result, Is.True);
        var fromDb = await _context.Activities.FindAsync(seeded.Id);
        Assert.That(fromDb, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        bool result = await _sut.DeleteAsync(999);

        Assert.That(result, Is.False);
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

    // ========================================================
    // Cleanup
    // ========================================================

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }
}