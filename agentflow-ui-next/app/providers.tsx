'use client';

import * as React from 'react';
import {
    FluentProvider,
    webDarkTheme,
    SSRProvider,
    RendererProvider,
    createDOMRenderer,
    renderToStyleElements,
} from '@fluentui/react-components';
import { useServerInsertedHTML } from 'next/navigation';
import { ConfigProvider } from './hooks/useConfig';

export function Providers({ children }: { children: React.ReactNode }) {
    const [renderer] = React.useState(() => createDOMRenderer());
    const didRenderRef = React.useRef(false);

    useServerInsertedHTML(() => {
        if (didRenderRef.current) {
            return;
        }
        didRenderRef.current = true;
        return <>{renderToStyleElements(renderer)}</>;
    });

    return (
        <RendererProvider renderer={renderer}>
            <SSRProvider>
                <ConfigProvider>
                    <FluentProvider theme={webDarkTheme}>{children}</FluentProvider>
                </ConfigProvider>
            </SSRProvider>
        </RendererProvider>
    );
}