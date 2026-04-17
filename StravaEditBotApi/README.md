# StravaEditBot API

ASP.NET Core Web API that powers StravaEditBot — a tool for automatically renaming, tagging, and editing Strava activities based on user-defined rules.

## What it does

When you record a Strava activity, the bot receives a webhook event, evaluates your rulesets against the activity, and applies matching effects (rename, set description, mark as commute, etc.). Think of it as IFTTT for Strava activities.

### Core concepts

- **Ruleset** — A user-owned rule: "if _filter_ matches, apply _effect_". Each user can have multiple rulesets evaluated in priority order.
- **Filter** — A composable expression tree (`and`, `or`, `not`, `check`) that matches activity properties like sport type, time of day, distance, location, etc.
- **Effect** — What to change on a matching activity: name, description, sport type, gear, commute flag. String fields support `{variable}` interpolation (e.g. `"Morning run — {distance_km}km"`).
- **Template** — A shareable ruleset definition. Browse public templates or share your own via link. PII (locations, gear IDs) is automatically sanitized when sharing.
- **Custom variable** — User-defined variables with conditional logic (e.g. `{pace_label}` = "Easy" if pace > 6:00/km, else "Fast"). Referenced in effect templates.

## API routes

| Route | Description |
|---|---|
| `POST /api/auth/strava/callback` | Strava OAuth callback — creates user + issues JWT |
| `POST /api/auth/refresh` | Rotate refresh token, issue new JWT |
| `POST /api/auth/logout` | Revoke refresh token |
| `GET /api/users/me` | Current user profile |
| `GET/POST/PUT/DELETE /api/rulesets` | Ruleset CRUD |
| `PUT /api/rulesets/reorder` | Reorder ruleset priorities |
| `PATCH /api/rulesets/{id}/toggle` | Toggle ruleset enabled/disabled |
| `POST /api/rulesets/{id}/share` | Create a shareable template from a ruleset |
| `POST /api/rulesets/validate` | Validate filter + effect without saving |
| `GET /api/templates` | Browse public templates |
| `GET /api/templates/shared/{token}` | Get template by share link |
| `POST /api/templates/{id}/use` | Instantiate a template as a new ruleset |
| `GET/POST/PUT/DELETE /api/variables` | Custom variable CRUD |
| `GET /api/runs` | Paginated history of ruleset evaluations |
| `GET /api/webhook` | Strava webhook verification handshake |
| `POST /api/webhook` | Receive Strava webhook events |

## Project structure

```
Controllers/          7 REST controllers, one per resource
Services/
  Auth/               JWT minting, Strava OAuth token exchange
  Webhook/            Webhook event processing + background worker
  Rulesets/           Ruleset CRUD, templates, validation, sanitization, variables
DTOs/
  Auth/               Login/token response DTOs
  Webhook/            Strava event DTOs
  Rulesets/           Ruleset request/response DTOs
  Templates/          Template + share response DTOs
  Runs/               Run history DTOs
  Variables/          Custom variable DTOs
Models/               EF Core entities (AppUser, Ruleset, RulesetTemplate, etc.)
  Rules/              JSON-serialized types (FilterExpression, RulesetEffect, etc.)
Validators/           FluentValidation validators for DTOs
Middleware/           Global exception handler, dev auth bypass
Data/                 AppDbContext + DbSeeder
Migrations/           EF Core migrations (auto-generated)
```

## Running locally

```bash
# Start SQL Server
docker compose up -d

# Run the API (http://localhost:5247)
dotnet run --project StravaEditBotApi

# Run tests
dotnet test
```

In Development mode, all requests are auto-authenticated — no Strava OAuth needed.

## Stack

- .NET 10, ASP.NET Core Web API
- EF Core 10 + Azure SQL / SQL Server
- ASP.NET Core Identity + JWT bearer + HttpOnly refresh token cookie
- FluentValidation 12
- NUnit + NSubstitute (tests)
