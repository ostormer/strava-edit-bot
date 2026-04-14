using Microsoft.AspNetCore.Identity;

namespace StravaEditBotApi.Models;

public class AppUser : IdentityUser
{
    public long? StravaAthleteId { get; set; }
    public string? StravaAccessToken { get; set; }
    public string? StravaRefreshToken { get; set; }
    public DateTime? StravaTokenExpiresAt { get; set; }
    public string? StravaFirstname { get; set; }
    public string? StravaLastname { get; set; }
    public string? StravaProfileMedium { get; set; }
    public string? StravaProfile { get; set; }
}
