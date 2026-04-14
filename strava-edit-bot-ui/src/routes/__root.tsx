import { createRootRoute, Outlet } from '@tanstack/react-router'
import { TanStackRouterDevtools } from '@tanstack/router-devtools'
import { NavBar } from '@/components/NavBar'

export const Route = createRootRoute({
  component: () => (
    <>
      <NavBar />
      <main className="flex flex-1 flex-col">
        <Outlet />
      </main>
      <TanStackRouterDevtools />
    </>
  ),
})
