'use client'

import { ChatContainer } from '../components/chat/ChatContainer';
import DebugSection from '../components/debug/DebugSection'
import AppShell from '../components/layout/AppShell';
import SplitPane from '../components/layout/SplitPane';

export default function ChatPage() {
    return (
        <AppShell>
            <SplitPane
                left={<ChatContainer />}
                right={<DebugSection focusedMessageId={null} />}
            />
        </AppShell>
    )
}