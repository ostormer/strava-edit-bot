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

**App secrets**: `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience` are not in Bicep. GitHub Actions pushes them to App Service app settings on every deploy from GitHub Environment secrets.

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
