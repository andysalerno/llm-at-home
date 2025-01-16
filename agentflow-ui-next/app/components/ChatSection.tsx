'use client'

import React, { useState, useEffect, useRef, useCallback } from 'react';
import { TrashIcon } from '@heroicons/react/24/outline';

interface Message {
    role: 'user' | 'assistant' | 'system';
    content: string;
    correlationId: string;
}

const STORAGE_KEY = 'chatMessages';

interface ChatSectionProps {
    onMessageClick?: (correlationId: string) => void;
}

const ChatSection: React.FC<ChatSectionProps> = ({ onMessageClick }) => {
    const [messages, setMessages] = useState<Message[]>(() => {
        // Initialize from localStorage if available
        if (typeof window !== 'undefined') {
            const stored = localStorage.getItem(STORAGE_KEY);
            return stored ? JSON.parse(stored) : [];
        }
        return [];
    });
    const [newMessage, setNewMessage] = useState<string>('');
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [streamingMessage, setStreamingMessage] = useState<string>('');
    const messagesEndRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        scrollToBottom();
    }, [messages, streamingMessage]);

    useEffect(() => {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(messages));
    }, [messages]);

    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    };

    const handleSendMessage = useCallback(async (e: React.FormEvent) => {
        e.preventDefault();
        if (newMessage.trim() === '') return;

        const userMessage: Message = {
            role: 'user',
            content: newMessage.trim(),
            correlationId: Date.now().toString()
        };

        // Update messages with the new user message
        setMessages(prevMessages => [...prevMessages, userMessage]);
        setNewMessage('');
        setIsLoading(true);
        setStreamingMessage('');

        try {
            // Here's the key fix - we need to send ALL previous messages
            const response = await fetch(
                'http://nzxt.local:8003/v1/chat/completions',
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        // Include ALL messages, not just the new one
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

            let streamingMessageContent = '';

            while (true) {
                const { done, value } = await reader.read();
                if (done) {
                    break;
                };

                const lines = value.split('\n').filter(line => line.trim() !== '');

                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        const data = line.slice(6);
                        try {
                            const parsed = JSON.parse(data);
                            const content = parsed.choices[0].delta.content;
                            if (content) {
                                streamingMessageContent = streamingMessageContent + content;
                                setStreamingMessage(prev => prev + content);
                            }
                        } catch (error) {
                            console.error('Error parsing SSE data:', error);
                        }
                    }
                }
            }

            setMessages(prevMessages => [
                ...prevMessages,
                { role: 'assistant', content: streamingMessageContent, correlationId: "emptyfornow" }
            ]);
            setStreamingMessage('');

        } catch (error) {
            console.error('Error sending message:', error);
            setMessages(prevMessages => [
                ...prevMessages,
                {
                    role: 'system',
                    content: 'Sorry, there was an error processing your request.',
                    correlationId: Date.now().toString()
                }
            ]);
        } finally {
            setIsLoading(false);
        }

    }, [messages, newMessage, streamingMessage]);

    // const handleSendMessage = useCallback(async (e: React.FormEvent) => {
    //     e.preventDefault();
    //     if (newMessage.trim() === '') return;

    //     const userMessage: Message = {
    //         role: 'user',
    //         content: newMessage.trim(),
    //         correlationId: Date.now().toString()
    //     };

    //     setMessages(prevMessages => [...prevMessages, userMessage]);
    //     setNewMessage('');
    //     setIsLoading(true);
    //     setStreamingMessage('');

    //     try {
    //         const response = await fetch(
    //             'http://nzxt.local:8003/v1/chat/completions',
    //             {
    //                 method: 'POST',
    //                 headers: {
    //                     'Content-Type': 'application/json',
    //                 },
    //                 body: JSON.stringify({
    //                     messages: [...messages, userMessage],
    //                 }),
    //             });

    //         if (!response.body) throw new Error('No response body');

    //         const reader = response.body
    //             .pipeThrough(new TextDecoderStream())
    //             .getReader();

    //         while (true) {
    //             const { done, value } = await reader.read();
    //             if (done) break;

    //             const lines = value.split('\n').filter(line => line.trim() !== '');

    //             for (const line of lines) {
    //                 if (line.startsWith('data: ')) {
    //                     const data = line.slice(6);
    //                     if (data === '[DONE]') {
    //                         setMessages(prevMessages => {
    //                             const lastMessage = {
    //                                 role: 'assistant' as const,
    //                                 content: streamingMessage,
    //                                 correlationId: Date.now().toString()
    //                             };
    //                             const newMessages = [...prevMessages, lastMessage];
    //                             localStorage.setItem(STORAGE_KEY, JSON.stringify(newMessages));

    //                             return newMessages;
    //                         });
    //                         setStreamingMessage('');
    //                     } else {
    //                         try {
    //                             const parsed = JSON.parse(data);
    //                             const content = parsed.choices[0].delta.content;
    //                             if (content) {
    //                                 setStreamingMessage(prev => prev + content);
    //                             }
    //                         } catch (error) {
    //                             console.error('Error parsing SSE data:', error);
    //                         }
    //                     }
    //                 }
    //             }
    //         }
    //     } catch (error) {
    //         console.error('Error sending message:', error);
    //         setMessages(prevMessages => [
    //             ...prevMessages,
    //             {
    //                 role: 'system',
    //                 content: 'Sorry, there was an error processing your request.',
    //                 correlationId: Date.now().toString()
    //             }
    //         ]);
    //     } finally {
    //         setIsLoading(false);
    //     }
    // }, [messages, newMessage, streamingMessage]);

    const handleDeleteMessage = useCallback((correlationId: string) => {
        setMessages(prevMessages => prevMessages.filter(message => message.correlationId !== correlationId));
    }, []);

    return (
        <div className="flex flex-col h-full bg-gray-100">
            <div className="flex-1 overflow-auto p-4">
                {messages.map((message) => (
                    <div
                        key={message.correlationId}
                        className={`relative max-w-[70%] mb-4 p-3 rounded-lg hover:shadow-md transition-shadow duration-200 ${message.role === 'user'
                            ? 'ml-auto bg-blue-500 text-white'
                            : 'mr-auto bg-white text-gray-800'
                            }`}
                    >
                        <div
                            className="cursor-pointer"
                            onClick={() => onMessageClick?.(message.correlationId)}
                        >
                            {message.content}
                        </div>
                        <button
                            onClick={() => handleDeleteMessage(message.correlationId)}
                            className="absolute top-1 right-1 p-1 text-gray-500 hover:text-red-500 focus:outline-none opacity-0 hover:opacity-100 transition-opacity duration-200"
                            aria-label="Delete message"
                        >
                            <TrashIcon className="h-4 w-4" />
                        </button>
                    </div>
                ))}
                {streamingMessage && (
                    <div className="mr-auto bg-white text-gray-800 max-w-[70%] mb-4 p-3 rounded-lg">
                        {streamingMessage}
                    </div>
                )}
                <div ref={messagesEndRef} />
            </div>
            <form onSubmit={handleSendMessage} className="p-4 bg-white">
                <div className="flex">
                    <input
                        type="text"
                        value={newMessage}
                        onChange={(e) => setNewMessage(e.target.value)}
                        placeholder="Type your message here..."
                        className="flex-1 p-2 border border-gray-300 rounded-l-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        disabled={isLoading}
                    />
                    <button
                        type="submit"
                        className="px-4 py-2 bg-blue-500 text-white rounded-r-md hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-blue-300"
                        disabled={isLoading}
                    >
                        Send
                    </button>
                </div>
            </form>
        </div>
    );
};

export default ChatSection;