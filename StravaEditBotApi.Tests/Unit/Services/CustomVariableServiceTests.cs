using Microsoft.EntityFrameworkCore;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs.Variables;
using StravaEditBotApi.Models;
using StravaEditBotApi.Models.Rules;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Tests.Unit.Services;

[TestFixture]
public class CustomVariableServiceTests
{
    private AppDbContext _db = null!;
    private CustomVariableService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new CustomVariableService(_db);

        _db.Users.Add(new AppUser { Id = "user1", UserName = "user1" });
        _db.SaveChanges();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ========================================================
    // GetUserVariablesAsync
    // ========================================================

    [Test]
    public async Task GetUserVariablesAsync_NoVariables_ReturnsEmptyList()
    {
        var result = await _sut.GetUserVariablesAsync("user1");

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetUserVariablesAsync_MultipleUsers_ReturnsOnlyRequestedUsersVariables()
    {
        _db.Users.Add(new AppUser { Id = "user2", UserName = "user2" });
        await _db.SaveChangesAsync();

        await _sut.CreateAsync("user1", MakeCreateDto("alpha"));
        await _sut.CreateAsync("user2", MakeCreateDto("beta"));

        var result = await _sut.GetUserVariablesAsync("user1");

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("alpha"));
    }

    [Test]
    public async Task GetUserVariablesAsync_MultipleVariables_ReturnsOrderedByName()
    {
        await _sut.CreateAsync("user1", MakeCreateDto("zebra"));
        await _sut.CreateAsync("user1", MakeCreateDto("alpha"));
        await _sut.CreateAsync("user1", MakeCreateDto("mango"));

        var result = await _sut.GetUserVariablesAsync("user1");

        Assert.That(result.Select(v => v.Name), Is.EqualTo(new[] { "alpha", "mango", "zebra" }));
    }

    // ========================================================
    // GetByIdAsync
    // ========================================================

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync("user1", 999);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByIdAsync_IdBelongsToDifferentUser_ReturnsNull()
    {
        _db.Users.Add(new AppUser { Id = "user2", UserName = "user2" });
        await _db.SaveChangesAsync();

        var (created, _) = await _sut.CreateAsync("user2", MakeCreateDto("pace_label"));

        var result = await _sut.GetByIdAsync("user1", created!.Id);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByIdAsync_ValidIdAndUser_ReturnsCorrectVariable()
    {
        var (created, _) = await _sut.CreateAsync("user1", MakeCreateDto("pace_label"));

        var result = await _sut.GetByIdAsync("user1", created!.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("pace_label"));
    }

    // ========================================================
    // CreateAsync
    // ========================================================

    [TestCase("distance_km")]
    [TestCase("sport_type")]
    [TestCase("start_time")]
    public async Task CreateAsync_BuiltInVariableName_ReturnsError(string builtInName)
    {
        var (result, error) = await _sut.CreateAsync("user1", MakeCreateDto(builtInName));

        Assert.That(result, Is.Null);
        Assert.That(error, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task CreateAsync_UserHas50Variables_ReturnsError()
    {
        for (int i = 0; i < 50; i++)
        {
            await _sut.CreateAsync("user1", MakeCreateDto($"var_{i}"));
        }

        var (result, error) = await _sut.CreateAsync("user1", MakeCreateDto("one_too_many"));

        Assert.That(result, Is.Null);
        Assert.That(error, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task CreateAsync_NameAlreadyExistsForUser_ReturnsError()
    {
        await _sut.CreateAsync("user1", MakeCreateDto("pace_label"));

        var (result, error) = await _sut.CreateAsync("user1", MakeCreateDto("pace_label"));

        Assert.That(result, Is.Null);
        Assert.That(error, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task CreateAsync_ValidInput_CreatesVariableSuccessfully()
    {
        var (result, error) = await _sut.CreateAsync("user1", MakeCreateDto("pace_label"));

        Assert.That(error, Is.Null);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("pace_label"));

        var fromDb = await _db.CustomVariables.SingleOrDefaultAsync(cv => cv.Id == result.Id);
        Assert.That(fromDb, Is.Not.Null);
    }

    [Test]
    public async Task CreateAsync_ValidInput_EmbedsDtoNameIntoDefinition()
    {
        var dto = new CreateCustomVariableDto(
            "pace_label",
            "My pace label",
            new CustomVariableDefinition { Name = "wrong_name", Cases = [], DefaultValue = "Slow" }
        );

        var (result, _) = await _sut.CreateAsync("user1", dto);

        Assert.That(result!.Definition.Name, Is.EqualTo("pace_label"));
    }

    [Test]
    public async Task CreateAsync_ValidInput_SetsCreatedAtAndUpdatedAt()
    {
        DateTime before = DateTime.UtcNow.AddSeconds(-1);

        var (result, _) = await _sut.CreateAsync("user1", MakeCreateDto("pace_label"));

        DateTime after = DateTime.UtcNow.AddSeconds(1);

        Assert.That(result!.CreatedAt, Is.GreaterThan(before).And.LessThan(after));
        Assert.That(result.UpdatedAt, Is.GreaterThan(before).And.LessThan(after));
    }

    // ========================================================
    // UpdateAsync
    // ========================================================

    [Test]
    public async Task UpdateAsync_NonExistentVariable_ReturnsNullResult()
    {
        var (result, error) = await _sut.UpdateAsync("user1", 999, new UpdateCustomVariableDto(null, null));

        Assert.That(result, Is.Null);
        Assert.That(error, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_VariableOwnedByDifferentUser_ReturnsNullResult()
    {
        _db.Users.Add(new AppUser { Id = "user2", UserName = "user2" });
        await _db.SaveChangesAsync();

        var (created, _) = await _sut.CreateAsync("user2", MakeCreateDto("pace_label"));

        var (result, error) = await _sut.UpdateAsync("user1", created!.Id, new UpdateCustomVariableDto("New desc", null));

        Assert.That(result, Is.Null);
        Assert.That(error, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_DescriptionProvided_UpdatesDescription()
    {
        var (created, _) = await _sut.CreateAsync("user1", MakeCreateDto("pace_label", description: "Old desc"));

        var (result, _) = await _sut.UpdateAsync("user1", created!.Id, new UpdateCustomVariableDto("New desc", null));

        Assert.That(result!.Description, Is.EqualTo("New desc"));

        var fromDb = await _db.CustomVariables.FindAsync(created.Id);
        Assert.That(fromDb!.Description, Is.EqualTo("New desc"));
    }

    [Test]
    public async Task UpdateAsync_DefinitionProvided_UpdatesDefinition()
    {
        var (created, _) = await _sut.CreateAsync("user1", MakeCreateDto("pace_label"));
        var newDefinition = new CustomVariableDefinition
        {
            Name = "pace_label",
            Cases = [],
            DefaultValue = "Fast"
        };

        var (result, _) = await _sut.UpdateAsync("user1", created!.Id, new UpdateCustomVariableDto(null, newDefinition));

        Assert.That(result!.Definition.DefaultValue, Is.EqualTo("Fast"));
    }

    [Test]
    public async Task UpdateAsync_DescriptionIsNull_DoesNotChangeDescription()
    {
        var (created, _) = await _sut.CreateAsync("user1", MakeCreateDto("pace_label", description: "Keep this"));

        await _sut.UpdateAsync("user1", created!.Id, new UpdateCustomVariableDto(null, null));

        var fromDb = await _db.CustomVariables.FindAsync(created.Id);
        Assert.That(fromDb!.Description, Is.EqualTo("Keep this"));
    }

    [Test]
    public async Task UpdateAsync_ClearDescriptionTrue_SetsDescriptionToNull()
    {
        var (created, _) = await _sut.CreateAsync("user1", MakeCreateDto("pace_label", description: "Remove me"));

        var (result, _) = await _sut.UpdateAsync("user1", created!.Id, new UpdateCustomVariableDto(null, null, ClearDescription: true));

        Assert.That(result!.Description, Is.Null);

        var fromDb = await _db.CustomVariables.FindAsync(created.Id);
        Assert.That(fromDb!.Description, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_ClearDescriptionTrueWithDescriptionProvided_ClearWins()
    {
        var (created, _) = await _sut.CreateAsync("user1", MakeCreateDto("pace_label", description: "Old"));

        var (result, _) = await _sut.UpdateAsync("user1", created!.Id, new UpdateCustomVariableDto("New desc", null, ClearDescription: true));

        var fromDb = await _db.CustomVariables.FindAsync(created.Id);
        Assert.That(fromDb!.Description, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_DefinitionIsNull_DoesNotChangeDefinition()
    {
        var originalDefinition = new CustomVariableDefinition
        {
            Name = "pace_label",
            Cases = [],
            DefaultValue = "Original"
        };
        var (created, _) = await _sut.CreateAsync("user1", new CreateCustomVariableDto("pace_label", null, originalDefinition));

        await _sut.UpdateAsync("user1", created!.Id, new UpdateCustomVariableDto("New desc", null));

        var fromDb = await _db.CustomVariables.FindAsync(created.Id);
        Assert.That(fromDb!.Definition.DefaultValue, Is.EqualTo("Original"));
    }

    // ========================================================
    // DeleteAsync
    // ========================================================

    [Test]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        bool result = await _sut.DeleteAsync("user1", 999);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DeleteAsync_IdBelongsToDifferentUser_ReturnsFalse()
    {
        _db.Users.Add(new AppUser { Id = "user2", UserName = "user2" });
        await _db.SaveChangesAsync();

        var (created, _) = await _sut.CreateAsync("user2", MakeCreateDto("pace_label"));

        bool result = await _sut.DeleteAsync("user1", created!.Id);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DeleteAsync_ValidIdAndUser_RemovesVariableFromDbAndReturnsTrue()
    {
        var (created, _) = await _sut.CreateAsync("user1", MakeCreateDto("pace_label"));

        bool result = await _sut.DeleteAsync("user1", created!.Id);

        Assert.That(result, Is.True);
        var fromDb = await _db.CustomVariables.FindAsync(created.Id);
        Assert.That(fromDb, Is.Null);
    }

    // ========================================================
    // Helpers
    // ========================================================

    private static CreateCustomVariableDto MakeCreateDto(
        string? name = null,
        string? description = null,
        CustomVariableDefinition? definition = null)
    {
        string resolvedName = name ?? "pace_label";
        return new CreateCustomVariableDto(
            resolvedName,
            description ?? "My pace label",
            definition ?? new CustomVariableDefinition { Name = resolvedName, Cases = [], DefaultValue = "Slow" }
        );
    }
}
