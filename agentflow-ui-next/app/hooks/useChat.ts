// src/hooks/useChat.ts

import { useState, useCallback } from 'react';
import { Message } from '../types';

const STORAGE_KEY = 'chatMessages';

export function useChat() {
    const [messages, setMessages] = useState<Message[]>(() => {
        if (typeof window !== 'undefined') {
            const stored = localStorage.getItem(STORAGE_KEY);
            return stored ? JSON.parse(stored) : [];
        }
        return [];
    });
    const [streamingMessage, setStreamingMessage] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    const sendMessage = useCallback(async (content: string) => {
        if (!content.trim()) return;

        const userMessage: Message = {
            id: crypto.randomUUID(),
            role: 'user',
            content: content.trim(),
            timestamp: new Date().toISOString(),
            correlationId: crypto.randomUUID()
        };

        setMessages(prev => [...prev, userMessage]);
        setIsLoading(true);
        setStreamingMessage('');

        try {
            const response = await fetch('http://nzxt.local:8003/v1/chat/completions', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    messages: [...messages, userMessage].map(msg => ({
                        role: msg.role,
                        content: msg.content
                    })),
                }),
            });

            if (!response.body) throw new Error('No response body');

            const reader = response.body
                .pipeThrough(new TextDecoderStream())
                .getReader();

            let streamContent = '';

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                const lines = value.split('\n').filter(line => line.trim() !== '');

                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        const data = line.slice(6);
                        try {
                            const parsed = JSON.parse(data);
                            const content = parsed.choices[0].delta.content;
                            if (content) {
                                streamContent += content;
                                setStreamingMessage(streamContent);
                            }
                        } catch (error) {
                            console.error('Error parsing SSE data:', error);
                        }
                    }
                }
            }

            setMessages(prev => [
                ...prev,
                {
                    id: crypto.randomUUID(),
                    role: 'assistant',
                    content: streamContent,
                    timestamp: new Date().toISOString(),
                    correlationId: crypto.randomUUID()
                }
            ]);
        } catch (error) {
            console.error('Error sending message:', error);
            setMessages(prev => [
                ...prev,
                {
                    id: crypto.randomUUID(),
                    role: 'system',
                    content: 'Sorry, there was an error processing your request.',
                    timestamp: new Date().toISOString(),
                    correlationId: crypto.randomUUID()
                }
            ]);
        } finally {
            setIsLoading(false);
            setStreamingMessage('');
        }
    }, [messages]);

    const deleteMessage = useCallback((id: string) => {
        setMessages(prev => prev.filter(msg => msg.id !== id));
    }, []);

    const cancelStream = useCallback(() => {
        setStreamingMessage('');
        setIsLoading(false);
    }, []);

    return {
        messages,
        streamingMessage,
        isLoading,
        sendMessage,
        deleteMessage,
        cancelStream,
    };
}