using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs;
using StravaEditBotApi.Models;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Tests.Unit.Services;

public class ActivityServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ActivityService _sut;

    public ActivityServiceTests()
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

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsActivityWithGeneratedId()
    {
        var dto = MakeCreateDto();

        var result = await _sut.CreateAsync(dto);

        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Morning Run");
        result.ActivitySport.Should().Be("Run");
        result.Distance.Should().Be(5.0);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_PersistsToDatabase()
    {
        var dto = MakeCreateDto(name: "Persisted Run");

        var created = await _sut.CreateAsync(dto);

        // Query the database directly — don't trust the return value alone.
        // This catches bugs where the service returns an object but
        // forgets to call SaveChangesAsync.
        var fromDb = await _context.Activities.FindAsync(created.Id);
        fromDb.Should().NotBeNull();
        fromDb!.Name.Should().Be("Persisted Run");
    }

    [Fact]
    public async Task CreateAsync_MultipleCalls_GeneratesUniqueIds()
    {
        var first = await _sut.CreateAsync(MakeCreateDto(name: "First"));
        var second = await _sut.CreateAsync(MakeCreateDto(name: "Second"));

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
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
        result.Name.Should().Be("Full Map Test");
        result.Description.Should().Be("Testing all fields");
        result.ActivitySport.Should().Be("Ride");
        result.StartTime.Should().Be(startTime);
        result.Distance.Should().Be(25.5);
        result.ElapsedTime.Should().Be(elapsed);
    }

    // ========================================================
    // GetAllAsync
    // ========================================================

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyCollection()
    {
        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithActivities_ReturnsAll()
    {
        await SeedActivityAsync("Run 1");
        await SeedActivityAsync("Run 2");
        await SeedActivityAsync("Run 3");

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(3);
    }

    // ========================================================
    // GetByIdAsync
    // ========================================================

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsActivity()
    {
        var seeded = await SeedActivityAsync("Find Me");

        var result = await _sut.GetByIdAsync(seeded.Id);

        result.Should().NotBeNull();
        result.Name.Should().Be("Find Me");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(999);

        result.Should().BeNull();
    }

    // ========================================================
    // UpdateAsync
    // ========================================================

    [Fact]
    public async Task UpdateAsync_ExistingId_ReturnsTrueAndUpdatesFields()
    {
        var seeded = await SeedActivityAsync("Original Name");
        var updateDto = MakeUpdateDto(
            name: "Updated Name",
            activitySport: "Ride",
            distance: 42.0);

        bool result = await _sut.UpdateAsync(seeded.Id, updateDto);

        // Verify return value
        result.Should().BeTrue();

        // Verify the database was actually updated
        var fromDb = await _context.Activities.FindAsync(seeded.Id);
        fromDb!.Name.Should().Be("Updated Name");
        fromDb.ActivitySport.Should().Be("Ride");
        fromDb.Distance.Should().Be(42.0);
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_DoesNotChangeId()
    {
        var seeded = await SeedActivityAsync();
        int originalId = seeded.Id;
        var updateDto = MakeUpdateDto(name: "New Name");

        await _sut.UpdateAsync(originalId, updateDto);

        var fromDb = await _context.Activities.FindAsync(originalId);
        fromDb.Should().NotBeNull();
        fromDb!.Id.Should().Be(originalId);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_ReturnsFalse()
    {
        var updateDto = MakeUpdateDto(name: "Won't Update");

        bool result = await _sut.UpdateAsync(999, updateDto);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_DoesNotCreateActivity()
    {
        var updateDto = MakeUpdateDto(name: "Ghost");

        await _sut.UpdateAsync(999, updateDto);

        var all = await _context.Activities.ToListAsync();
        all.Should().BeEmpty();
    }

    // ========================================================
    // DeleteAsync
    // ========================================================

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrueAndRemoves()
    {
        var seeded = await SeedActivityAsync();

        bool result = await _sut.DeleteAsync(seeded.Id);

        result.Should().BeTrue();
        var fromDb = await _context.Activities.FindAsync(seeded.Id);
        fromDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        bool result = await _sut.DeleteAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_OnlyRemovesTargetActivity()
    {
        var keep = await SeedActivityAsync("Keep Me");
        var remove = await SeedActivityAsync("Remove Me");

        await _sut.DeleteAsync(remove.Id);

        var remaining = await _context.Activities.ToListAsync();
        remaining.Should().HaveCount(1);
        remaining[0].Name.Should().Be("Keep Me");
    }

    // ========================================================
    // Cleanup
    // ========================================================

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}