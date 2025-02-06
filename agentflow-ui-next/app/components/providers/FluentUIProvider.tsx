'use client'

import { FluentProvider, webDarkTheme } from '@fluentui/react-components'

export default function FluentUIProvider({
    children
}: {
    children: React.ReactNode
}) {
    return (
        <FluentProvider theme={webDarkTheme}>
            {children}
        </FluentProvider>
    )
}