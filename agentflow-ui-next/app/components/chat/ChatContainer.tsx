'use client'

import { useEffect, useRef } from 'react';
import { ChatMessage } from './ChatMessage';
import { ChatInput } from './ChatInput';
import { useChat } from '../../hooks/useChat';
import { Message } from '../../types';
import { useSearchParams, useRouter } from 'next/navigation'

export function ChatContainer() {
    const router = useRouter();
    const searchParams = useSearchParams();

    const messagesEndRef = useRef<HTMLDivElement>(null);
    const { messages, streamingMessage, sendMessage, cancelStream, clearMessages, deleteMessage } = useChat();

    const handleDeleteMessage = (id: string) => {
        console.log('(1)Deleting message with id:', id);
        deleteMessage(id);
    };

    const handleMessageClick = (id: string) => {
        // Handle message click - you might want to implement this based on your needs
        console.log('Message clicked:', id);
        const params = new URLSearchParams(searchParams)
        params.set('selectedMessageId', id)

        // Update the URL without a full page refresh
        router.push(`?${params.toString()}`)
    };

    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages, streamingMessage]);

    return (
        <div className="flex flex-col h-full">
            <div className="flex-1 overflow-y-auto p-6">
                {messages.map((message: Message) => (
                    <ChatMessage
                        key={message.id}
                        message={message}
                        onDelete={handleDeleteMessage}
                        onClick={handleMessageClick}
                    />
                ))}
                {streamingMessage && (
                    <div className="opacity-70">
                        <ChatMessage
                            message={{
                                id: 'streaming',
                                role: 'assistant',
                                content: streamingMessage,
                                timestamp: new Date().toISOString(),
                                correlationId: 'streaming'
                            }}
                        />
                    </div>
                )}
                <div ref={messagesEndRef} />
            </div>
            <ChatInput
                onSubmit={sendMessage}
                onCancel={cancelStream}
                onClear={clearMessages}
                disabled={streamingMessage !== ''}
            />
        </div>
    );
}