// Entry point for all infrastructure.
// Subscription-scoped so Bicep manages the resource group too.
//
// Deploy with:
//   az deployment sub create --location norwayeast --template-file infrastructure/main.bicep --parameters infrastructure/environments/dev.bicepparam
//
// Note: --location is only where Azure stores deployment metadata; actual resources go to the location in the .bicepparam file.

targetScope = 'subscription'

// ── Parameters ────────────────────────────────────────────────────────────────

@description('Short name of the application, e.g. "strava-edit-bot".')
param appName string

@description('Environment name, e.g. "dev" or "prod". Included in all resource names.')
@allowed(['dev', 'prod'])
param env string

@description('Azure region for all resources.')
param location string

@description('Client ID of the Entra ID App Registration for this environment.')
param entraAppClientId string

@description('Azure region for the Static Web App resource. Must be a supported SWA region — norwayeast is not supported. Content is served globally via CDN regardless of this value.')
param swaLocation string = 'westeurope'

// ── Resource group ────────────────────────────────────────────────────────────

resource rg 'Microsoft.Resources/resourceGroups@2025-04-01' = {
  name: '${appName}-${env}-rg'
  location: location
}

// ── Variables (resource names) ────────────────────────────────────────────────

var prefix        = '${appName}-${env}'
var identityName  = '${prefix}-identity'
var planName      = '${prefix}-plan'
var webAppName    = prefix
var sqlServerName = '${prefix}-sql'   // must be globally unique in Azure
var databaseName  = '${prefix}-db'
var swaName       = '${prefix}-ui'

// ── Modules ───────────────────────────────────────────────────────────────────

module identity 'modules/identity.bicep' = {
  name: 'deploy-identity'
  scope: rg
  params: {
    name: identityName
    location: location
  }
}

module sql 'modules/sql.bicep' = {
  name: 'deploy-sql'
  scope: rg
  params: {
    serverName: sqlServerName
    databaseName: databaseName
    location: location
    managedIdentityName: identityName
    managedIdentityPrincipalId: identity.outputs.principalId
  }
}

module staticwebapp 'modules/staticwebapp.bicep' = {
  name: 'deploy-staticwebapp'
  scope: rg
  params: {
    name: swaName
    location: swaLocation
  }
}

module appservice 'modules/appservice.bicep' = {
  name: 'deploy-appservice'
  scope: rg
  params: {
    planName: planName
    appName: webAppName
    location: location
    identityResourceId: identity.outputs.identityResourceId
    identityClientId: identity.outputs.clientId
    sqlServerFqdn: sql.outputs.sqlServerFqdn
    databaseName: databaseName
    entraAppClientId: entraAppClientId
    corsAllowedOrigins: 'https://${staticwebapp.outputs.defaultHostname}'
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

output resourceGroup string = rg.name
output appHostname string = appservice.outputs.hostname
output uiHostname string = staticwebapp.outputs.defaultHostname
