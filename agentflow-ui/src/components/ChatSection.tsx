import React, { useState, useEffect, useRef, useCallback } from 'react';
import { v4 as uuidv4 } from 'uuid';
import { TrashIcon } from '@heroicons/react/24/outline';

const STORAGE_KEY = 'chatMessages';

interface ChatSectionProps {
    // setFocusedMessageId: React.Dispatch<React.SetStateAction<string>>;
    setFocusedMessageId: (id: string) => void;

}

const ChatSection: React.FC<ChatSectionProps> = ({ setFocusedMessageId }) => {
    const [messages, setMessages] = useState(() => {
        const storedMessages = localStorage.getItem(STORAGE_KEY);
        return storedMessages ? JSON.parse(storedMessages) : [];
    });
    const [newMessage, setNewMessage] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [streamingMessage, setStreamingMessage] = useState('');
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

    const handleSendMessage = useCallback(async (e) => {
        e.preventDefault();
        if (newMessage.trim() === '') return;

        const userMessage = {
            role: 'user',
            content: newMessage.trim()
        };

        setMessages(prevMessages => [...prevMessages, userMessage]);
        setNewMessage('');
        setIsLoading(true);
        setStreamingMessage('');

        const correlationId = uuidv4();

        try {
            const response = await fetch(
                'http://nzxt.local:8003/v1/chat/completions',
                {
                    method: 'POST',
                    headers: { "X-Correlation-ID": correlationId },
                    body: JSON.stringify({
                        messages: [...messages, userMessage].map(msg => ({ role: msg.role, content: msg.content })),
                        model: "gpt-3.5-turbo",
                        stream: true
                    })
                });

            // To recieve data as a string we use TextDecoderStream class in pipethrough
            const reader = response?.body?.pipeThrough(new TextDecoderStream()).getReader()

            if (!reader) {
                return;
            }

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
                { role: 'assistant', content: streamingMessageContent, correlationId: correlationId }
            ]);
            setStreamingMessage('');

        } catch (error) {
            console.error('Error sending message:', error);
            setMessages(prevMessages => [
                ...prevMessages,
                { role: 'system', content: 'Sorry, there was an error processing your request.' }
            ]);
        } finally {
            setIsLoading(false);
        }
    }, [messages, newMessage, streamingMessage]);

    const clearChat = useCallback(() => {
        setMessages([]);
        localStorage.removeItem(STORAGE_KEY);
    }, []);

    const handleDeleteMessage = useCallback((correlationId) => {
        setMessages(prevMessages => prevMessages.filter(message => message.correlationId !== correlationId));
    }, []);

    return (
        <div className="flex flex-col h-full">
            <div className="flex-1 overflow-auto p-4">
                {messages.map((message, index) => (
                    <div
                        key={message.correlationId}
                        className={`relative max-w-[70%] mb-4 p-3 rounded-lg ${message.role === 'user'
                            ? 'ml-auto'
                            : 'mr-auto'
                            }`}
                    >
                        <div
                            className="cursor-pointer"
                            onClick={() => setFocusedMessageId(message.correlationId)}
                        >
                            {message.content}
                        </div>
                        <button
                            onClick={() => handleDeleteMessage(message.correlationId)}
                            className="absolute top-1 right-1 p-1 text-gray-500 hover:text-red-500 focus:outline-none"
                            aria-label="Delete message"
                        >
                            <TrashIcon className="h-4 w-4" />
                        </button>
                    </div>
                ))}
                {streamingMessage && (
                    <div className="mr-auto max-w-[70%] mb-4 p-3 rounded-lg">
                        {streamingMessage}
                    </div>
                )}
                <div ref={messagesEndRef} />
            </div>
            <form onSubmit={handleSendMessage} className="p-4">
                <div className="flex">
                    <input
                        type="text"
                        value={newMessage}
                        onChange={(e) => setNewMessage(e.target.value)}
                        placeholder="Type your message here..."
                        className="flex-1 p-2 border rounded-l-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        disabled={isLoading}
                    />
                    <button
                        type="submit"
                        className="px-4 py-2 rounded-r-md hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-blue-300"
                        disabled={isLoading}
                    >
                        Send
                    </button>
                </div>
            </form>
            <div className="p-4 border-t">
                <button
                    onClick={clearChat}
                    className="px-4 py-2 rounded-md focus:outline-none focus:ring-2 focus:ring-red-500"
                >
                    Clear Chat
                </button>
            </div>
        </div>
    );
};

export default ChatSection;