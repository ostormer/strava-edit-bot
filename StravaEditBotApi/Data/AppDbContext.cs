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
    private static readonly ValueComparer<Dictionary<string, string>> DictComparer = new(
        (a, b) => JsonSerializer.Serialize(a, JsonOptions) == JsonSerializer.Serialize(b, JsonOptions),
        v => JsonSerializer.Serialize(v, JsonOptions).GetHashCode(),
        v => JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(v, JsonOptions), JsonOptions)!);

    private static readonly ValueComparer<List<CustomVariableDefinition>> BundledVarsComparer = new(
        (a, b) => JsonSerializer.Serialize(a, JsonOptions) == JsonSerializer.Serialize(b, JsonOptions),
        v => JsonSerializer.Serialize(v, JsonOptions).GetHashCode(),
        v => JsonSerializer.Deserialize<List<CustomVariableDefinition>>(JsonSerializer.Serialize(v, JsonOptions), JsonOptions)!);

    private static readonly JsonSerializerOptions JsonOptions = new()
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
                    v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
                    v => v == null ? null : JsonSerializer.Deserialize<FilterExpression>(v, JsonOptions));

            entity.Property(r => r.Effect)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
                    v => v == null ? null : JsonSerializer.Deserialize<RulesetEffect>(v, JsonOptions));

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

            entity.Property(t => t.Name).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(2000);

            entity.Property(t => t.Filter)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
                    v => v == null ? null : JsonSerializer.Deserialize<FilterExpression>(v, JsonOptions));

            entity.Property(t => t.Effect)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
                    v => v == null ? null : JsonSerializer.Deserialize<RulesetEffect>(v, JsonOptions));

            entity.Property(t => t.BundledVariables)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
                    v => v == null ? null : JsonSerializer.Deserialize<List<CustomVariableDefinition>>(v, JsonOptions))
                .Metadata.SetValueComparer(BundledVarsComparer);

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
                    v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
                    v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonOptions))
                .Metadata.SetValueComparer(DictComparer);

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
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<CustomVariableDefinition>(v, JsonOptions)!);

            entity.HasOne(cv => cv.User)
                .WithMany(u => u.CustomVariables)
                .HasForeignKey(cv => cv.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
