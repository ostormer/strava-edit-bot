namespace StravaEditBotApi.DTOs;

using System.ComponentModel.DataAnnotations;

public record CreateActivityDto
(
    [Required] string Name,
    string Description,
    [Required] string ActivitySport,
    DateTime StartTime,
    [Range(0, 1000)] double Distance,
    TimeSpan ElapsedTime
);