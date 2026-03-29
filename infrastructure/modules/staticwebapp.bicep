// Azure Static Web Apps — hosts the React frontend.
//
// Static Web Apps has a Free tier with a global CDN and built-in CI/CD support.
// The resource itself must be in one of the supported SWA regions (not all Azure
// regions are available). Content is served globally regardless of this location.
//
// Deployment workflow:
//   1. After first deploy, retrieve the token:
//        az staticwebapp secrets list --name <name> --query "properties.apiKey" -o tsv
//   2. Add the token as a GitHub Actions secret (AZURE_STATIC_WEB_APPS_API_TOKEN).
//   3. GitHub Actions workflow (in .github/workflows/) picks it up on push.
//
// The frontend calls the API on its App Service hostname directly.
// Ensure the API has CORS configured to allow this app's defaultHostname.

param name string
param location string

resource swa 'Microsoft.Web/staticSites@2024-04-01' = {
  name: name
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    buildProperties: {
      skipGithubActionWorkflowGeneration: true  // we manage the workflow ourselves
    }
  }
}

output defaultHostname string = swa.properties.defaultHostname
output id string = swa.id
output name string = swa.name
