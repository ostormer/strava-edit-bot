using StravaEditBotApi.Models.Rules;

namespace StravaEditBotApi.Models;

public class CustomVariable
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Variable name (without braces). Pattern: [a-z][a-z0-9_]*, max 50 chars.
    /// Must not collide with built-in variable names.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public CustomVariableDefinition Definition { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public AppUser User { get; set; } = null!;
}
