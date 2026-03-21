using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using StravaEditBotApi.Data;

namespace StravaEditBotApi.Tests.Integration;

public class WebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // In EF Core 8+, AddDbContext registers four service types:
            //   1. IDbContextOptionsConfiguration<T>  — the configuration action lambda (UseSqlServer etc.)
            //   2. DbContextOptions<T>                — the built options object
            //   3. DbContextOptions                   — non-generic alias of the above
            //   4. AppDbContext                       — the context itself
            //
            // When DbContextOptions<T> is resolved, EF Core applies ALL registered
            // IDbContextOptionsConfiguration<T> entries. If we only remove #2–4
            // and leave #1 behind, the Npgsql configuration action is still present.
            // AddDbContext for InMemory then adds a second configuration action,
            // and EF Core ends up with both providers registered — hence the error.
            //
            // We must remove all four before registering the InMemory replacement.

            Remove<IDbContextOptionsConfiguration<AppDbContext>>(services);
            Remove<DbContextOptions<AppDbContext>>(services);
            Remove<DbContextOptions>(services);
            Remove<AppDbContext>(services);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("IntegrationTestDb"));
        });
    }

    private static void Remove<T>(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(T));
        if (descriptor is not null)
        {

            services.Remove(descriptor);
        }

    }
}
