# StravaEditBot — Agent Guide

Bot for bulk-editing Strava activity names, descriptions, metadata via web UI. Learning project — dev has strong Python web background; use Python analogies for .NET concepts.

---

## Stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core Web API (.NET 10) |
| Database | Azure SQL / SQL Server, EF Core 10 |
| Auth | ASP.NET Core Identity + JWT bearer + HttpOnly refresh token cookie |
| Validation | FluentValidation 12 |
| Frontend | React 19, Vite, TypeScript, Tailwind v4, Neobrutalism (shadcn-based) |
| Hosting | Azure App Service (B1 Linux) + Azure Static Web Apps (free tier) |
| IaC | Bicep, deployed via GitHub Actions with OIDC |

---

## Repo structure

```
StravaEditBotApi/          # ASP.NET Core Web API       → StravaEditBotApi/CLAUDE.md
StravaEditBotApi.Tests/    # NUnit test suite            → StravaEditBotApi.Tests/CLAUDE.md
StravaAPILibrary/          # Strava API client library   → StravaAPILibrary/CLAUDE.md
strava-edit-bot-ui/        # React frontend              → strava-edit-bot-ui/CLAUDE.md
infrastructure/            # Bicep IaC                   → infrastructure/CLAUDE.md
bruno/                     # Bruno API collection for manual testing
docs/                      # Architecture docs
```

### Docs

| File | Contents |
|---|---|
| [docs/auth.md](docs/auth.md) | Auth design: token model, OAuth flow, JWT + refresh token lifecycle, swimlane diagrams |
| [docs/webhook.md](docs/webhook.md) | Webhook pipeline: Strava subscription setup, event flow, background channel architecture |
| [docs/data-model.md](docs/data-model.md) | DB entities for ruleset engine: `Ruleset`, `RulesetTemplate`, `RulesetRun`, `CustomVariable`, EF config, seeding |
| [docs/filter-effect-types.md](docs/filter-effect-types.md) | C# POCO types for JSON-serialized columns: `FilterExpression` (polymorphic tree), `RulesetEffect`, `CustomVariableDefinition` |
| [docs/implementation-plan.md](docs/implementation-plan.md) | Phased build plan for ruleset engine (Phase 0–5): scope, task breakdown, dependency graph, API route summary |

---

## Known issues

1. Strava access/refresh tokens stored plaintext — needs encryption at rest (see Phase 5.2)
2. `RulesetRun.RulesetId` FK is `NO ACTION` (not `SET NULL`) — SQL Server cascade path restriction — `RulesetService.DeleteAsync` nullifies run refs explicitly via `ExecuteUpdateAsync` before deleting ruleset