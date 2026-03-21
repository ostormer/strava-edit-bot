namespace StravaEditBotApi.DTOs;

public record CreateActivityDto(
    string Name,
    string? Description,
    string ActivitySport,
    DateTime StartTime,
    double Distance,
    TimeSpan ElapsedTime
);
