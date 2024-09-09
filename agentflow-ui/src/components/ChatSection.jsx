import React, { useState, useEffect, useRef, useCallback } from 'react';

const STORAGE_KEY = 'chatMessages';

const ChatSection = () => {
    const [messages, setMessages] = useState(() => {
        const storedMessages = localStorage.getItem(STORAGE_KEY);
        return storedMessages ? JSON.parse(storedMessages) : [];
    });
    const [newMessage, setNewMessage] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [streamingMessage, setStreamingMessage] = useState('');
    const messagesEndRef = useRef(null);

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

        try {
            const response = await fetch(
                'http://nzxt.local:8003/v1/chat/completions',
                {
                    method: 'POST',
                    body: JSON.stringify({
                        messages: [...messages, userMessage].map(msg => ({ role: msg.role, content: msg.content })),
                        model: "gpt-3.5-turbo",
                        stream: true,
                    })
                });

            // To recieve data as a string we use TextDecoderStream class in pipethrough
            const reader = response.body.pipeThrough(new TextDecoderStream()).getReader()

            let streamingMessageContent = '';

            while (true) {
                const { done, value } = await reader.read();
                if (done) {
                    console.info("done");
                    break;
                };

                console.info("got line: " + value);

                const lines = value.split('\n').filter(line => line.trim() !== '');

                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        const data = line.slice(6);
                        console.info("got data: " + data);
                        try {
                            const parsed = JSON.parse(data);
                            const content = parsed.choices[0].delta.content;
                            console.info("got content: " + content);
                            if (content) {
                                streamingMessageContent = streamingMessageContent + content;
                                setStreamingMessage(prev => prev + content);
                                console.log("streamingMessage is now:" + streamingMessage);
                            }
                        } catch (error) {
                            console.error('Error parsing SSE data:', error);
                        }
                    }
                }
            }

            console.log("setting messages with latest message to: " + streamingMessageContent);
            setMessages(prevMessages => [
                ...prevMessages,
                { role: 'assistant', content: streamingMessageContent }
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

    return (
        <div className="flex flex-col h-full bg-gray-100">
            <div className="flex-1 overflow-auto p-4">
                {messages.map((message, index) => (
                    <div
                        key={index}
                        className={`max-w-[70%] mb-4 p-3 rounded-lg ${message.role === 'user'
                            ? 'ml-auto bg-blue-500 text-white'
                            : 'mr-auto bg-white text-gray-800'
                            }`}
                    >
                        {message.content}
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
            <div className="p-4 bg-white border-t">
                <button
                    onClick={clearChat}
                    className="px-4 py-2 bg-red-500 text-white rounded-md hover:bg-red-600 focus:outline-none focus:ring-2 focus:ring-red-500"
                >
                    Clear Chat
                </button>
            </div>
        </div>
    );
};

export default ChatSection;