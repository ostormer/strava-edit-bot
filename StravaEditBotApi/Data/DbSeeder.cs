using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StravaEditBotApi.Models;
using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.Data;

/// <summary>
/// Seeds predefined system templates on startup if they don't already exist.
/// Identified by name — if a template with the same name exists, it is skipped.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        bool anyExist = await db.RulesetTemplates
            .AnyAsync(t => t.CreatedByUserId == null);

        if (anyExist)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;

        List<RulesetTemplate> templates =
        [
            new RulesetTemplate
            {
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

        await db.RulesetTemplates.AddRangeAsync(templates);
        await db.SaveChangesAsync();
    }
}
