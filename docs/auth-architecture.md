# Auth & User Architecture

## Recommendation: ASP.NET Core Identity + JWT

The app uses **ASP.NET Core Identity** for user accounts (email/password) and **JWT bearer tokens** for API authentication. This is the idiomatic .NET approach — it integrates directly with EF Core, handles password hashing and account management out of the box, and keeps everything self-hosted with no external identity service costs.

The existing `Microsoft.Identity.Web` (Entra ID) setup is replaced for end-user auth. Entra ID B2C and services like Auth0 add cost and complexity without meaningful benefit at this scale.

Strava is connected as a **linked OAuth account** after the user has already registered — it is not the primary identity provider.

---

## System Overview

```mermaid
graph TB
    subgraph Browser
        FE[React Frontend]
        MEM["In-memory (access token JWT)"]
        COOKIE["httpOnly Cookie (refresh token)"]
    end

    subgraph "Azure App Service — ASP.NET Core API"
        AUTH["/api/auth (register, login, refresh, logout)"]
        ACTIVITIES_API["/api/activities"]
        STRAVA_OAUTH["/api/strava (connect, callback)"]
        WEBHOOK["/api/strava/webhook (Strava events)"]
    end

    subgraph "Azure SQL"
        USERS[(AspNetUsers - ASP.NET Identity)]
        REFRESH[(RefreshTokens)]
        STRAVA_TOK[(StravaTokens - encrypted)]
        ACT[(Activities)]
    end

    STRAVA_EXT[Strava API]

    FE -- "Authorization: Bearer JWT" --> ACTIVITIES_API
    FE -- "httpOnly cookie auto-sent" --> AUTH
    AUTH --> USERS
    AUTH --> REFRESH
    ACTIVITIES_API --> ACT
    STRAVA_OAUTH -- "store encrypted tokens" --> STRAVA_TOK
    STRAVA_EXT -- "POST webhook event" --> WEBHOOK
    WEBHOOK -- "look up user by StravaAthleteId" --> STRAVA_TOK
    WEBHOOK -- "write activity" --> ACT
```

---

## Flow 1: Registration & Login

```mermaid
sequenceDiagram
    participant FE as React Frontend
    participant API as ASP.NET Core API
    participant DB as SQL Server

    Note over FE,DB: Registration
    FE->>API: POST /api/auth/register {email, password}
    API->>DB: Create user via Identity (hashes password)
    API->>DB: Store refresh token (hashed)
    API-->>FE: 200 {accessToken} + Set-Cookie refreshToken (HttpOnly Secure)

    Note over FE,DB: Login
    FE->>API: POST /api/auth/login {email, password}
    API->>DB: Validate credentials via Identity
    API->>DB: Store new refresh token, invalidate old
    API-->>FE: 200 {accessToken} + Set-Cookie refreshToken (HttpOnly)
    FE->>FE: Store access token in memory (not localStorage)
```

The refresh token is delivered via an **httpOnly cookie** — JavaScript cannot read it, so XSS attacks cannot steal it. The access token lives only in memory and is gone on page refresh, which is an acceptable UX tradeoff for the security gain.

---

## Flow 2: JWT Token Lifecycle

Access tokens are short-lived (15 minutes). The frontend silently refreshes before expiry.

```mermaid
sequenceDiagram
    participant FE as React Frontend
    participant API as ASP.NET Core API
    participant DB as SQL Server

    Note over FE,API: Normal authenticated request
    FE->>API: GET /api/activities (Authorization: Bearer token)
    API->>API: Validate JWT signature + expiry
    API-->>FE: 200 OK

    Note over FE,API: Access token expired — silent refresh
    FE->>API: POST /api/auth/refresh (httpOnly cookie sent automatically)
    API->>DB: Look up refresh token, verify not used/expired
    API->>DB: Invalidate old refresh token, store new one (rotation)
    API-->>FE: 200 {accessToken} + Set-Cookie refreshToken rotated (HttpOnly)
    FE->>FE: Replace in-memory access token

    Note over FE,API: Logout
    FE->>API: POST /api/auth/logout (cookie sent automatically)
    API->>DB: Delete refresh token from DB
    API-->>FE: 200 + Set-Cookie refreshToken cleared
    FE->>FE: Clear in-memory access token
```

**Refresh token rotation**: every refresh issues a new token and invalidates the old one. If a stolen token is used twice, the second use detects reuse and invalidates the entire token family.

---

## Flow 3: Strava Account Linking

This happens after the user is already logged in.

```mermaid
sequenceDiagram
    participant FE as React Frontend
    participant API as ASP.NET Core API
    participant STRAVA as Strava OAuth

    FE->>API: GET /api/strava/connect (Authorization: Bearer token)
    API->>API: Store CSRF state param tied to userId
    API-->>FE: 302 Redirect to Strava authorize URL (scope: activity:read_all)

    FE->>STRAVA: User approves in Strava UI
    STRAVA->>API: GET /api/strava/callback?code=xxx&state=yyy
    API->>API: Validate state param (prevents CSRF)
    API->>STRAVA: POST /oauth/token {code, client_id, client_secret, grant_type}
    STRAVA-->>API: {access_token, refresh_token, expires_at, athlete.id}
    API->>DB: Upsert StravaToken for userId (tokens encrypted at rest)
    API-->>FE: Redirect to frontend /settings?strava=connected
```

Scope `activity:read_all` is required to receive private activity events through the webhook.

**Strava token details:**
- Access tokens expire after **6 hours**
- Refresh tokens are **rotated** — each refresh response may return a new refresh token; always persist the latest one
- Check `expires_at` before every Strava API call and refresh proactively if needed

---

## Flow 4: Webhook → Activity Ingestion

Strava uses a single webhook subscription per app — one event fires for every athlete who has connected their account.

```mermaid
sequenceDiagram
    participant STRAVA as Strava
    participant API as ASP.NET Core API
    participant DB as SQL Server

    Note over STRAVA,API: One-time subscription setup (you call this once)
    API->>STRAVA: POST /push_subscriptions {callback_url, verify_token}
    STRAVA->>API: GET /api/strava/webhook?hub.challenge=xxx&hub.verify_token=yyy
    API-->>STRAVA: 200 {"hub.challenge": "xxx"} within 2 seconds
    STRAVA-->>API: 200 {id: subscription_id}

    Note over STRAVA,API: Activity created event
    STRAVA->>API: POST /api/strava/webhook {object_type, aspect_type: create, object_id, owner_id}
    API-->>STRAVA: 200 OK immediately, before any work
    API->>DB: Look up StravaToken by StravaAthleteId
    API->>API: Refresh Strava access token if expires_at < now
    API->>STRAVA: GET /activities/{id} (Authorization: Bearer strava_token)
    STRAVA-->>API: Full activity payload
    API->>DB: Upsert Activity for userId
```

**Critical**: return `200 OK` to Strava immediately, then do the fetch-and-store work asynchronously. Strava retries up to 3 times if no response within 2 seconds. Use a background queue (e.g. `IHostedService` + `Channel<T>`) for the async work.

---

## Data Model

```
AspNetUsers                         ← managed by ASP.NET Identity
├── Id              GUID
├── Email           NVARCHAR(256)
├── PasswordHash    NVARCHAR(MAX)
└── ...             (standard Identity columns)

RefreshTokens
├── Id              GUID
├── UserId          FK → AspNetUsers.Id
├── TokenHash       NVARCHAR(256)   ← store hash, never plaintext
├── ExpiresAt       DATETIME2
├── CreatedAt       DATETIME2
└── RevokedAt       DATETIME2?      ← null = still valid

StravaTokens
├── UserId          FK → AspNetUsers.Id   (unique — one Strava account per user)
├── StravaAthleteId BIGINT               ← used to match incoming webhook events
├── AccessToken     NVARCHAR(MAX)        ← encrypted via Data Protection API
├── RefreshToken    NVARCHAR(MAX)        ← encrypted via Data Protection API
└── ExpiresAt       DATETIME2

Activities
├── Id              INT IDENTITY
├── UserId          FK → AspNetUsers.Id  ← add this (currently missing)
├── StravaActivityId BIGINT?             ← link back to source activity on Strava
└── ... (existing columns)
```

---

## Changes to Existing Codebase

| Area | Change |
|---|---|
| `Program.cs` | Replace Entra ID auth with JWT bearer; keep `DevBypass` for local dev |
| DB schema | Add `AspNetUsers`, `RefreshTokens`, `StravaTokens`; add `UserId` FK to `Activities` |
| New controllers | `AuthController` (register/login/refresh/logout), `StravaController` (connect/callback/webhook) |
| New services | `TokenService` (issue/validate JWTs), `StravaTokenService` (store/refresh Strava tokens) |
| `ActivityService` | Scope all queries to `UserId` — users must only see their own activities |
| Config | Add `Jwt:Secret`, `Jwt:Issuer`, `Strava:ClientId`, `Strava:ClientSecret`, `Strava:WebhookVerifyToken` to user secrets |

---

## Security Checklist

- **Never store JWT secret in appsettings** — use User Secrets locally, Key Vault in Azure
- **Encrypt Strava tokens at rest** — use ASP.NET Data Protection API on those columns
- **Validate CSRF state param** in the Strava OAuth callback
- **Scope `Activities` queries to the authenticated user** — never return another user's data
- **Refresh token rotation + reuse detection** — invalidate the entire token family on reuse
- **Webhook verify token** — reject webhook requests that don't present the expected verify token
