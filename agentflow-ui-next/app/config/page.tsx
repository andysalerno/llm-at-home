'use client'

import ConfigSection from '../components/config/ConfigSection';
import AppShell from '../components/layout/AppShell';
import { Suspense } from 'react'
import { useSearchParams } from 'next/navigation'

function ConfigPageInner() {
    const searchParams = useSearchParams();
    const focusedMessageId = searchParams.get('selectedMessageId');

    console.log(`rerendered page.tsx, setting id: ${focusedMessageId}`);

    return (
        <AppShell>
            <ConfigSection />
        </AppShell>
    )
}

export default function ConfigPage() {
    return (
        <Suspense>
            <ConfigPageInner />
        </Suspense>
    )
}