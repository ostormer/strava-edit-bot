# strava-edit-bot-ui — Agent Guide

React frontend. See the root `AGENTS.md` for project context and stack overview.

---

## Stack

| | |
|---|---|
| Bundler | Vite 6 |
| Framework | React 19, TypeScript (strict) |
| Styling | Tailwind CSS v4, Neobrutalism components (shadcn-based) |
| HTTP client | Axios with token interceptors (`src/lib/api.ts`) |
| Path alias | `@/` → `src/` |

---

## Project structure

```
src/
  lib/
    api.ts              # Axios instance + Bearer token + refresh interceptor
  components/ui/        # shadcn components (added via CLI, restyled for Neobrutalism)
  App.tsx
  main.tsx
  globals.css           # Tailwind import + Neobrutalism CSS variables + layout CSS
public/
  staticwebapp.config.json  # SPA routing fallback for Azure Static Web Apps
```

---

## Styling

CSS variables are defined in `globals.css` using the Neobrutalism design system:

| Variable | Purpose |
|---|---|
| `--main` | Primary accent (orange) |
| `--background` | Page background |
| `--secondary-background` | Card / surface |
| `--foreground` | Body text |
| `--border` | Always black — the hard neobrutalist border |
| `--shadow` | `4px 4px 0px 0px var(--border)` — offset shadow |

Dark mode uses the `.dark` class (not `prefers-color-scheme` media query).

Tailwind tokens are mapped via `@theme inline` — use utility classes like `bg-background`, `text-foreground`, `bg-main`, `shadow-shadow` in components.

**Adding a component**: `npx shadcn@latest add <component>`, then replace the generated styles with the Neobrutalism variant from neobrutalism.dev.

---

## API client

Import `api` from `@/lib/api` everywhere — do not call `axios` directly.

```ts
import api, { setAccessToken } from '@/lib/api'

// After login — store the access token in module memory:
const { data } = await api.post('/api/auth/login', credentials)
setAccessToken(data.accessToken)

// Authenticated requests — Bearer token is attached automatically:
const { data } = await api.get('/api/activities')
```

The instance handles:
- `baseURL` from `VITE_API_BASE_URL` (empty in dev — Vite proxy rewrites `/api/*` to `localhost:5001`)
- `withCredentials: true` — sends the HttpOnly refresh token cookie on every request
- Request interceptor — attaches `Authorization: Bearer <token>`
- Response interceptor — on 401, calls `/api/auth/refresh`, stores the new token, retries the original request. Concurrent 401s queue up and resolve from a single refresh call.

**Page load**: the access token lives in module memory and is lost on refresh. Call `/api/auth/refresh` on app mount to re-hydrate it from the cookie.

---

## Build modes

| Command | Vite mode | Target |
|---|---|---|
| `npm run dev` | `development` | Local — Vite proxy handles `/api/*` |
| `npm run build:staging` | `staging` | Azure staging environment |
| `npm run build:production` | `production` | Azure production (not yet set up) |

`VITE_API_BASE_URL` is injected by the GitHub Actions pipeline as an environment variable at build time. Do not add real URLs to `.env.staging` or `.env.production` — those files are for non-secret defaults only.
