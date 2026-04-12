import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { useEffect } from 'react'
import api, { setAccessToken } from '@/lib/api'

export const Route = createFileRoute('/logout')({
  component: LogoutPage,
})

function LogoutPage() {
  const navigate = useNavigate()

  useEffect(() => {
    api
      .post('/api/auth/logout')
      .finally(() => {
        setAccessToken(null)
        navigate({ to: '/login' })
      })
  }, [navigate])

  return null
}
