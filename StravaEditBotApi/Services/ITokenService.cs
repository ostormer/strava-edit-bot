using StravaEditBotApi.Models;

namespace StravaEditBotApi.Services;

public interface ITokenService
{
    string GenerateAccessToken(AppUser user);
    string GenerateRefreshToken();
    string HashToken(string token);
}
