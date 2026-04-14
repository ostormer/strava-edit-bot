import { useQuery } from '@tanstack/react-query'
import { currentUserQueryOptions } from '@/lib/queries/user'

export function useCurrentUser() {
  return useQuery(currentUserQueryOptions)
}
