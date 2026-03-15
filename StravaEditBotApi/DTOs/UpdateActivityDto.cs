namespace StravaEditBotApi.DTOs;

public record UpdateActivityDto(
    string? Name,
    string? Description,
    string? ActivitySport,
    DateTime? StartTime,
    double? Distance,
    TimeSpan? ElapsedTime
);
