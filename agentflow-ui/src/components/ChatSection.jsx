import React, { useState } from 'react';

const ChatSection = () => {
    const [messages, setMessages] = useState([
        { id: 1, sender: 'user', content: 'Hello, how are you?' },
        {
            id: 2, sender: 'bot', content: 'Hello! I\'m doing well, thank you for asking.How can I assist you today?'
        },
        { id: 3, sender: 'user', content: 'I have a question about React hooks.' },
        { id: 4, sender: 'bot', content: 'Certainly! I\'d be happy to help you with React hooks.What specific aspect of hooks would you like to know more about?' },
    ]);

    const [newMessage, setNewMessage] = useState('');

    const handleSendMessage = (e) => {
        e.preventDefault();
        if (newMessage.trim() === '') return;

        const newMsg = {
            id: messages.length + 1,
            sender: 'user',
            content: newMessage.trim()
        };

        setMessages([...messages, newMsg]);
        setNewMessage('');

        // Here you would typically send the message to your API
        // and then add the bot's response to the messages array
    };

    return (
        <div className="flex flex-col h-full bg-gray-100">
            <div className="flex-1 overflow-auto p-4">
                {messages.map((message) => (
                    <div
                        key={message.id}
                        className={`max-w-[70%] mb-4 p-3 rounded-lg ${message.sender === 'user'
                            ? 'ml-auto bg-blue-500 text-white'
                            : 'mr-auto bg-white text-gray-800'
                            }`}
                    >
                        {message.content}
                    </div>
                ))}
            </div>
            <form onSubmit={handleSendMessage} className="p-4 bg-white">
                <div className="flex">
                    <input
                        type="text"
                        value={newMessage}
                        onChange={(e) => setNewMessage(e.target.value)}
                        placeholder="Type your message here..."
                        className="flex-1 p-2 border border-gray-300 rounded-l-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                    />
                    <button
                        type="submit"
                        className="px-4 py-2 bg-blue-500 text-white rounded-r-md hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-500"
                    >
                        Send
                    </button>
                </div>
            </form>
        </div>
    );
};

export default ChatSection;