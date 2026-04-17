namespace StravaEditBotApi.DTOs.Auth;

public record AuthResponseDto(
    string AccessToken,
    string Firstname,
    string Lastname,
    string ProfileMedium,
    string Profile
);
