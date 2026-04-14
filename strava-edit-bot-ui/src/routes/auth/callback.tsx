import { createFileRoute, Link, useNavigate } from '@tanstack/react-router'
import { useEffect, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import api, { setAccessToken } from '@/lib/api'
import { currentUserQueryOptions, type CurrentUser } from '@/lib/queries/user'

export const Route = createFileRoute('/auth/callback')({
  component: CallbackPage,
})

interface CallbackResponse extends CurrentUser {
  accessToken: string
}

function CallbackPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    const code = params.get('code')

    if (!code) {
      setError('No authorization code received from Strava.')
      return
    }

    api
      .post<CallbackResponse>('/api/auth/strava/callback', { code })
      .then(({ data }) => {
        setAccessToken(data.accessToken)
        // Pre-populate the query cache so any component calling useCurrentUser()
        // has the data immediately — no extra /api/users/me fetch needed.
        queryClient.setQueryData(currentUserQueryOptions.queryKey, {
          firstname: data.firstname,
          lastname: data.lastname,
          profileMedium: data.profileMedium,
          profile: data.profile,
        })
        navigate({ to: '/' })
      })
      .catch(() => {
        setError('Failed to complete sign-in. Please try again.')
      })
  }, [navigate, queryClient])

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
