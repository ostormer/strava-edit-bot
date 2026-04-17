import { queryOptions } from '@tanstack/react-query'
import api from '@/lib/api'
import { isAxiosError } from 'axios'

export interface CurrentUser {
  firstname: string
  lastname: string
  profileMedium: string
  profile: string
}

async function fetchCurrentUser(): Promise<CurrentUser | null> {
  try {
    const { data } = await api.get<CurrentUser>('/api/users/me')
    return data
  } catch (error) {
    // 401 means the session is gone (refresh already attempted by the Axios
    // interceptor). Return null so the query resolves with "no user" instead
    // of staying stuck in pending/error state forever.
    if (isAxiosError(error) && error.response?.status === 401) {
      return null
    }
    throw error
  }
}

export const currentUserQueryOptions = queryOptions({
  queryKey: ['currentUser'],
  queryFn: fetchCurrentUser,
  // User data only changes on login/logout, so never re-fetch automatically.
  // We control freshness explicitly: setQueryData on login, removeQueries on logout.
  staleTime: Infinity,
  // Don't retry on failure — the Axios interceptor already handles the
  // token refresh + retry internally. By the time TanStack Query sees an
  // error, the refresh has already been attempted and failed.
  retry: false,
})
