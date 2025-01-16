'use client'

import { useState } from 'react';
import { Text } from '@fluentui/react-components';
import { Navigation } from './Navigation';

interface AppShellProps {
    children: React.ReactNode;
}

export default function AppShell({ children }: AppShellProps) {
    const [isNavigationOpen, setIsNavigationOpen] = useState(true);

    return (
        <div className="flex h-screen bg-neutral-50">
            <Navigation
                isOpen={isNavigationOpen}
                onOpenChange={setIsNavigationOpen}
            />
            <div
                className="flex flex-col flex-grow overflow-hidden transition-[margin-left] duration-300"
                style={{ marginLeft: isNavigationOpen ? '256px' : '48px' }}
            >
                <header className="bg-white shadow-md z-10">
                    <div className="max-w-7xl mx-auto px-6">
                        <Text
                            size={600}
                            weight="semibold"
                            className="py-4 block"
                        >
                            AI Assistant
                        </Text>
                    </div>
                </header>
                <main className="flex-grow overflow-x-hidden overflow-y-auto bg-neutral-100 h-full">
                    {children}
                </main>
            </div>
        </div>
    );
}