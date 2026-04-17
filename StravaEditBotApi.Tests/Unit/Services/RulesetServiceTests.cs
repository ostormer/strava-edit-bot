using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs.Rulesets;
using StravaEditBotApi.DTOs.Templates;
using StravaEditBotApi.Models;
using StravaEditBotApi.Models.Rules;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Tests.Unit.Services;

[TestFixture]
public class RulesetServiceTests
{
    private AppDbContext _db = null!;
    private IRulesetValidator _validator = null!;
    private IFilterSanitizer _sanitizer = null!;
    private RulesetService _sut = null!;

    // Held open so the in-memory SQLite DB survives across calls within a test.
    private SqliteConnection? _sqliteConnection;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _validator = Substitute.For<IRulesetValidator>();
        _sanitizer = Substitute.For<IFilterSanitizer>();
        _sut = new RulesetService(_db, _validator, _sanitizer);

        // Default: validator says everything is valid
        _validator.Validate(Arg.Any<FilterExpression?>(), Arg.Any<RulesetEffect?>())
            .Returns(new RulesetValidationResult(true, []));

        _db.Users.Add(new AppUser { Id = "user1", UserName = "user1" });
        _db.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
        _sqliteConnection?.Dispose();
        _sqliteConnection = null;
    }

    /// <summary>
    /// Creates a SQLite-backed AppDbContext and RulesetService for tests that need
    /// ExecuteUpdateAsync support (not available in EF InMemory provider).
    /// Uses a subclass that overrides HasColumnType for nvarchar(max) columns so
    /// the schema is compatible with SQLite.
    /// </summary>
    private (AppDbContext Db, RulesetService Sut) CreateSqliteContext()
    {
        _sqliteConnection?.Dispose();
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        var db = new SqliteCompatibleAppDbContext(options);
        db.Database.EnsureCreated();
        db.Users.Add(new AppUser { Id = "user1", UserName = "user1" });
        db.SaveChanges();

        var sut = new RulesetService(db, _validator, _sanitizer);
        return (db, sut);
    }

    /// <summary>
    /// AppDbContext subclass for SQLite tests. After the base OnModelCreating runs,
    /// strips the SQL Server-specific HasColumnType("nvarchar(max)") from all string
    /// properties so EF can generate a SQLite-compatible schema.
    /// </summary>
    private sealed class SqliteCompatibleAppDbContext(DbContextOptions<AppDbContext> options)
        : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // SQLite does not understand nvarchar(max). Remove the explicit column
            // type from every property that was given one, so EF falls back to TEXT.
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.GetColumnType() == "nvarchar(max)")
                    {
                        property.SetColumnType(null);
                    }
                }
            }
        }
    }

    // --------------------------------------------------------
    // Factory helpers
    // --------------------------------------------------------

    private static CreateRulesetDto MakeCreateDto(
        string? name = null,
        string? description = null,
        FilterExpression? filter = null,
        RulesetEffect? effect = null,
        bool isEnabled = true)
    {
        return new CreateRulesetDto(
            Name: name ?? "Morning Ride",
            Description: description,
            Filter: filter,
            Effect: effect,
            IsEnabled: isEnabled
        );
    }

    private static UpdateRulesetDto MakeUpdateDto(
        string? name = null,
        string? description = null,
        FilterExpression? filter = null,
        RulesetEffect? effect = null,
        bool? isEnabled = null,
        bool clearFilter = false,
        bool clearEffect = false)
    {
        return new UpdateRulesetDto(
            Name: name,
            Description: description,
            Filter: filter,
            Effect: effect,
            IsEnabled: isEnabled,
            ClearFilter: clearFilter,
            ClearEffect: clearEffect
        );
    }

    private static CreateTemplateFromRulesetDto MakeShareDto(
        string? name = null,
        string? description = null,
        bool isPublic = false)
    {
        return new CreateTemplateFromRulesetDto(
            Name: name ?? "Shared Template",
            Description: description,
            IsPublic: isPublic
        );
    }

    private static CheckFilter MakeCheckFilter(
        string? property = "sport_type",
        string? op = "equals",
        string? value = "Run")
    {
        return new CheckFilter(property, op, value is null ? null : System.Text.Json.JsonDocument.Parse($"\"{value}\"").RootElement);
    }

    private async Task<RulesetResponseDto> SeedRulesetAsync(
        string userId = "user1",
        string? name = null,
        FilterExpression? filter = null,
        RulesetEffect? effect = null)
    {
        return await _sut.CreateAsync(userId, MakeCreateDto(name: name, filter: filter, effect: effect));
    }

    // ========================================================
    // GetUserRulesetsAsync
    // ========================================================

    [Test]
    public async Task GetUserRulesetsAsync_UserHasNoRulesets_ReturnsEmptyList()
    {
        List<RulesetResponseDto> result = await _sut.GetUserRulesetsAsync("user1");

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetUserRulesetsAsync_MultipleRulesets_ReturnsByPriorityAscending()
    {
        await SeedRulesetAsync(name: "First");
        await SeedRulesetAsync(name: "Second");
        await SeedRulesetAsync(name: "Third");

        List<RulesetResponseDto> result = await _sut.GetUserRulesetsAsync("user1");

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0].Priority, Is.LessThan(result[1].Priority));
        Assert.That(result[1].Priority, Is.LessThan(result[2].Priority));
    }

    [Test]
    public async Task GetUserRulesetsAsync_MultipleUsers_ReturnsOnlySpecifiedUserRulesets()
    {
        _db.Users.Add(new AppUser { Id = "user2", UserName = "user2" });
        await _db.SaveChangesAsync();

        await SeedRulesetAsync(userId: "user1", name: "User1 Ruleset");
        await SeedRulesetAsync(userId: "user2", name: "User2 Ruleset");

        List<RulesetResponseDto> result = await _sut.GetUserRulesetsAsync("user1");

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("User1 Ruleset"));
    }

    // ========================================================
    // GetByIdAsync
    // ========================================================

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        RulesetResponseDto? result = await _sut.GetByIdAsync("user1", 9999);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByIdAsync_IdBelongsToDifferentUser_ReturnsNull()
    {
        _db.Users.Add(new AppUser { Id = "user2", UserName = "user2" });
        await _db.SaveChangesAsync();

        RulesetResponseDto created = await SeedRulesetAsync(userId: "user2");

        RulesetResponseDto? result = await _sut.GetByIdAsync("user1", created.Id);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByIdAsync_InvalidRuleset_ReturnsEmptyValidationErrors()
    {
        // IsValid is stored on create/update — GetByIdAsync returns it from DB without re-validating.
        // ValidationErrors are only returned by CreateAsync/UpdateAsync, not by GET endpoints.
        _validator.Validate(Arg.Any<FilterExpression?>(), Arg.Any<RulesetEffect?>())
            .Returns(new RulesetValidationResult(false, [new("filter", "filter_required", "Filter is required")]));

        RulesetResponseDto created = await SeedRulesetAsync();

        RulesetResponseDto? result = await _sut.GetByIdAsync("user1", created.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsValid, Is.False);
        Assert.That(result.ValidationErrors, Is.Empty);
        // Validator called exactly once — by CreateAsync inside SeedRulesetAsync, not by GetByIdAsync
        _validator.Received(1).Validate(Arg.Any<FilterExpression?>(), Arg.Any<RulesetEffect?>());
    }

    // ========================================================
    // CreateAsync
    // ========================================================

    [Test]
    public async Task CreateAsync_FirstRuleset_AssignsPriorityZero()
    {
        RulesetResponseDto result = await _sut.CreateAsync("user1", MakeCreateDto());

        Assert.That(result.Priority, Is.EqualTo(0));
    }

    [Test]
    public async Task CreateAsync_SubsequentRuleset_AssignsPriorityMaxPlusOne()
    {
        await _sut.CreateAsync("user1", MakeCreateDto(name: "First"));
        await _sut.CreateAsync("user1", MakeCreateDto(name: "Second"));

        RulesetResponseDto third = await _sut.CreateAsync("user1", MakeCreateDto(name: "Third"));

        Assert.That(third.Priority, Is.EqualTo(2));
    }

    [Test]
    public async Task CreateAsync_ValidDto_SetsIsValidFromValidator()
    {
        _validator.Validate(Arg.Any<FilterExpression?>(), Arg.Any<RulesetEffect?>())
            .Returns(new RulesetValidationResult(false, []));

        RulesetResponseDto result = await _sut.CreateAsync("user1", MakeCreateDto());

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task CreateAsync_ValidDto_SetsCreatedAtAndUpdatedAtToUtcNow()
    {
        DateTime before = DateTime.UtcNow;

        RulesetResponseDto result = await _sut.CreateAsync("user1", MakeCreateDto());

        DateTime after = DateTime.UtcNow;
        Assert.That(result.CreatedAt, Is.GreaterThanOrEqualTo(before).And.LessThanOrEqualTo(after));
        Assert.That(result.UpdatedAt, Is.GreaterThanOrEqualTo(before).And.LessThanOrEqualTo(after));
    }

    [Test]
    public async Task CreateAsync_ValidDto_PersistsToDatabase()
    {
        RulesetResponseDto result = await _sut.CreateAsync("user1", MakeCreateDto(name: "Saved Ruleset"));

        Ruleset? fromDb = await _db.Rulesets.FindAsync(result.Id);
        Assert.That(fromDb, Is.Not.Null);
        Assert.That(fromDb!.Name, Is.EqualTo("Saved Ruleset"));
    }

    // ========================================================
    // UpdateAsync
    // ========================================================

    [Test]
    public async Task UpdateAsync_NonExistentRuleset_ReturnsNull()
    {
        RulesetResponseDto? result = await _sut.UpdateAsync("user1", 9999, MakeUpdateDto(name: "New Name"));

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_NameProvided_UpdatesName()
    {
        RulesetResponseDto created = await SeedRulesetAsync(name: "Old Name");

        RulesetResponseDto? result = await _sut.UpdateAsync("user1", created.Id, MakeUpdateDto(name: "New Name"));

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("New Name"));

        Ruleset? fromDb = await _db.Rulesets.FindAsync(created.Id);
        Assert.That(fromDb!.Name, Is.EqualTo("New Name"));
    }

    [Test]
    public async Task UpdateAsync_FilterAndEffectBothNull_DoesNotChangeExistingFilterOrEffect()
    {
        CheckFilter originalFilter = MakeCheckFilter();
        RulesetEffect originalEffect = new RulesetEffect { Name = "Morning Run" };
        RulesetResponseDto created = await SeedRulesetAsync(filter: originalFilter, effect: originalEffect);

        RulesetResponseDto? result = await _sut.UpdateAsync("user1", created.Id, MakeUpdateDto());

        Assert.That(result, Is.Not.Null);

        Ruleset? fromDb = await _db.Rulesets.FindAsync(created.Id);
        Assert.That(fromDb!.Filter, Is.Not.Null);
        Assert.That(fromDb.Effect, Is.Not.Null);
        Assert.That(fromDb.Effect!.Name, Is.EqualTo("Morning Run"));
    }

    [Test]
    public async Task UpdateAsync_ClearFilterTrue_SetsFilterToNull()
    {
        CheckFilter originalFilter = MakeCheckFilter();
        RulesetResponseDto created = await SeedRulesetAsync(filter: originalFilter);

        RulesetResponseDto? result = await _sut.UpdateAsync("user1", created.Id, MakeUpdateDto(clearFilter: true));

        Assert.That(result, Is.Not.Null);

        Ruleset? fromDb = await _db.Rulesets.FindAsync(created.Id);
        Assert.That(fromDb!.Filter, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_ClearEffectTrue_SetsEffectToNull()
    {
        RulesetEffect originalEffect = new RulesetEffect { Name = "Morning Run" };
        RulesetResponseDto created = await SeedRulesetAsync(effect: originalEffect);

        RulesetResponseDto? result = await _sut.UpdateAsync("user1", created.Id, MakeUpdateDto(clearEffect: true));

        Assert.That(result, Is.Not.Null);

        Ruleset? fromDb = await _db.Rulesets.FindAsync(created.Id);
        Assert.That(fromDb!.Effect, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_ClearFilterTrueWithFilterProvided_ClearWins()
    {
        CheckFilter originalFilter = MakeCheckFilter();
        RulesetResponseDto created = await SeedRulesetAsync(filter: originalFilter);

        CheckFilter newFilter = MakeCheckFilter(property: "name", op: "contains", value: "Run");
        RulesetResponseDto? result = await _sut.UpdateAsync("user1", created.Id,
            MakeUpdateDto(filter: newFilter, clearFilter: true));

        Ruleset? fromDb = await _db.Rulesets.FindAsync(created.Id);
        Assert.That(fromDb!.Filter, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_FilterProvidedWithoutClear_UpdatesFilter()
    {
        CheckFilter originalFilter = MakeCheckFilter();
        RulesetResponseDto created = await SeedRulesetAsync(filter: originalFilter);

        CheckFilter newFilter = MakeCheckFilter(property: "name", op: "contains", value: "Run");
        RulesetResponseDto? result = await _sut.UpdateAsync("user1", created.Id,
            MakeUpdateDto(filter: newFilter));

        Ruleset? fromDb = await _db.Rulesets.FindAsync(created.Id);
        Assert.That(fromDb!.Filter, Is.Not.Null);
        var check = (CheckFilter)fromDb.Filter!;
        Assert.That(check.Property, Is.EqualTo("name"));
    }

    [Test]
    public async Task UpdateAsync_AfterUpdate_RecomputesIsValid()
    {
        RulesetResponseDto created = await SeedRulesetAsync();

        _validator.Validate(Arg.Any<FilterExpression?>(), Arg.Any<RulesetEffect?>())
            .Returns(new RulesetValidationResult(false, [new("effect", "effect_required", "Effect is required")]));

        RulesetResponseDto? result = await _sut.UpdateAsync("user1", created.Id, MakeUpdateDto(name: "Updated"));

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsValid, Is.False);

        Ruleset? fromDb = await _db.Rulesets.FindAsync(created.Id);
        Assert.That(fromDb!.IsValid, Is.False);
    }

    // ========================================================
    // DeleteAsync
    // ========================================================

    [Test]
    public async Task DeleteAsync_NonExistentRuleset_ReturnsFalse()
    {
        bool result = await _sut.DeleteAsync("user1", 9999);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DeleteAsync_ExistingRuleset_RemovesFromDatabase()
    {
        // ExecuteUpdateAsync is not supported by EF InMemory — use SQLite instead.
        (AppDbContext db, RulesetService sut) = CreateSqliteContext();

        RulesetResponseDto created = await sut.CreateAsync("user1", MakeCreateDto());

        bool result = await sut.DeleteAsync("user1", created.Id);

        Assert.That(result, Is.True);
        Ruleset? fromDb = await db.Rulesets.FindAsync(created.Id);
        Assert.That(fromDb, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_MiddleRuleset_RenumbersRemainingPrioritiesWithNoGaps()
    {
        // ExecuteUpdateAsync is not supported by EF InMemory — use SQLite instead.
        (AppDbContext db, RulesetService sut) = CreateSqliteContext();

        RulesetResponseDto first = await sut.CreateAsync("user1", MakeCreateDto(name: "First"));
        RulesetResponseDto second = await sut.CreateAsync("user1", MakeCreateDto(name: "Second"));
        RulesetResponseDto third = await sut.CreateAsync("user1", MakeCreateDto(name: "Third"));

        await sut.DeleteAsync("user1", second.Id);

        List<Ruleset> remaining = await db.Rulesets
            .Where(r => r.UserId == "user1")
            .OrderBy(r => r.Priority)
            .ToListAsync();

        Assert.That(remaining, Has.Count.EqualTo(2));
        Assert.That(remaining[0].Priority, Is.EqualTo(0));
        Assert.That(remaining[1].Priority, Is.EqualTo(1));
    }

    // ========================================================
    // ReorderAsync
    // ========================================================

    [Test]
    public async Task ReorderAsync_OrderedIdsCountMismatch_ReturnsNull()
    {
        await SeedRulesetAsync(name: "A");
        await SeedRulesetAsync(name: "B");

        // Provide only one ID for two rulesets
        List<RulesetResponseDto>? result = await _sut.ReorderAsync("user1", new ReorderRulesetsDto([999]));

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ReorderAsync_IdDoesNotBelongToUser_ReturnsNull()
    {
        RulesetResponseDto a = await SeedRulesetAsync(name: "A");

        List<RulesetResponseDto>? result = await _sut.ReorderAsync("user1", new ReorderRulesetsDto([a.Id, 9999]));

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ReorderAsync_ValidOrder_UpdatesPrioritiesAccordingToPosition()
    {
        RulesetResponseDto a = await SeedRulesetAsync(name: "A");
        RulesetResponseDto b = await SeedRulesetAsync(name: "B");
        RulesetResponseDto c = await SeedRulesetAsync(name: "C");

        // Reverse the order
        List<RulesetResponseDto>? result = await _sut.ReorderAsync(
            "user1",
            new ReorderRulesetsDto([c.Id, b.Id, a.Id]));

        Assert.That(result, Is.Not.Null);

        Ruleset? rulesetC = await _db.Rulesets.FindAsync(c.Id);
        Ruleset? rulesetB = await _db.Rulesets.FindAsync(b.Id);
        Ruleset? rulesetA = await _db.Rulesets.FindAsync(a.Id);

        Assert.That(rulesetC!.Priority, Is.EqualTo(0));
        Assert.That(rulesetB!.Priority, Is.EqualTo(1));
        Assert.That(rulesetA!.Priority, Is.EqualTo(2));
    }

    // ========================================================
    // ToggleEnabledAsync
    // ========================================================

    [Test]
    public async Task ToggleEnabledAsync_NonExistentRuleset_ReturnsNull()
    {
        RulesetResponseDto? result = await _sut.ToggleEnabledAsync("user1", 9999);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ToggleEnabledAsync_RulesetIsEnabled_FlipsToDisabled()
    {
        RulesetResponseDto created = await _sut.CreateAsync("user1", MakeCreateDto(isEnabled: true));

        RulesetResponseDto? result = await _sut.ToggleEnabledAsync("user1", created.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsEnabled, Is.False);

        Ruleset? fromDb = await _db.Rulesets.FindAsync(created.Id);
        Assert.That(fromDb!.IsEnabled, Is.False);
    }

    [Test]
    public async Task ToggleEnabledAsync_RulesetIsDisabled_FlipsToEnabled()
    {
        RulesetResponseDto created = await _sut.CreateAsync("user1", MakeCreateDto(isEnabled: false));

        RulesetResponseDto? result = await _sut.ToggleEnabledAsync("user1", created.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsEnabled, Is.True);

        Ruleset? fromDb = await _db.Rulesets.FindAsync(created.Id);
        Assert.That(fromDb!.IsEnabled, Is.True);
    }

    // ========================================================
    // ShareAsync
    // ========================================================

    [Test]
    public async Task ShareAsync_NonExistentRuleset_ReturnsNull()
    {
        (RulesetTemplateResponseDto Template, List<string> SanitizedProperties)? result =
            await _sut.ShareAsync("user1", 9999, MakeShareDto());

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ShareAsync_RulesetWithFilter_CallsSanitizerWithRulesetsFilter()
    {
        CheckFilter filter = MakeCheckFilter();
        _sanitizer.SanitizeForSharing(Arg.Any<FilterExpression>())
            .Returns((filter, new List<string>()));

        RulesetResponseDto created = await SeedRulesetAsync(filter: filter);

        await _sut.ShareAsync("user1", created.Id, MakeShareDto());

        _sanitizer.Received(1).SanitizeForSharing(Arg.Any<FilterExpression>());
    }

    [Test]
    public async Task ShareAsync_ValidRuleset_CreatesRulesetTemplateInDatabase()
    {
        RulesetResponseDto created = await SeedRulesetAsync();

        await _sut.ShareAsync("user1", created.Id, MakeShareDto(name: "My Template"));

        RulesetTemplate? fromDb = await _db.RulesetTemplates
            .FirstOrDefaultAsync(t => t.CreatedByUserId == "user1");
        Assert.That(fromDb, Is.Not.Null);
        Assert.That(fromDb!.Name, Is.EqualTo("My Template"));
    }

    [Test]
    public async Task ShareAsync_ValidRuleset_ReturnsNonNullNonEmptyShareToken()
    {
        RulesetResponseDto created = await SeedRulesetAsync();

        (RulesetTemplateResponseDto Template, List<string> SanitizedProperties)? result =
            await _sut.ShareAsync("user1", created.Id, MakeShareDto());

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Template.ShareToken, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task ShareAsync_SanitizerReturnsSanitizedProperties_ReturnsSanitizedPropertiesFromSanitizer()
    {
        CheckFilter filter = MakeCheckFilter(property: "gear_id");
        var sanitizedFilter = new CheckFilter("gear_id", "equals", null);
        var sanitizedProperties = new List<string> { "gear_id" };

        _sanitizer.SanitizeForSharing(Arg.Any<FilterExpression>())
            .Returns((sanitizedFilter, sanitizedProperties));

        RulesetResponseDto created = await SeedRulesetAsync(filter: filter);

        (RulesetTemplateResponseDto Template, List<string> SanitizedProperties)? result =
            await _sut.ShareAsync("user1", created.Id, MakeShareDto());

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.SanitizedProperties, Is.EquivalentTo(sanitizedProperties));
        Assert.That(result.Value.Template.SanitizedProperties, Is.EquivalentTo(sanitizedProperties));
    }

    // ========================================================
    // ShareAsync — BundledVariables
    // ========================================================

    private CustomVariable SeedCustomVariable(
        string userId = "user1",
        string name = "pace_label",
        string defaultValue = "Slow")
    {
        var definition = new CustomVariableDefinition
        {
            Name = name,
            Cases = [],
            DefaultValue = defaultValue
        };
        var variable = new CustomVariable
        {
            UserId = userId,
            Name = name,
            Definition = definition,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.CustomVariables.Add(variable);
        _db.SaveChanges();
        return variable;
    }

    [Test]
    public async Task ShareAsync_EffectWithNoVariableReferences_SetsBundledVariablesToNull()
    {
        var effect = new RulesetEffect { Name = "Morning ride" };
        RulesetResponseDto created = await SeedRulesetAsync(effect: effect);

        (RulesetTemplateResponseDto Template, List<string> SanitizedProperties)? result =
            await _sut.ShareAsync("user1", created.Id, MakeShareDto());

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Template.BundledVariables, Is.Null);

        RulesetTemplate? fromDb = await _db.RulesetTemplates.FirstAsync();
        Assert.That(fromDb.BundledVariables, Is.Null);
    }

    [Test]
    public async Task ShareAsync_NullEffect_SetsBundledVariablesToNull()
    {
        RulesetResponseDto created = await SeedRulesetAsync(effect: null);

        (RulesetTemplateResponseDto Template, List<string> SanitizedProperties)? result =
            await _sut.ShareAsync("user1", created.Id, MakeShareDto());

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Template.BundledVariables, Is.Null);
    }

    [Test]
    public async Task ShareAsync_EffectReferencesOnlyBuiltInVariables_SetsBundledVariablesToNull()
    {
        // Built-ins like {distance_km} have no CustomVariable row — they should not be bundled
        var effect = new RulesetEffect { Name = "{distance_km}km in {elapsed_time_human}" };
        RulesetResponseDto created = await SeedRulesetAsync(effect: effect);

        (RulesetTemplateResponseDto Template, List<string> SanitizedProperties)? result =
            await _sut.ShareAsync("user1", created.Id, MakeShareDto());

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Template.BundledVariables, Is.Null);
    }

    [Test]
    public async Task ShareAsync_EffectReferencesOneCustomVariable_BundlesThatVariablesDefinition()
    {
        CustomVariable paceLabel = SeedCustomVariable(name: "pace_label", defaultValue: "Slow");
        var effect = new RulesetEffect { Name = "Run — {pace_label}" };
        RulesetResponseDto created = await SeedRulesetAsync(effect: effect);

        (RulesetTemplateResponseDto Template, List<string> SanitizedProperties)? result =
            await _sut.ShareAsync("user1", created.Id, MakeShareDto());

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Template.BundledVariables, Has.Count.EqualTo(1));
        Assert.That(result.Value.Template.BundledVariables![0].Name, Is.EqualTo("pace_label"));
        Assert.That(result.Value.Template.BundledVariables[0].DefaultValue, Is.EqualTo("Slow"));
    }

    [Test]
    public async Task ShareAsync_EffectReferencesOneCustomVariable_PersistsBundledVariablesToDatabase()
    {
        SeedCustomVariable(name: "pace_label");
        var effect = new RulesetEffect { Name = "{pace_label}" };
        RulesetResponseDto created = await SeedRulesetAsync(effect: effect);

        await _sut.ShareAsync("user1", created.Id, MakeShareDto());

        RulesetTemplate? fromDb = await _db.RulesetTemplates.FirstAsync();
        Assert.That(fromDb.BundledVariables, Is.Not.Null);
        Assert.That(fromDb.BundledVariables!, Has.Count.EqualTo(1));
        Assert.That(fromDb.BundledVariables![0].Name, Is.EqualTo("pace_label"));
    }

    [Test]
    public async Task ShareAsync_EffectReferencesMultipleCustomVariables_BundlesAll()
    {
        SeedCustomVariable(name: "pace_label");
        SeedCustomVariable(name: "time_of_day", defaultValue: "Evening");
        var effect = new RulesetEffect
        {
            Name = "{time_of_day} run — {pace_label}",
            Description = "A {pace_label} {time_of_day} run"
        };
        RulesetResponseDto created = await SeedRulesetAsync(effect: effect);

        (RulesetTemplateResponseDto Template, List<string> SanitizedProperties)? result =
            await _sut.ShareAsync("user1", created.Id, MakeShareDto());

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Template.BundledVariables, Has.Count.EqualTo(2));

        IEnumerable<string> bundledNames = result.Value.Template.BundledVariables!.Select(v => v.Name);
        Assert.That(bundledNames, Is.EquivalentTo(new[] { "pace_label", "time_of_day" }));
    }

    [Test]
    public async Task ShareAsync_EffectReferencesSameVariableMultipleTimes_BundlesItOnce()
    {
        SeedCustomVariable(name: "pace_label");
        var effect = new RulesetEffect
        {
            Name = "{pace_label}",
            Description = "Today's pace: {pace_label}"
        };
        RulesetResponseDto created = await SeedRulesetAsync(effect: effect);

        (RulesetTemplateResponseDto Template, List<string> SanitizedProperties)? result =
            await _sut.ShareAsync("user1", created.Id, MakeShareDto());

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Template.BundledVariables, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ShareAsync_EffectMixesBuiltInAndCustomVariables_BundlesOnlyCustom()
    {
        SeedCustomVariable(name: "pace_label");
        var effect = new RulesetEffect { Name = "{distance_km}km — {pace_label}" };
        RulesetResponseDto created = await SeedRulesetAsync(effect: effect);

        (RulesetTemplateResponseDto Template, List<string> SanitizedProperties)? result =
            await _sut.ShareAsync("user1", created.Id, MakeShareDto());

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Template.BundledVariables, Has.Count.EqualTo(1));
        Assert.That(result.Value.Template.BundledVariables![0].Name, Is.EqualTo("pace_label"));
    }

    [Test]
    public async Task ShareAsync_ReferencedVariableBelongsToDifferentUser_DoesNotBundle()
    {
        _db.Users.Add(new AppUser { Id = "user2", UserName = "user2" });
        await _db.SaveChangesAsync();
        SeedCustomVariable(userId: "user2", name: "pace_label");

        var effect = new RulesetEffect { Name = "{pace_label}" };
        RulesetResponseDto created = await SeedRulesetAsync(effect: effect);

        (RulesetTemplateResponseDto Template, List<string> SanitizedProperties)? result =
            await _sut.ShareAsync("user1", created.Id, MakeShareDto());

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Template.BundledVariables, Is.Null);
    }

    [Test]
    public async Task ShareAsync_ReferencedVariableDoesNotExist_SetsBundledVariablesToNull()
    {
        // {unknown_var} referenced but user has no such custom variable
        var effect = new RulesetEffect { Name = "{unknown_var}" };
        RulesetResponseDto created = await SeedRulesetAsync(effect: effect);

        (RulesetTemplateResponseDto Template, List<string> SanitizedProperties)? result =
            await _sut.ShareAsync("user1", created.Id, MakeShareDto());

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Template.BundledVariables, Is.Null);
    }

    [Test]
    public async Task ShareAsync_CustomVariableInDescriptionOnly_BundlesIt()
    {
        SeedCustomVariable(name: "time_of_day");
        var effect = new RulesetEffect
        {
            Name = "Morning ride",
            Description = "Went out in the {time_of_day}"
        };
        RulesetResponseDto created = await SeedRulesetAsync(effect: effect);

        (RulesetTemplateResponseDto Template, List<string> SanitizedProperties)? result =
            await _sut.ShareAsync("user1", created.Id, MakeShareDto());

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Template.BundledVariables, Has.Count.EqualTo(1));
        Assert.That(result.Value.Template.BundledVariables![0].Name, Is.EqualTo("time_of_day"));
    }
}
