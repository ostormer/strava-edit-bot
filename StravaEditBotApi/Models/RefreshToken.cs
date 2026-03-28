namespace StravaEditBotApi.Models;

public class RefreshToken
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = default!;
    public AppUser User { get; set; } = default!;

    // SHA-256 hash of the raw token sent to the client.
    // We never store the raw token — if the DB leaks, sessions stay safe.
    public string TokenHash { get; set; } = default!;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
}
