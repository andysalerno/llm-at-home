// 'use client'

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

// src/app/page.tsx
import { redirect } from 'next/navigation'

export default function Home() {
  redirect('/chat')
}