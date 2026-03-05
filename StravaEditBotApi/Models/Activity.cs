namespace StravaEditBotApi.Models;

using System.ComponentModel.DataAnnotations;

public class Activity
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = default!;

    public string Description { get; set; } = string.Empty;

    [Required]
    public string ActivitySport { get; set; } = default!;

    public DateTime StartTime { get; set; }

    [Range(0, 1000)]
    public double Distance { get; set; }

    public TimeSpan ElapsedTime { get; set; }

    // Parameterless constructor (required by EF)
    private Activity() { }

    // Optional convenience constructor
    public Activity(
        string name,
        string description,
        string activitySport,
        DateTime startTime,
        double distance,
        TimeSpan elapsedTime)
    {
        Name = name;
        Description = description;
        ActivitySport = activitySport;
        StartTime = startTime;
        Distance = distance;
        ElapsedTime = elapsedTime;
    }
}