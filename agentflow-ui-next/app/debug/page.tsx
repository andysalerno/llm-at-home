'use client'

import DebugSection from '../components/debug/DebugSection'
import AppShell from '../components/layout/AppShell';

export default function ChatPage() {
    return (
        <AppShell>
            <DebugSection focusedMessageId={null} />
        </AppShell>
    )
}