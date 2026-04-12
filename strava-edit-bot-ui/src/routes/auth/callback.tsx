import { createFileRoute, Link, useNavigate } from '@tanstack/react-router'
import { useEffect, useState } from 'react'
import api, { setAccessToken } from '@/lib/api'

export const Route = createFileRoute('/auth/callback')({
  component: CallbackPage,
})

function CallbackPage() {
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    const code = params.get('code')

    if (!code) {
      setError('No authorization code received from Strava.')
      return
    }

    api
      .post<{ accessToken: string }>('/api/auth/strava/callback', { code })
      .then(({ data }) => {
        setAccessToken(data.accessToken)
        navigate({ to: '/' })
      })
      .catch(() => {
        setError('Failed to complete sign-in. Please try again.')
      })
  }, [navigate])

  if (error) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-center">
          <p className="text-red-600 mb-4">{error}</p>
          <Link to="/login" className="underline underline-offset-4">
            Back to sign in
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="flex min-h-screen items-center justify-center">
      <p className="text-foreground">Completing sign-in…</p>
    </div>
  )
}
