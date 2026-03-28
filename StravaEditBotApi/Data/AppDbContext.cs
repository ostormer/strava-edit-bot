using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StravaEditBotApi.Models;

namespace StravaEditBotApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
}
