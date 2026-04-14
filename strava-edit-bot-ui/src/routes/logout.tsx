import { createFileRoute } from '@tanstack/react-router'
import { useEffect } from 'react'
import api from '@/lib/api'

export const Route = createFileRoute('/logout')({
  component: LogoutPage,
})

function LogoutPage() {
  useEffect(() => {
    api
      .post('/api/auth/logout')
      .finally(() => {
        window.location.href = '/login'
      })
  }, [])

  return null
}
