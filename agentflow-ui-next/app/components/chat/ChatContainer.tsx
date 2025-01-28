'use client'

import { useEffect, useRef } from 'react';
import { ChatMessage } from './ChatMessage';
import { ChatInput } from './ChatInput';
import { useChat } from '../../hooks/useChat';
import { Message } from '../../types';

export function ChatContainer() {
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const { messages, streamingMessage, sendMessage, cancelStream, deleteMessage } = useChat();

    const handleDeleteMessage = (id: string) => {
        deleteMessage(id);
    };

    const handleMessageClick = (id: string) => {
        // Handle message click - you might want to implement this based on your needs
        console.log('Message clicked:', id);
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
                disabled={streamingMessage !== ''}
            />
        </div>
    );
}