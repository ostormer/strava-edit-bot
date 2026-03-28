// User-assigned managed identity.
//
// Think of this as a service account for the App Service. Instead of storing
// database passwords or API keys, Azure services can grant permissions directly
// to this identity. The App Service presents it automatically — no credentials
// in config.

param name string
param location string

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: name
  location: location
}

output identityResourceId string = identity.id
output principalId string = identity.properties.principalId  // used to grant permissions
output clientId string = identity.properties.clientId        // used by app code to select this identity
