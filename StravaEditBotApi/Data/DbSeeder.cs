using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StravaEditBotApi.Models;
using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.Data;

/// <summary>
/// Seeds and upserts predefined system templates on startup.
/// Each template is identified by a stable <see cref="RulesetTemplate.SeedKey"/>.
/// New keys are inserted, existing keys are updated, keys removed from the list are deleted.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        DateTime now = DateTime.UtcNow;

        List<RulesetTemplate> desired = BuildSystemTemplates(now);
        HashSet<string> desiredKeys = desired.Select(t => t.SeedKey!).ToHashSet();

        List<RulesetTemplate> existing = await db.RulesetTemplates
            .Where(t => t.SeedKey != null)
            .ToListAsync();

        Dictionary<string, RulesetTemplate> existingByKey = existing
            .ToDictionary(t => t.SeedKey!);

        // Insert new, update existing
        foreach (RulesetTemplate template in desired)
        {
            if (existingByKey.TryGetValue(template.SeedKey!, out RulesetTemplate? current))
            {
                current.Name = template.Name;
                current.Description = template.Description;
                current.Filter = template.Filter;
                current.Effect = template.Effect;
                current.IsPublic = template.IsPublic;
                current.BundledVariables = template.BundledVariables;
            }
            else
            {
                db.RulesetTemplates.Add(template);
            }
        }

        // Delete removed
        foreach (RulesetTemplate stale in existing.Where(t => !desiredKeys.Contains(t.SeedKey!)))
        {
            db.RulesetTemplates.Remove(stale);
        }

        await db.SaveChangesAsync();
    }

    private static List<RulesetTemplate> BuildSystemTemplates(DateTime now)
    {
        return
        [
            new RulesetTemplate
            {
                SeedKey = "morning-commute-ride",
                Name = "Morning Commute Ride",
                Description = "Automatically marks bike rides before 9 AM on weekdays as commutes.",
                Filter = new AndFilter(
                [
                    new CheckFilter("sport_type", "in", JsonSerializer.SerializeToElement(new[] { "Ride", "EBikeRide" })),
                    new CheckFilter("start_time", "before", JsonSerializer.SerializeToElement("09:00")),
                    new CheckFilter("day_of_week", "not_in", JsonSerializer.SerializeToElement(new[] { "Saturday", "Sunday" }))
                ]),
                Effect = new RulesetEffect { Commute = true, Name = "Bike commute to work" },
                IsPublic = true,
                CreatedAt = now
            },
            new RulesetTemplate
            {
                SeedKey = "evening-run-namer",
                Name = "Evening Run Namer",
                Description = "Renames evening runs with sport type and distance.",
                Filter = new AndFilter(
                [
                    new CheckFilter("sport_type", "in", JsonSerializer.SerializeToElement(new[] { "Run", "TrailRun" })),
                    new CheckFilter("start_time", "after", JsonSerializer.SerializeToElement("17:00"))
                ]),
                Effect = new RulesetEffect { Name = "Evening {sport_type} — {distance_km}km" },
                IsPublic = true,
                CreatedAt = now
            },
            new RulesetTemplate
            {
                SeedKey = "weekend-long-run",
                Name = "Weekend Long Run",
                Description = "Labels long weekend runs with distance and elevation.",
                Filter = new AndFilter(
                [
                    new CheckFilter("sport_type", "in", JsonSerializer.SerializeToElement(new[] { "Run", "TrailRun" })),
                    new CheckFilter("day_of_week", "in", JsonSerializer.SerializeToElement(new[] { "Saturday", "Sunday" })),
                    new CheckFilter("distance_meters", "gt", JsonSerializer.SerializeToElement(15000))
                ]),
                Effect = new RulesetEffect { Name = "Long run — {distance_km}km, {elevation_gain_m}m gain" },
                IsPublic = true,
                CreatedAt = now
            },
            new RulesetTemplate
            {
                SeedKey = "trainer-ride-labeler",
                Name = "Trainer Ride Labeler",
                Description = "Labels indoor trainer rides and hides them from the home feed.",
                Filter = new AndFilter(
                [
                    new CheckFilter("is_trainer", "eq", JsonSerializer.SerializeToElement(true)),
                    new CheckFilter("sport_type", "in", JsonSerializer.SerializeToElement(new[] { "Ride", "VirtualRide" }))
                ]),
                Effect = new RulesetEffect { Name = "Indoor ride — {elapsed_time_human}", HideFromHome = true },
                IsPublic = true,
                CreatedAt = now
            },
            new RulesetTemplate
            {
                SeedKey = "lunchtime-walk",
                Name = "Lunchtime Walk",
                Description = "Labels walks between 11 AM and 2 PM as lunch walks.",
                Filter = new AndFilter(
                [
                    new CheckFilter("sport_type", "in", JsonSerializer.SerializeToElement(new[] { "Walk" })),
                    new CheckFilter("start_time", "after", JsonSerializer.SerializeToElement("11:00")),
                    new CheckFilter("start_time", "before", JsonSerializer.SerializeToElement("14:00"))
                ]),
                Effect = new RulesetEffect { Name = "Lunch walk", Commute = false },
                IsPublic = true,
                CreatedAt = now
            }
        ];
    }
}
