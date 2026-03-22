// App Service Plan (B1 Basic tier) + Web App.
// B1 is the cheapest paid tier (~€11/month). F1 (free) has a quota of 0 on
// free/trial Azure subscriptions and a 60 min/day compute cap anyway.
//
// The web app is assigned the managed identity, which is how it authenticates
// to Azure SQL without a password. Two key environment variables make this work:
//
//   AZURE_CLIENT_ID  — tells DefaultAzureCredential which user-assigned identity
//                      to use when requesting tokens (important if there's ever
//                      more than one identity on the app)
//
//   AzureAd__*       — double-underscore is ASP.NET Core's env var hierarchy
//                      separator, equivalent to AzureAd:TenantId in appsettings.json

param planName string
param appName string
param location string
param identityResourceId string   // resource ID of the managed identity
param identityClientId string     // client ID of the managed identity
param sqlServerFqdn string
param databaseName string
param entraAppClientId string     // App Registration client ID (for JWT validation)

var tenantId = tenant().tenantId

// Connection string uses Active Directory Default — no password.
// DefaultAzureCredential (via Azure.Identity) handles token acquisition automatically.
var connectionString = 'Server=tcp:${sqlServerFqdn},1433;Database=${databaseName};Authentication=Active Directory Default;Encrypt=True;'

// ── App Service Plan ──────────────────────────────────────────────────────────

resource plan 'Microsoft.Web/serverfarms@2025-03-01' = {
  name: planName
  location: location
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: true // required for Linux plans
  }
}

// ── Web App ───────────────────────────────────────────────────────────────────

resource app 'Microsoft.Web/sites@2025-03-01' = {
  name: appName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityResourceId}': {}  // attach the managed identity to this app
    }
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'

      // App settings = environment variables inside the app.
      // ASP.NET Core maps AzureAd__TenantId → AzureAd:TenantId in IConfiguration.
      appSettings: [
        {
          name: 'AZURE_CLIENT_ID'
          value: identityClientId
        }
        {
          name: 'AzureAd__TenantId'
          value: tenantId
        }
        {
          name: 'AzureAd__ClientId'
          value: entraAppClientId
        }
      ]

      // Connection strings show up under Configuration > Connection strings in the
      // portal, and override any matching key in appsettings.json at runtime.
      connectionStrings: [
        {
          name: 'DefaultConnection'
          connectionString: connectionString
          type: 'SQLAzure'
        }
      ]
    }
  }
}

output hostname string = app.properties.defaultHostName
