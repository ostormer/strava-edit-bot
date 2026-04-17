import axios, { type InternalAxiosRequestConfig } from 'axios'

// Access token lives in module memory — not localStorage or sessionStorage.
// This avoids XSS exposure: JS on the page can call setAccessToken, but a
// script injected by an attacker can't read it out of browser storage.
// The downside: it's lost on page refresh, so call /api/auth/refresh on app
// load to re-hydrate it from the HttpOnly cookie.
let accessToken: string | null = null

export const setAccessToken = (token: string | null) => { accessToken = token }
export const getAccessToken = () => accessToken

// ── Axios instance ─────────────────────────────────────────────────────────────

const api = axios.create({
  // In dev this is empty — the Vite proxy handles routing /api/* to localhost:5001.
  // In production it's the App Service base URL set via VITE_API_BASE_URL.
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '',

  // Send the HttpOnly refresh token cookie on every request, including cross-origin.
  // The browser won't include cookies on cross-origin requests without this.
  withCredentials: true,
})

// ── Request interceptor — attach Bearer token ──────────────────────────────────

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`
  }
  return config
})

// ── Response interceptor — handle 401 with token refresh ──────────────────────

// Guards against multiple concurrent refreshes: if a refresh is already in
// flight, subsequent 401s queue up and resolve once the refresh completes.
let isRefreshing = false
let refreshQueue: Array<(token: string | null) => void> = []

api.interceptors.response.use(
  response => response,
  async error => {
    const original = error.config as InternalAxiosRequestConfig & { _retry?: boolean }

    // Only attempt refresh on 401 and only once per request.
    // Never intercept the refresh endpoint itself — that would deadlock
    // (the queued refresh waits for the in-flight refresh to complete).
    if (
      error.response?.status !== 401 ||
      original._retry ||
      original.url === '/api/auth/refresh'
    ) {
      return Promise.reject(error)
    }

    original._retry = true

    if (isRefreshing) {
      // A refresh is already in flight — queue this request until it resolves.
      return new Promise((resolve, reject) => {
        refreshQueue.push(token => {
          if (!token) return reject(error)
          original.headers.Authorization = `Bearer ${token}`
          resolve(api(original))
        })
      })
    }

    isRefreshing = true

    try {
      const { data } = await api.post<{ accessToken: string }>('/api/auth/refresh')
      setAccessToken(data.accessToken)
      refreshQueue.forEach(cb => cb(data.accessToken))
      original.headers.Authorization = `Bearer ${data.accessToken}`
      return api(original)
    } catch {
      // Refresh failed — session is expired. Clear token so the app can redirect to login.
      setAccessToken(null)
      refreshQueue.forEach(cb => cb(null))
      return Promise.reject(error)
    } finally {
      isRefreshing = false
      refreshQueue = []
    }
  },
)

export default api
