using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StravaEditBotApi.Data;
using StravaEditBotApi.Middleware;
using StravaEditBotApi.Models;
using StravaEditBotApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Registers UserManager<AppUser>, password hasher, validators, and EF stores.
// AddIdentityCore (not AddIdentity) avoids setting cookie auth as the default scheme,
// which would conflict with JWT bearer on an API.
builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.User.RequireUniqueEmail = true;
})
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
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<ITokenService, TokenService>();

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
