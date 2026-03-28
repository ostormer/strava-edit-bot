// Azure SQL Server + Database.
//
// SQL auth (username/password) is disabled entirely — Entra ID only.
// The managed identity is set as the Entra admin, so the App Service can
// connect using "Authentication=Active Directory Default" in the connection
// string with no password.
//
// Database uses the Azure SQL free offer: 32 GB, 100k vCore-seconds/month.
// When the free limit is hit it auto-pauses rather than charging you.

param serverName string
param databaseName string
param location string
param managedIdentityName string       // display name shown in SQL as the admin login
param managedIdentityPrincipalId string // object ID of the managed identity in Entra ID

var tenantId = tenant().tenantId

// ── SQL Server ────────────────────────────────────────────────────────────────

resource sqlServer 'Microsoft.Sql/servers@2023-08-01' = {
  name: serverName
  location: location
  properties: {
    // Entra-only auth — no SA password, no SQL logins.
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: managedIdentityName
      sid: managedIdentityPrincipalId
      tenantId: tenantId
    }
  }
}

// Allow Azure-internal traffic to reach the server (required for App Service).
// The 0.0.0.0 → 0.0.0.0 range is the Azure magic value for "allow Azure services".
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ── Database ──────────────────────────────────────────────────────────────────

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    // GP_S_Gen5 = General Purpose Serverless (scales to zero when idle).
    // Required SKU for the free offer.
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    useFreeLimit: true                          // opt in to the free monthly allowance
    freeLimitExhaustionBehavior: 'AutoPause'    // pause instead of charging when limit hit
    autoPauseDelay: 60                          // pause after 60 minutes of inactivity
    minCapacity: json('0.5')                    // scale down to 0.5 vCores when idle (decimal, not int)
    requestedBackupStorageRedundancy: 'Local'   // cheapest backup option (single region)
  }
}

output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
