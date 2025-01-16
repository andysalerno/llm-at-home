'use client'

import { ChatContainer } from '../components/chat/ChatContainer';
import DebugSection from '../components/DebugSection'
import AppShell from '../components/layout/AppShell';
import SplitView from '../components/SplitView'

// export default function ChatPage() {
//     return (
//         <div className="h-full p-4">
//             <ChatContainer />
//         </div>
//     )
// }

export default function ChatPage() {
    return (
        <AppShell>
            <SplitView
                left={<ChatContainer />}
                right={<DebugSection focusedMessageId={null} />}
            />
        </AppShell>
    )
}

// import { useState } from 'react'
// import SplitView from './components/SplitView'
// import ChatSection from './components/ChatSection'
// import DebugSection from './components/DebugSection'
// import AppShell from './components/layout/AppShell'

// export default function Home() {
//   const [focusedMessageId, setFocusedMessageId] = useState<string | null>(null);

//   const handleMessageClick = (correlationId: string) => {
//     setFocusedMessageId(correlationId);
//   };

//   return (
//     <AppShell>
//       <SplitView
//         left={<ChatSection onMessageClick={handleMessageClick} />}
//         right={<DebugSection focusedMessageId={focusedMessageId} />}
//       />
//     </AppShell>
//   )
// }