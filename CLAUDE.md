# StravaEditBot — Agent Guide

A bot that lets users bulk-edit their Strava activity names, descriptions, and metadata through a web interface. Learning project — the developer has a strong Python web dev background; Python analogies are helpful when explaining .NET concepts.

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
StravaEditBotApi.Tests/    # NUnit test suite            → StravaEditBotApi.Tests/AGENTS.md
StravaAPILibrary/          # Strava API client library   → StravaAPILibrary/CLAUDE.md
strava-edit-bot-ui/        # React frontend              → strava-edit-bot-ui/AGENTS.md
infrastructure/            # Bicep IaC                   → infrastructure/AGENTS.md
bruno/                     # Bruno API collection for manual testing
docs/                      # Architecture docs
  auth.md                  # Auth design, token model, swimlane diagrams → docs/auth.md
```

---

## Known issues

1. Controllers return the `Activity` entity directly — should use response DTOs
2. No pagination on `GetAllAsync`
3. Activities not yet scoped to `UserId` — all users see all activities
