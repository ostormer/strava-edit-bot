# Infrastructure — Agent Guide

Azure infrastructure as Bicep. For the full setup walkthrough (Azure account, App Registration, service principal, GitHub Actions), see `SETUP.md`.

---

## Module structure

```
main.bicep              # subscription-scoped entry point
environments/
  dev.bicepparam        # dev parameter values
modules/
  identity.bicep        # user-assigned managed identity
  sql.bicep             # Azure SQL Server + database
  appservice.bicep      # App Service Plan (B1 Linux) + Web App (.NET 10)
  staticwebapp.bicep    # Azure Static Web Apps (free tier) — React frontend
```

---

## Naming convention

All resources use the prefix `${appName}-${env}` (e.g. `strava-edit-bot-dev`):

| Resource | Name |
|---|---|
| Resource group | `strava-edit-bot-dev-rg` |
| App Service | `strava-edit-bot-dev` |
| Static Web App | `strava-edit-bot-dev-ui` |
| Azure SQL Server | `strava-edit-bot-dev-sql` |
| Managed identity | `strava-edit-bot-dev-identity` |

---

## Key wiring decisions

**CORS**: `main.bicep` reads `staticwebapp.outputs.defaultHostname` and passes `'https://${staticwebapp.outputs.defaultHostname}'` to `appservice` as `corsAllowedOrigins`. Bicep resolves the deployment order automatically — no manual CORS step after deploy.

**Managed identity**: the App Service authenticates to Azure SQL via a user-assigned managed identity — no passwords in connection strings. `AZURE_CLIENT_ID` on the App Service tells `DefaultAzureCredential` which identity to use.

**App secrets**: Secrets are **never** set manually in Azure Portal. They live in GitHub Environment secrets (Settings → Environments → `<env>` → Secrets) and get pushed to App Service app settings on every deploy via `az webapp config appsettings set` in the `deploy-app` job. Current secrets:

| GitHub Secret | App Setting | Purpose |
|---|---|---|
| `JWT_SECRET` | `Jwt__Secret` | JWT signing key |
| `JWT_ISSUER` | `Jwt__Issuer` | Token issuer |
| `JWT_AUDIENCE` | `Jwt__Audience` | Token audience |
| `STRAVA_CLIENT_SECRET` | `Strava__ClientSecret` | Strava API client secret |
| `STRAVA_WEBHOOK_VERIFY_TOKEN` | `Strava__WebhookVerifyToken` | Webhook handshake verify token |

To add a new secret: add it to GitHub Environment secrets, then add the `az webapp config appsettings set` line in `deploy.yml`.

**SWA region**: Azure Static Web Apps do not support `norwayeast`. The `swaLocation` parameter defaults to `westeurope`. Content is served globally via CDN regardless of where the resource lives.

**`main.json`**: generated ARM template output of `az bicep build`. Gitignored — do not edit.

---

## Key commands

```bash
# Deploy or update an environment
az deployment sub create \
  --location norwayeast \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/environments/dev.bicepparam

# Validate without deploying
az deployment sub validate \
  --location norwayeast \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/environments/dev.bicepparam

# Lint / compile Bicep
az bicep build --file infrastructure/main.bicep
```
