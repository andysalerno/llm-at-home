'use client'

import { useSearchParams } from 'next/navigation'
import DebugSection from '../components/debug/DebugSection'
import AppShell from '../components/layout/AppShell';

export default function ChatPage() {
    const searchParams = useSearchParams();
    const focusedMessageId = searchParams.get('selectedMessageId');

    return (
        <AppShell>
            <DebugSection focusedMessageId={focusedMessageId} />
        </AppShell>
    )
}