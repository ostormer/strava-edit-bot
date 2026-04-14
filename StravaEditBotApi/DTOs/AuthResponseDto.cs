namespace StravaEditBotApi.DTOs;

public record AuthResponseDto(
    string AccessToken,
    string Firstname,
    string Lastname,
    string ProfileMedium,
    string Profile
);
