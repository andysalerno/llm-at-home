'use client'

import { FluentProvider, webLightTheme } from '@fluentui/react-components'

export default function FluentUIProvider({
    children
}: {
    children: React.ReactNode
}) {
    return (
        <FluentProvider theme={webLightTheme}>
            {children}
        </FluentProvider>
    )
}