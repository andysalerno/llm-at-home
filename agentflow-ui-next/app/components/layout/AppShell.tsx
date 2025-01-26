'use client'

import { useState } from 'react';
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
                <main className="flex-grow overflow-x-hidden overflow-y-auto bg-neutral-100 h-full">
                    {children}
                </main>
            </div>
        </div>
    );
}