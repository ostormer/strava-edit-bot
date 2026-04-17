namespace StravaEditBotApi.Services.Auth;

public record StravaTokenData(
    long AthleteId,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string Firstname = "",
    string Lastname = "",
    string ProfileMedium = "",
    string Profile = ""
);

public interface IStravaAuthService
{
    Task<StravaTokenData> ExchangeCodeAsync(string code);
}
