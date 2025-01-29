'use client'

import { ChatContainer } from '../components/chat/ChatContainer';
import DebugSection from '../components/debug/DebugSection'
import AppShell from '../components/layout/AppShell';
import SplitPane from '../components/layout/SplitPane';
import { useSearchParams } from 'next/navigation'

export default function ChatPage() {
    const searchParams = useSearchParams();
    const focusedMessageId = searchParams.get('selectedMessageId');

    console.log(`rerendered page.tsx, setting id: ${focusedMessageId}`);

    return (
        <AppShell>
            <SplitPane
                left={<ChatContainer />}
                right={<DebugSection focusedMessageId={focusedMessageId} />}
            />
        </AppShell>
    )
}