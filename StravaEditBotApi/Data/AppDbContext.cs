using Microsoft.EntityFrameworkCore;
using StravaEditBotApi.Models;

namespace StravaEditBotApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Activity> Activities => Set<Activity>();
}
