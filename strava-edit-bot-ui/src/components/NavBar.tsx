import { Link } from '@tanstack/react-router'
import { useCurrentUser } from '@/hooks/useCurrentUser'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Button } from '@/components/ui/button'

export function NavBar() {
  const { data: user, isLoading } = useCurrentUser()

  return (
    <header className="flex h-14 items-center justify-between border-b-4 border-border bg-secondary-background px-6 text-left">
      <Link to="/" className="font-heading font-black text-base">
        Strava Edit Bot
      </Link>

      <div>
        {isLoading && (
          <Button disabled className="w-36 animate-pulse" />
        )}

        {!isLoading && !user && (
          <Link to="/login">
            <Button className="w-36">Sign in</Button>
          </Link>
        )}

        {!isLoading && user && (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button className="w-36 justify-start px-1">
                <img
                  src={user.profileMedium}
                  alt={user.firstname}
                  className="size-7 shrink-0 border-2 border-border object-cover"
                />
                <span className="min-w-0 truncate">{user.firstname}</span>
              </Button>
            </DropdownMenuTrigger>

            <DropdownMenuContent align="end">
              <DropdownMenuItem asChild>
                <Link to="/logout">Sign out</Link>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        )}
      </div>
    </header>
  )
}
