import { queryOptions } from '@tanstack/react-query'
import api from '@/lib/api'

export interface CurrentUser {
  firstname: string
  lastname: string
  profileMedium: string
  profile: string
}

async function fetchCurrentUser(): Promise<CurrentUser> {
  const { data } = await api.get<CurrentUser>('/api/users/me')
  return data
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
