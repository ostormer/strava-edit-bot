import { createFileRoute } from '@tanstack/react-router'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'

export const Route = createFileRoute('/login')({
  component: LoginPage,
})

function LoginPage() {
  function handleConnectStrava() {
    const clientId = import.meta.env.VITE_STRAVA_CLIENT_ID
    const callbackUrl = `${window.location.origin}/auth/callback`
    const stravaAuthUrl =
      `https://www.strava.com/oauth/authorize` +
      `?client_id=${clientId}` +
      `&redirect_uri=${encodeURIComponent(callbackUrl)}` +
      `&response_type=code` +
      `&scope=read,activity:read_all` +
      `&approval_prompt=auto`

    window.location.href = stravaAuthUrl
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-background bg-grid">
      <Card className="w-full max-w-sm bg-secondary-background">
        <CardHeader>
          <CardTitle className="title">Sign in</CardTitle>
          <CardDescription>Connect your Strava account to get started.</CardDescription>
        </CardHeader>
        <CardContent />
        <CardFooter>
          <Button className="w-full" onClick={handleConnectStrava}>
            Connect with Strava
          </Button>
        </CardFooter>
      </Card>
    </div>
  )
}
