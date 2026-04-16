import { Link } from '@tanstack/react-router'
import { useCurrentUser } from '@/hooks/useCurrentUser'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Button } from '@/components/ui/button'

const navLinkClass =
  'flex h-full items-center px-4 font-medium text-sm transition-all duration-150 [&>span]:transition-transform [&>span]:duration-150 hover:shadow-[inset_2px_2px_0px_0px_var(--border)] hover:[&>span]:translate-x-[2px] hover:[&>span]:translate-y-[2px]'
const navLinkActiveClass =
  'bg-main shadow-[inset_4px_4px_0px_0px_var(--border)] [&>span]:translate-x-[4px] [&>span]:translate-y-[4px] hover:shadow-[inset_4px_4px_0px_0px_var(--border)] hover:[&>span]:translate-x-[4px] hover:[&>span]:translate-y-[4px]'

export function NavBar() {
  const { data: user, isLoading } = useCurrentUser()

  return (
    <header className="flex h-14 items-center justify-between border-b-4 border-border bg-secondary-background text-left">
      <div className="flex h-full items-center">
        <Link to="/" className="px-6 font-heading font-black text-base">
          Strava Edit Bot
        </Link>

        {!isLoading && user && (
          <>
            <Link
              to="/activities"
              className={navLinkClass}
              activeProps={{ className: navLinkActiveClass }}
            >
              <span>My Activities</span>
            </Link>
            <Link
              to="/rules"
              className={navLinkClass}
              activeProps={{ className: navLinkActiveClass }}
            >
              <span>My Rules</span>
            </Link>
          </>
        )}
      </div>

      <div className="px-6">
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
