import { Card } from '@/components/ui/card'
import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/')({
  component: HomePage,
})

function HomePage() {
  return (
    <div className="p-8 bg-grid h-full bg-background">
      <Card className="bg-secondary-background">
        <h1 className="text-2xl font-heading">e</h1>
      </Card>
    </div>
  )
}
