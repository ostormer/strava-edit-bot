namespace StravaEditBotApi.Services;

public record StravaTokenData(
    long AthleteId,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);

public interface IStravaAuthService
{
    Task<StravaTokenData> ExchangeCodeAsync(string code);
}
