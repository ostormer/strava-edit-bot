# strava-edit-bot
Create rules to automatically rename and/or edit your completed Strava activities.

---

## Local development setup

### Prerequisites

- .NET 10 SDK
- Node.js 20+
- Docker (for SQL Server)

### 1. Start the database

```bash
docker compose up -d
```

### 2. Configure API secrets

The API uses [.NET user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) to keep credentials out of source control.

```bash
# JWT signing key — any long random string works locally
dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 64)" --project StravaEditBotApi

# Strava OAuth app credentials — create an app at https://www.strava.com/settings/api
dotnet user-secrets set "Strava:ClientId" "<your-strava-client-id>" --project StravaEditBotApi
dotnet user-secrets set "Strava:ClientSecret" "<your-strava-client-secret>" --project StravaEditBotApi
```

In your Strava app settings, add `http://localhost:5173/auth/callback` as an authorized redirect URI.

### 3. Configure frontend environment

Edit `strava-edit-bot-ui/.env.development` and set your Strava client ID (public — safe to put here):

```
VITE_STRAVA_CLIENT_ID=<your-strava-client-id>
```

### 4. Run the API

```bash
dotnet run --project StravaEditBotApi
# Runs on http://localhost:5247
# Swagger UI: http://localhost:5247/swagger
```

### 5. Run the frontend

```bash
cd strava-edit-bot-ui
npm install
npm run dev
# Runs on http://localhost:5173
```

Navigate to `http://localhost:5173/login` and click **Connect with Strava** to sign in.

---

## Running tests

```bash
dotnet test StravaEditBotApi.Tests/StravaEditBotApi.Tests.csproj
```
