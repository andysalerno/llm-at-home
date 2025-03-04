import { useState, useCallback, useEffect, useRef } from 'react';
import { Message } from '../types';
import { v4 as uuidv4 } from 'uuid';

const STORAGE_KEY = 'chatMessages';

function createMessage(
    role: Message['role'],
    content: string,
    conversationId: string
): Message {
    return {
        id: uuidv4(),
        role,
        content,
        timestamp: new Date().toISOString(),
        conversationId: conversationId,
    };
}

export function useChat() {
    const [messages, setMessages] = useState<Message[]>([]);
    const [streamingMessage, setStreamingMessage] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [conversationId, setConversationId] = useState<string>(uuidv4());
    const abortControllerRef = useRef<AbortController | null>(null);

    // Load messages from localStorage on mount
    useEffect(() => {
        const stored = localStorage.getItem(STORAGE_KEY);
        if (stored) {
            try {
                setMessages(JSON.parse(stored));
            } catch (error) {
                console.error('Error parsing stored messages:', error);
            }
        }
    }, []);

    // Persist messages to localStorage
    useEffect(() => {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(messages));
    }, [messages]);

    const processStream = async (
        reader: ReadableStreamDefaultReader<string>,
        // correlationId: string
    ) => {
        let buffer = '';
        let streamContent = '';

        try {
            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                buffer += value;
                const lines = buffer.split('\n');
                buffer = lines.pop() || '';

                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        const data = line.slice(6);
                        if (data === '[DONE]') continue;

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
        } finally {
            reader.releaseLock();
        }

        return streamContent;
    };

    const sendMessage = useCallback(async (content: string) => {
        if (!content.trim() || isLoading) return;

        const userMessage = createMessage('user', content.trim(), conversationId);

        setMessages((prev) => [...prev, userMessage]);
        setIsLoading(true);
        setStreamingMessage('');
        abortControllerRef.current = new AbortController();

        try {
            const response = await fetch('http://nzxt.local:8003/v1/chat/completions', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                signal: abortControllerRef.current.signal,
                body: JSON.stringify({
                    messages: [...messages, userMessage].map(({ role, content }) => ({
                        role,
                        content,
                    })),
                }),
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            if (!response.body) throw new Error('No response body');

            const reader = response.body
                .pipeThrough(new TextDecoderStream())
                .getReader();

            const streamContent = await processStream(reader); //, correlationId);

            setMessages((prev) => [
                ...prev,
                createMessage('assistant', streamContent, conversationId),
            ]);
        } catch (error) {
            if ((error as Error).name === 'AbortError') {
                setMessages((prev) => prev.filter((msg) => msg.id !== userMessage.id));
            } else {
                console.error('Error sending message:', error);
                setMessages((prev) => [
                    ...prev,
                    createMessage('system', 'Sorry, there was an error processing your request.', uuidv4()),
                ]);
            }
        } finally {
            setIsLoading(false);
            setStreamingMessage('');
            abortControllerRef.current = null;
        }
    }, [isLoading, messages, conversationId]);

    const cancelStream = useCallback(() => {
        if (abortControllerRef.current) {
            abortControllerRef.current.abort();
        }
        setIsLoading(false);
        setStreamingMessage('');
    }, []);

    const deleteMessage = useCallback((id: string) => {
        setMessages((prev) => prev.filter((msg) => msg.id !== id));
    }, []);

    const clearMessages = useCallback(() => {
        setMessages([]);
        setConversationId(uuidv4());
    }, []);

    return {
        messages,
        streamingMessage,
        isLoading,
        sendMessage,
        deleteMessage,
        clearMessages,
        cancelStream,
        conversationId,
    };
}