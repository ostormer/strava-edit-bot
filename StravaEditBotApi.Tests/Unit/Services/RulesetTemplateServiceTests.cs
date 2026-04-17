using Microsoft.EntityFrameworkCore;
using NSubstitute;
using StravaEditBotApi.Data;
using StravaEditBotApi.Models;
using StravaEditBotApi.Models.Rules;
using StravaEditBotApi.Services;

namespace StravaEditBotApi.Tests.Unit.Services;

[TestFixture]
public class RulesetTemplateServiceTests
{
    private AppDbContext _db = null!;
    private IRulesetValidator _validator = null!;
    private RulesetTemplateService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _validator = Substitute.For<IRulesetValidator>();
        _sut = new RulesetTemplateService(_db, _validator);

        _validator.Validate(Arg.Any<FilterExpression?>(), Arg.Any<RulesetEffect?>())
            .Returns(new RulesetValidationResult(true, []));

        _db.Users.Add(new AppUser { Id = "user1", UserName = "user1" });
        _db.SaveChanges();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ========================================================
    // GetPublicTemplatesAsync
    // ========================================================

    [Test]
    public async Task GetPublicTemplatesAsync_NoPublicTemplates_ReturnsEmptyList()
    {
        var result = await _sut.GetPublicTemplatesAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetPublicTemplatesAsync_MixedVisibility_ReturnsOnlyPublicTemplates()
    {
        _db.RulesetTemplates.Add(MakeTemplate(name: "Public One", isPublic: true));
        _db.RulesetTemplates.Add(MakeTemplate(name: "Private One", isPublic: false));
        await _db.SaveChangesAsync();

        var result = await _sut.GetPublicTemplatesAsync();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Public One"));
    }

    [Test]
    public async Task GetPublicTemplatesAsync_MultiplePublicTemplates_ReturnsOrderedByUsageCountDescending()
    {
        _db.RulesetTemplates.Add(MakeTemplate(name: "Zero Usage", isPublic: true, usageCount: 0));
        _db.RulesetTemplates.Add(MakeTemplate(name: "High Usage", isPublic: true, usageCount: 50));
        _db.RulesetTemplates.Add(MakeTemplate(name: "Low Usage", isPublic: true, usageCount: 5));
        await _db.SaveChangesAsync();

        var result = await _sut.GetPublicTemplatesAsync();

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0].Name, Is.EqualTo("High Usage"));
        Assert.That(result[1].Name, Is.EqualTo("Low Usage"));
        Assert.That(result[2].Name, Is.EqualTo("Zero Usage"));
    }

    // ========================================================
    // GetByShareTokenAsync
    // ========================================================

    [Test]
    public async Task GetByShareTokenAsync_UnknownToken_ReturnsNull()
    {
        var result = await _sut.GetByShareTokenAsync("nonexistent-token");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByShareTokenAsync_KnownToken_ReturnsMatchingTemplate()
    {
        _db.RulesetTemplates.Add(MakeTemplate(name: "Shared Template", shareToken: "abc-123"));
        await _db.SaveChangesAsync();

        var result = await _sut.GetByShareTokenAsync("abc-123");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Shared Template"));
    }

    [Test]
    public async Task GetByShareTokenAsync_PrivateTemplate_ReturnsTemplateViaShareToken()
    {
        _db.RulesetTemplates.Add(MakeTemplate(name: "Private Shared", isPublic: false, shareToken: "private-token-xyz"));
        await _db.SaveChangesAsync();

        var result = await _sut.GetByShareTokenAsync("private-token-xyz");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsPublic, Is.False);
        Assert.That(result.Name, Is.EqualTo("Private Shared"));
    }

    // ========================================================
    // InstantiateAsync
    // ========================================================

    [Test]
    public async Task InstantiateAsync_NonExistentTemplate_ReturnsNull()
    {
        var result = await _sut.InstantiateAsync("user1", templateId: 9999);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task InstantiateAsync_ValidTemplate_CreatesRulesetWithFilterAndEffect()
    {
        var filter = new CheckFilter("sport_type", "eq", null);
        var effect = new RulesetEffect { Name = "Morning Run" };
        var template = MakeTemplate(name: "My Template", filter: filter, effect: effect);
        _db.RulesetTemplates.Add(template);
        await _db.SaveChangesAsync();

        await _sut.InstantiateAsync("user1", template.Id);

        var ruleset = await _db.Rulesets.SingleOrDefaultAsync(r => r.UserId == "user1");
        Assert.That(ruleset, Is.Not.Null);
        Assert.That(ruleset!.Name, Is.EqualTo("My Template"));
        Assert.That(ruleset.Filter, Is.Not.Null);
        Assert.That(ruleset.Effect, Is.Not.Null);
        Assert.That(ruleset.CreatedFromTemplateId, Is.EqualTo(template.Id));
    }

    [Test]
    public async Task InstantiateAsync_ValidTemplate_IncrementsUsageCount()
    {
        var template = MakeTemplate(name: "Counted Template", usageCount: 3);
        _db.RulesetTemplates.Add(template);
        await _db.SaveChangesAsync();

        await _sut.InstantiateAsync("user1", template.Id);

        var fromDb = await _db.RulesetTemplates.FindAsync(template.Id);
        Assert.That(fromDb!.UsageCount, Is.EqualTo(4));
    }

    [Test]
    public async Task InstantiateAsync_ValidTemplate_SetsIsValidFromValidatorResult()
    {
        _validator.Validate(Arg.Any<FilterExpression?>(), Arg.Any<RulesetEffect?>())
            .Returns(new RulesetValidationResult(false, []));

        var template = MakeTemplate(name: "Invalid Template");
        _db.RulesetTemplates.Add(template);
        await _db.SaveChangesAsync();

        await _sut.InstantiateAsync("user1", template.Id);

        var ruleset = await _db.Rulesets.SingleOrDefaultAsync(r => r.UserId == "user1");
        Assert.That(ruleset, Is.Not.Null);
        Assert.That(ruleset!.IsValid, Is.False);
    }

    [Test]
    public async Task InstantiateAsync_UserHasNoRulesets_CreateRulesetWithPriorityZero()
    {
        var template = MakeTemplate(name: "First Ruleset Template");
        _db.RulesetTemplates.Add(template);
        await _db.SaveChangesAsync();

        await _sut.InstantiateAsync("user1", template.Id);

        var ruleset = await _db.Rulesets.SingleOrDefaultAsync(r => r.UserId == "user1");
        Assert.That(ruleset, Is.Not.Null);
        Assert.That(ruleset!.Priority, Is.EqualTo(0));
    }

    [Test]
    public async Task InstantiateAsync_UserHasExistingRulesets_CreateRulesetWithMaxPriorityPlusOne()
    {
        _db.Rulesets.Add(MakeRuleset("user1", priority: 0, name: "Existing A"));
        _db.Rulesets.Add(MakeRuleset("user1", priority: 1, name: "Existing B"));
        await _db.SaveChangesAsync();

        var template = MakeTemplate(name: "New Template");
        _db.RulesetTemplates.Add(template);
        await _db.SaveChangesAsync();

        await _sut.InstantiateAsync("user1", template.Id);

        var newRuleset = await _db.Rulesets.SingleOrDefaultAsync(r => r.Name == "New Template");
        Assert.That(newRuleset, Is.Not.Null);
        Assert.That(newRuleset!.Priority, Is.EqualTo(2));
    }

    [Test]
    public async Task InstantiateAsync_TemplateBundledVariables_UserHasNone_CreatesCustomVariables()
    {
        var template = MakeTemplate(
            name: "Template With Vars",
            bundledVariables:
            [
                new CustomVariableDefinition { Name = "pace_label", Cases = [], DefaultValue = "Slow" },
                new CustomVariableDefinition { Name = "effort_level", Cases = [], DefaultValue = "Easy" }
            ]);
        _db.RulesetTemplates.Add(template);
        await _db.SaveChangesAsync();

        await _sut.InstantiateAsync("user1", template.Id);

        var variables = await _db.CustomVariables
            .Where(cv => cv.UserId == "user1")
            .ToListAsync();

        Assert.That(variables, Has.Count.EqualTo(2));
        Assert.That(variables.Select(v => v.Name), Is.EquivalentTo(new[] { "pace_label", "effort_level" }));
    }

    [Test]
    public async Task InstantiateAsync_TemplateBundledVariables_UserAlreadyHasSameName_SkipsDuplicate()
    {
        _db.CustomVariables.Add(new CustomVariable
        {
            UserId = "user1",
            Name = "pace_label",
            Definition = new CustomVariableDefinition { Name = "pace_label", Cases = [], DefaultValue = "My Custom Value" },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var template = MakeTemplate(
            name: "Template With Conflict",
            bundledVariables:
            [
                new CustomVariableDefinition { Name = "pace_label", Cases = [], DefaultValue = "Slow" },
                new CustomVariableDefinition { Name = "new_variable", Cases = [], DefaultValue = "Fresh" }
            ]);
        _db.RulesetTemplates.Add(template);
        await _db.SaveChangesAsync();

        await _sut.InstantiateAsync("user1", template.Id);

        var variables = await _db.CustomVariables
            .Where(cv => cv.UserId == "user1")
            .ToListAsync();

        Assert.That(variables, Has.Count.EqualTo(2));

        var existing = variables.Single(v => v.Name == "pace_label");
        Assert.That(existing.Definition.DefaultValue, Is.EqualTo("My Custom Value"));

        Assert.That(variables.Any(v => v.Name == "new_variable"), Is.True);
    }

    // ========================================================
    // Factory helpers
    // ========================================================

    private static RulesetTemplate MakeTemplate(
        string name = "Test Template",
        string? description = null,
        bool isPublic = true,
        string? shareToken = null,
        int usageCount = 0,
        FilterExpression? filter = null,
        RulesetEffect? effect = null,
        List<CustomVariableDefinition>? bundledVariables = null)
    {
        return new RulesetTemplate
        {
            Name = name,
            Description = description,
            IsPublic = isPublic,
            ShareToken = shareToken,
            UsageCount = usageCount,
            Filter = filter,
            Effect = effect,
            BundledVariables = bundledVariables,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Ruleset MakeRuleset(string userId, int priority, string name = "Ruleset")
    {
        return new Ruleset
        {
            UserId = userId,
            Name = name,
            Priority = priority,
            IsEnabled = true,
            IsValid = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
