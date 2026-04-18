# Bruno API Collection

[Bruno](https://www.usebruno.com/) collection for manually testing the StravaEditBot API.

## Quick start

1. Install [Bruno](https://www.usebruno.com/) (desktop app or CLI)
2. Open this folder as a collection in Bruno
3. Select the **local** environment
4. Start the API:
   ```bash
   docker compose up -d
   dotnet run --project StravaEditBotApi
   ```
5. Send any request — no auth setup needed

## Local development

In Development mode, every request is automatically authenticated as a seeded dev user (`Dev User`). No tokens, no OAuth, no setup. Just send requests.

This works because:
- `DevBypassAuthenticationHandler` injects a `NameIdentifier` claim on every request
- A matching `AppUser` is created on startup with ID `dev-user-00000000-0000-0000-0000-000000000000`
- All user-scoped endpoints (rulesets, variables, runs) resolve to this user

The `accessToken` variable in the local environment can be left empty — the Bearer auth header in request files is ignored when the dev handler is active.

## Testing against deployed environments

For the **dev** environment (or any non-Development deployment), you need a real JWT:

1. Complete the Strava OAuth flow to get an authorization code
2. Send **auth/Strava Callback** with that code
3. Copy `accessToken` from the response
4. Paste into the `accessToken` secret variable in Bruno's environment editor (lock icon)

All authenticated requests use `Bearer {{accessToken}}` automatically. Refresh tokens are handled via HttpOnly cookies — Bruno manages these.

## Environments

| Environment | Base URL | Auth |
|---|---|---|
| **local** | `http://localhost:5247` | Auto-authenticated (no token needed) |
| **dev** | `https://strava-edit-bot-dev.azurewebsites.net` | Requires real JWT |

## Variables

| Variable | Used by | Notes |
|---|---|---|
| `baseUrl` | All requests | Pre-configured per environment |
| `accessToken` | Auth-required endpoints | Empty for local, set manually for deployed |
| `activityId` | Webhook events | Test Strava activity ID |
| `stravaAthleteId` | Webhook events | Test Strava athlete ID |
| `webhookVerifyToken` | Webhook handshake | Secret — set via lock icon in Bruno |

## Endpoints

### Public (no auth)

| Folder | Request | Route |
|---|---|---|
| auth | Strava Callback | `POST /api/auth/strava/callback` |
| auth | Refresh Token | `POST /api/auth/refresh` |
| auth | Logout | `POST /api/auth/logout` |
| templates | Get Public Templates | `GET /api/templates` |
| templates | Get by Share Token | `GET /api/templates/shared/{token}` |
| webhook | Validate Handshake | `GET /api/webhook` |
| webhook | Activity events | `POST /api/webhook` |

### User-scoped (auto-authenticated locally)

| Folder | Request | Route |
|---|---|---|
| users | Get Current User | `GET /api/users/me` |
| rulesets | Get All | `GET /api/rulesets` |
| rulesets | Get by ID | `GET /api/rulesets/{id}` |
| rulesets | Create | `POST /api/rulesets` |
| rulesets | Update | `PUT /api/rulesets/{id}` |
| rulesets | Delete | `DELETE /api/rulesets/{id}` |
| rulesets | Reorder | `PUT /api/rulesets/reorder` |
| rulesets | Toggle Enabled | `PATCH /api/rulesets/{id}/toggle` |
| rulesets | Share | `POST /api/rulesets/{id}/share` |
| rulesets | Validate | `POST /api/rulesets/validate` |
| templates | Instantiate | `POST /api/templates/{id}/use` |
| variables | Get All | `GET /api/variables` |
| variables | Get by ID | `GET /api/variables/{id}` |
| variables | Create | `POST /api/variables` |
| variables | Update | `PUT /api/variables/{id}` |
| variables | Delete | `DELETE /api/variables/{id}` |
| runs | Get All | `GET /api/runs?page=1&pageSize=50` |
| runs | Get by ID | `GET /api/runs/{id}` |

## Collection structure

```
bruno/
├── auth/           Strava OAuth + token management
├── users/          Current user profile
├── rulesets/       Ruleset CRUD, reorder, toggle, share, validate
├── templates/      Template marketplace + instantiation
├── runs/           Ruleset run history
├── variables/      Custom variable CRUD
├── webhook/        Strava webhook simulation
└── environments/   local + dev configs
```
