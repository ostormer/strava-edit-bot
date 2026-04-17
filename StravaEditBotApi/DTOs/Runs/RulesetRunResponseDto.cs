namespace StravaEditBotApi.DTOs.Runs;

public record RulesetRunResponseDto(
    long Id,
    long StravaActivityId,
    int? RulesetId,
    string? RulesetName,
    string Status,
    string? ErrorMessage,
    Dictionary<string, string>? FieldsChanged,
    DateTime ProcessedAt,
    DateTime StravaEventTime
);
