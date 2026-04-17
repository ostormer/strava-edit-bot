import { createFileRoute } from '@tanstack/react-router'
import { Card } from '@/components/ui/card'

export const Route = createFileRoute('/rules')({
  component: RulesPage,
})

function RulesPage() {
  return (
    <div className="p-8 bg-grid h-full bg-background">
      <Card className="bg-secondary-background">
        <h1 className="text-2xl font-heading">My Rules</h1>
      </Card>
    </div>
  )
}
