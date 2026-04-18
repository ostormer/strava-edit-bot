using System.Text;
using System.Threading.Channels;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StravaEditBotApi.Data;
using StravaEditBotApi.DTOs.Auth;
using StravaEditBotApi.DTOs.Webhook;
using StravaEditBotApi.Middleware;
using StravaEditBotApi.Models;
using StravaEditBotApi.Services.Auth;
using StravaEditBotApi.Services.Rulesets;
using StravaEditBotApi.Services.Webhook;

var builder = WebApplication.CreateBuilder(args);

// Registers UserManager<AppUser> and EF stores for the AspNetUsers table.
// AddIdentityCore (not AddIdentity) avoids setting cookie auth as the default scheme,
// which would conflict with JWT bearer on an API.
// No password authentication — users authenticate via Strava OAuth.
builder.Services.AddIdentityCore<AppUser>()
    .AddEntityFrameworkStores<AppDbContext>();

if (builder.Environment.IsDevelopment())
{
    // In Development, bypass auth entirely so endpoints can be hit without a real JWT.
    builder.Services.AddAuthentication("DevBypass")
        .AddScheme<AuthenticationSchemeOptions, DevBypassAuthenticationHandler>("DevBypass", null);
}
else
{
    // In Production, validate JWTs we issued ourselves.
    // The signing key lives in Azure Key Vault (or user secrets locally for non-dev environments).
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
            };
        });
}

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);
builder.Services.AddControllers(options =>
    options.SuppressAsyncSuffixInActionNames = false);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS — allow the frontend origin to call the API and send cookies (refresh token).
// In dev this is the Vite dev server. In production it's set via the
// Cors__AllowedOrigins environment variable on the App Service.
string[] allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // required for the HttpOnly refresh token cookie
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add FluentValidation — this does two things:
// 1. Scans the assembly for all AbstractValidator<T> classes and registers them
// 2. Hooks them into ASP.NET's model validation pipeline
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Custom services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddHttpClient<IStravaAuthService, StravaAuthService>();

// Ruleset engine
builder.Services.AddScoped<IRulesetValidator, RulesetValidator>();
builder.Services.AddScoped<IFilterSanitizer, FilterSanitizer>();
builder.Services.AddScoped<IFilterEvaluator, FilterEvaluator>();
builder.Services.AddScoped<IRulesetService, RulesetService>();
builder.Services.AddScoped<IRulesetTemplateService, RulesetTemplateService>();
builder.Services.AddScoped<ICustomVariableService, CustomVariableService>();

// Webhook infrastructure
var webhookChannel = Channel.CreateUnbounded<StravaWebhookEventDto>();
builder.Services.AddSingleton(webhookChannel);
builder.Services.AddSingleton(webhookChannel.Reader);
builder.Services.AddSingleton(webhookChannel.Writer);
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddHostedService<WebhookBackgroundService>();

var app = builder.Build();

// Apply any pending EF Core migrations on startup.
// This runs using the app's managed identity, so no CI runner SQL access is needed.
// Safe for single-instance deployments (App Service B1); for multi-instance or
// blue/green deployments, move this to a dedicated pre-deployment step instead.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }

    await DbSeeder.SeedAsync(db);

    // Seed a dev user so DevBypassAuthenticationHandler's hardcoded NameIdentifier resolves.
    if (app.Environment.IsDevelopment())
    {
        string devUserId = DevBypassAuthenticationHandler.DevUserId;
        if (!await db.Users.AnyAsync(u => u.Id == devUserId))
        {
            db.Users.Add(new AppUser
            {
                Id = devUserId,
                UserName = "dev-user",
                StravaFirstname = "Dev",
                StravaLastname = "User"
            });
            await db.SaveChangesAsync();
        }
    }
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { } // Expose Program class for integration testing
