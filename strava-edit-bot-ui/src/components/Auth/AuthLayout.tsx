import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'

type Props = {
    title: string
    description: string
    content: React.ReactNode
    footer: React.ReactNode
}

export default function AuthLayout({ title, description, content, footer }: Props) {
    return (
        <div className="flex min-h-screen items-center justify-center bg-secondary-background bg-grid">
            <Card className="w-full max-w-sm">
                <CardHeader>
                    <CardTitle className='title'>{title}</CardTitle>
                    <CardDescription>{description}</CardDescription>
                </CardHeader>
                <CardContent>{content}</CardContent>
                <CardFooter className="flex-col gap-2">{footer}</CardFooter>
            </Card>
        </div>
    )
}

