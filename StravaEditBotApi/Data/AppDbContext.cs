using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using StravaEditBotApi.Models;
using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Ruleset> Rulesets => Set<Ruleset>();
    public DbSet<RulesetTemplate> RulesetTemplates => Set<RulesetTemplate>();
    public DbSet<RulesetRun> RulesetRuns => Set<RulesetRun>();
    public DbSet<CustomVariable> CustomVariables => Set<CustomVariable>();

    // Value comparer for collection-type JSON columns so EF Core detects changes correctly.
    private static readonly ValueComparer<Dictionary<string, string>> _dictComparer = new(
        (a, b) => JsonSerializer.Serialize(a, _jsonOptions) == JsonSerializer.Serialize(b, _jsonOptions),
        v => JsonSerializer.Serialize(v, _jsonOptions).GetHashCode(),
        v => JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(v, _jsonOptions), _jsonOptions)!);

    private static readonly ValueComparer<List<CustomVariableDefinition>> _bundledVarsComparer = new(
        (a, b) => JsonSerializer.Serialize(a, _jsonOptions) == JsonSerializer.Serialize(b, _jsonOptions),
        v => JsonSerializer.Serialize(v, _jsonOptions).GetHashCode(),
        v => JsonSerializer.Deserialize<List<CustomVariableDefinition>>(JsonSerializer.Serialize(v, _jsonOptions), _jsonOptions)!);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Ruleset>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.HasIndex(r => new { r.UserId, r.Priority }).IsUnique();
            entity.HasIndex(r => r.UserId);

            entity.Property(r => r.Name).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Description).HasMaxLength(2000);
            entity.Property(r => r.IsEnabled).HasDefaultValue(true);
            entity.Property(r => r.IsValid).HasDefaultValue(false);

            entity.Property(r => r.Filter)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, _jsonOptions),
                    v => v == null ? null : JsonSerializer.Deserialize<FilterExpression>(v, _jsonOptions));

            entity.Property(r => r.Effect)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, _jsonOptions),
                    v => v == null ? null : JsonSerializer.Deserialize<RulesetEffect>(v, _jsonOptions));

            entity.HasOne(r => r.User)
                .WithMany(u => u.Rulesets)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.CreatedFromTemplate)
                .WithMany(t => t.Rulesets)
                .HasForeignKey(r => r.CreatedFromTemplateId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<RulesetTemplate>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.HasIndex(t => t.ShareToken)
                .IsUnique()
                .HasFilter("[ShareToken] IS NOT NULL");

            entity.HasIndex(t => t.SeedKey)
                .IsUnique()
                .HasFilter("[SeedKey] IS NOT NULL");

            entity.Property(t => t.SeedKey).HasMaxLength(100);
            entity.Property(t => t.Name).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(2000);

            entity.Property(t => t.Filter)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, _jsonOptions),
                    v => v == null ? null : JsonSerializer.Deserialize<FilterExpression>(v, _jsonOptions));

            entity.Property(t => t.Effect)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, _jsonOptions),
                    v => v == null ? null : JsonSerializer.Deserialize<RulesetEffect>(v, _jsonOptions));

            entity.Property(t => t.BundledVariables)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, _jsonOptions),
                    v => v == null ? null : JsonSerializer.Deserialize<List<CustomVariableDefinition>>(v, _jsonOptions))
                .Metadata.SetValueComparer(_bundledVarsComparer);

            entity.HasOne(t => t.CreatedByUser)
                .WithMany(u => u.RulesetTemplates)
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<RulesetRun>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.HasIndex(r => new { r.UserId, r.ProcessedAt });
            entity.HasIndex(r => r.StravaActivityId);

            entity.Property(r => r.Status).HasMaxLength(50).IsRequired();
            entity.Property(r => r.RulesetName).HasMaxLength(200);

            entity.Property(r => r.FieldsChanged)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, _jsonOptions),
                    v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(v, _jsonOptions))
                .Metadata.SetValueComparer(_dictComparer);

            entity.HasOne(r => r.User)
                .WithMany(u => u.RulesetRuns)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // NoAction on DB — SQL Server disallows SET NULL here due to multiple cascade paths
            // (AppUser→Rulesets→RulesetRuns and AppUser→RulesetRuns both cascade on user delete).
            // RulesetService.DeleteAsync nullifies this FK explicitly before deletion.
            entity.HasOne(r => r.Ruleset)
                .WithMany(s => s.Runs)
                .HasForeignKey(r => r.RulesetId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<CustomVariable>(entity =>
        {
            entity.HasKey(cv => cv.Id);

            entity.HasIndex(cv => new { cv.UserId, cv.Name }).IsUnique();

            entity.Property(cv => cv.Name).HasMaxLength(50).IsRequired();
            entity.Property(cv => cv.Description).HasMaxLength(500);

            entity.Property(cv => cv.Definition)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, _jsonOptions),
                    v => JsonSerializer.Deserialize<CustomVariableDefinition>(v, _jsonOptions)!);

            entity.HasOne(cv => cv.User)
                .WithMany(u => u.CustomVariables)
                .HasForeignKey(cv => cv.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
