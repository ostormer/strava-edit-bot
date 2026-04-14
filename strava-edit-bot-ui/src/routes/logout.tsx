import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { useEffect } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import api, { setAccessToken } from '@/lib/api'
import { currentUserQueryOptions } from '@/lib/queries/user'

export const Route = createFileRoute('/logout')({
  component: LogoutPage,
})

function LogoutPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  useEffect(() => {
    api
      .post('/api/auth/logout')
      .finally(() => {
        setAccessToken(null)
        queryClient.removeQueries({ queryKey: currentUserQueryOptions.queryKey })
        navigate({ to: '/login' })
      })
  }, [navigate, queryClient])

  return null
}
