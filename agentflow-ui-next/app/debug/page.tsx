'use client'

import { useSearchParams } from 'next/navigation'
import { Suspense } from 'react'
import DebugSection from '../components/debug/DebugSection'
import AppShell from '../components/layout/AppShell';

function DebugPageInner() {
    const searchParams = useSearchParams();
    const focusedMessageId = searchParams.get('selectedMessageId');

    return (
        <AppShell>
            <DebugSection focusedMessageId={focusedMessageId} />
        </AppShell>
    )

}

export default function DebugPage() {
    return (
        <Suspense>
            <DebugPageInner />
        </Suspense>
    )
}