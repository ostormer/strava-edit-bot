# Infrastructure Setup

Complete guide to go from a fresh Azure account to a working deployment with GitHub
Actions CI/CD. Follow the steps in order — each section is a prerequisite for the next.

Sections 3–5 must be repeated for each environment (dev/prod). Everything else is
done once per subscription.

---

## 1. Azure account and subscription

### Create an account

Sign up at https://portal.azure.com if you don't have one.

### Upgrade to Pay-As-You-Go

Free trial subscriptions have hard quota limits of 0 for most App Service Plan SKUs
and will block all deployments. Upgrade before proceeding:

`Portal → Subscriptions → your subscription → Upgrade`

B1 App Service (~€11/month) and the Azure SQL free offer are the only billable
resources in this project.

### Install the Azure CLI

```bash
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

Bicep is bundled with the Azure CLI — no separate installation needed.

---

## 2. Login and register resource providers

```bash
az login

# List subscriptions and set the correct one
az account list --output table
az account set --subscription "<your-subscription-id>"
```

On a new subscription, resource providers are not registered by default. Deployments
will fail with misleading `SubscriptionIsOverQuotaForSku` errors if this is skipped.
Register all providers used by this project:

```bash
az provider register --namespace Microsoft.Web
az provider register --namespace Microsoft.Sql
az provider register --namespace Microsoft.ManagedIdentity
```

Registration is async. Wait until all three return `Registered` before continuing
(usually 1–2 minutes):

```bash
az provider show --namespace Microsoft.Web --query "registrationState" -o tsv
az provider show --namespace Microsoft.Sql --query "registrationState" -o tsv
az provider show --namespace Microsoft.ManagedIdentity --query "registrationState" -o tsv
```

---

## 3. Create an App Registration

One App Registration per environment — dev and prod tokens must be separate audiences.
Repeat this section for each environment, substituting `dev`/`prod` as appropriate.

```bash
az ad app create --display-name "strava-edit-bot-dev"
```

Get the client ID:

```bash
az ad app list --display-name "strava-edit-bot-dev" --query "[0].appId" -o tsv
```

---

## 4. Set Application ID URI and expose a scope

The API needs an Application ID URI before clients can request tokens for it.

```bash
# Replace <appId> with the client ID from step 3
az ad app update \
  --id <appId> \
  --identifier-uris "api://<appId>"
```

Add the `access_as_user` scope. The `--set` flag fails on fresh app registrations
because the `api` object doesn't exist yet — use `az rest` to PATCH Graph directly:

```bash
OBJECT_ID=$(az ad app show --id <appId> --query id -o tsv)
SCOPE_ID=$(uuidgen)

az rest \
  --method PATCH \
  --uri "https://graph.microsoft.com/v1.0/applications/$OBJECT_ID" \
  --headers "Content-Type=application/json" \
  --body "{
    \"api\": {
      \"oauth2PermissionScopes\": [{
        \"id\": \"$SCOPE_ID\",
        \"value\": \"access_as_user\",
        \"type\": \"User\",
        \"isEnabled\": true,
        \"adminConsentDisplayName\": \"Access Strava Edit Bot API\",
        \"adminConsentDescription\": \"Access the Strava Edit Bot API on behalf of the signed-in user.\",
        \"userConsentDisplayName\": \"Access Strava Edit Bot API\",
        \"userConsentDescription\": \"Access the Strava Edit Bot API on your behalf.\"
      }]
    }
  }"
```

No output means success. Verify:

```bash
az ad app show --id <appId> --query "api.oauth2PermissionScopes" -o json
```

---

## 5. Fill in the param file for this environment

`infra/environments/dev.bicepparam`:
```
param entraAppClientId = '<dev-app-registration-client-id>'
```

---

## 6. Note down values needed for app configuration

These are needed later when configuring `appsettings.json` for `Microsoft.Identity.Web`:

```bash
# Tenant ID (same for all environments)
az account show --query tenantId -o tsv

# Client ID per environment (same value as used in the param file)
az ad app list --display-name "strava-edit-bot-dev" --query "[0].appId" -o tsv
az ad app list --display-name "strava-edit-bot-prod" --query "[0].appId" -o tsv
```

---

## 7. Create a service principal for GitHub Actions

GitHub Actions needs an Azure identity to run Bicep deployments. This uses OIDC
federated credentials — no client secrets are stored anywhere.

```bash
# Create the app registration and service principal
az ad app create --display-name "strava-edit-bot-github-actions"

az ad sp create \
  --id $(az ad app list --display-name "strava-edit-bot-github-actions" --query "[0].appId" -o tsv)
```

Grant Contributor at subscription level so it can create resource groups and all
resources defined in the Bicep:

```bash
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
SP_ID=$(az ad sp list --display-name "strava-edit-bot-github-actions" --query "[0].appId" -o tsv)

az role assignment create \
  --role Contributor \
  --assignee $SP_ID \
  --scope /subscriptions/$SUBSCRIPTION_ID
```

Add federated credentials. Replace `your-github-username/strava-edit-bot` with your
actual GitHub repo path:

```bash
GH_ACTIONS_APP_ID=$(az ad app list --display-name "strava-edit-bot-github-actions" --query "[0].appId" -o tsv)

# Pushes to main branch (triggers dev deployments)
az ad app federated-credential create \
  --id $GH_ACTIONS_APP_ID \
  --parameters '{
    "name": "github-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:your-github-username/strava-edit-bot:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Manual workflow_dispatch (triggers prod deployments)
az ad app federated-credential create \
  --id $GH_ACTIONS_APP_ID \
  --parameters '{
    "name": "github-dispatch",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:your-github-username/strava-edit-bot:environment:prod",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

Print the three values needed as GitHub secrets:

```bash
echo "AZURE_CLIENT_ID:       $GH_ACTIONS_APP_ID"
echo "AZURE_TENANT_ID:       $(az account show --query tenantId -o tsv)"
echo "AZURE_SUBSCRIPTION_ID: $SUBSCRIPTION_ID"
```

Add them in GitHub: `Settings → Secrets and variables → Actions → New repository secret`

---

## 8. First deploy (manual, from local machine)

Run once to create the Azure resources before GitHub Actions takes over:

```bash
# Dev
az deployment sub create \
  --location norwayeast \
  --template-file infra/main.bicep \
  --parameters infra/environments/dev.bicepparam

# Prod (when ready — not needed until prod environment is set up)
az deployment sub create \
  --location norwayeast \
  --template-file infra/main.bicep \
  --parameters infra/environments/prod.bicepparam
```

The `--location` flag is only where Azure stores the deployment metadata record —
actual resources go to the `location` value set inside the `.bicepparam` file.

A successful deployment prints the App Service hostname:
```
"appHostname": "strava-edit-bot-dev.azurewebsites.net"
```

---

## Troubleshooting

### Quota errors (`SubscriptionIsOverQuotaForSku`)

Even on Pay-As-You-Go, App Service Plan SKUs can have a default quota of 0 in
some regions. Check which regions actually support your SKU:

```bash
az appservice list-locations --sku B1 --linux-workers -o tsv
```

To check the current quota and request an increase:

```bash
az extension add --name quota
az provider register --namespace Microsoft.Quota
# Wait for: az provider show --namespace Microsoft.Quota --query "registrationState" -o tsv

# List quota resource names for a region
az quota list \
  --scope "/subscriptions/<subscription-id>/providers/Microsoft.Web/locations/<region>" \
  -o table

# Request an increase (resource-name is the SKU name, e.g. "B1")
az quota create \
  --scope "/subscriptions/<subscription-id>/providers/Microsoft.Web/locations/<region>" \
  --resource-name "B1" \
  --limit-object value=1
```

If `az quota create` returns `QuotaNotAvailableForResource`, validate the deployment
against alternative regions to find one that works before updating the param file:

```bash
az deployment sub validate \
  --location norwayeast \
  --template-file infra/main.bicep \
  --parameters infra/environments/dev.bicepparam \
  --parameters location=<region-to-test>
```

No output = validation passed.

### Stale deployment records

A failed deployment leaves a subscription-level record that blocks reruns against a
different location. Clean up before retrying:

```bash
# List records and their state
az deployment sub list --query "[].{name:name, state:properties.provisioningState}" -o table

# Delete by name (default name is 'main')
az deployment sub delete --name main
```

### Partially created resource groups

If a deployment fails mid-way, delete the resource group before retrying:

```bash
az group delete --name strava-edit-bot-dev-rg --yes
```
