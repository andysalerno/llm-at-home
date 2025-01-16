// 'use client'

// import React, { useState, useEffect, useRef, useCallback } from 'react';
// import {
//     Button,
//     Input,
//     Card,
//     CardHeader,
// } from '@fluentui/react-components';
// import { Delete24Regular, Send24Regular } from '@fluentui/react-icons';

// interface Message {
//     role: 'user' | 'assistant' | 'system';
//     content: string;
//     correlationId: string;
// }

// const STORAGE_KEY = 'chatMessages';

// interface ChatSectionProps {
//     onMessageClick?: (correlationId: string) => void;
// }

// const ChatSection: React.FC<ChatSectionProps> = ({ onMessageClick }) => {
//     const [messages, setMessages] = useState<Message[]>(() => {
//         if (typeof window !== 'undefined') {
//             const stored = localStorage.getItem(STORAGE_KEY);
//             return stored ? JSON.parse(stored) : [];
//         }
//         return [];
//     });
//     const [newMessage, setNewMessage] = useState<string>('');
//     const [isLoading, setIsLoading] = useState<boolean>(false);
//     const [streamingMessage, setStreamingMessage] = useState<string>('');
//     const messagesEndRef = useRef<HTMLDivElement>(null);

//     useEffect(() => {
//         scrollToBottom();
//     }, [messages, streamingMessage]);

//     useEffect(() => {
//         localStorage.setItem(STORAGE_KEY, JSON.stringify(messages));
//     }, [messages]);

//     const scrollToBottom = () => {
//         messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
//     };

//     const handleSendMessage = useCallback(async (e: React.FormEvent) => {
//         e.preventDefault();
//         if (newMessage.trim() === '') return;

//         const userMessage: Message = {
//             role: 'user',
//             content: newMessage.trim(),
//             correlationId: Date.now().toString()
//         };

//         setMessages(prevMessages => [...prevMessages, userMessage]);
//         setNewMessage('');
//         setIsLoading(true);
//         setStreamingMessage('');

//         try {
//             const response = await fetch(
//                 'http://nzxt.local:8003/v1/chat/completions',
//                 {
//                     method: 'POST',
//                     headers: {
//                         'Content-Type': 'application/json',
//                     },
//                     body: JSON.stringify({
//                         messages: [...messages, userMessage].map(msg => ({
//                             role: msg.role,
//                             content: msg.content
//                         })),
//                     }),
//                 });

//             if (!response.body) throw new Error('No response body');

//             const reader = response.body
//                 .pipeThrough(new TextDecoderStream())
//                 .getReader();

//             let streamingMessageContent = '';

//             while (true) {
//                 const { done, value } = await reader.read();
//                 if (done) break;

//                 const lines = value.split('\n').filter(line => line.trim() !== '');

//                 for (const line of lines) {
//                     if (line.startsWith('data: ')) {
//                         const data = line.slice(6);
//                         try {
//                             const parsed = JSON.parse(data);
//                             const content = parsed.choices[0].delta.content;
//                             if (content) {
//                                 streamingMessageContent = streamingMessageContent + content;
//                                 setStreamingMessage(prev => prev + content);
//                             }
//                         } catch (error) {
//                             console.error('Error parsing SSE data:', error);
//                         }
//                     }
//                 }
//             }

//             setMessages(prevMessages => [
//                 ...prevMessages,
//                 { role: 'assistant', content: streamingMessageContent, correlationId: Date.now().toString() }
//             ]);
//             setStreamingMessage('');

//         } catch (error) {
//             console.error('Error sending message:', error);
//             setMessages(prevMessages => [
//                 ...prevMessages,
//                 {
//                     role: 'system',
//                     content: 'Sorry, there was an error processing your request.',
//                     correlationId: Date.now().toString()
//                 }
//             ]);
//         } finally {
//             setIsLoading(false);
//         }
//     }, [messages, newMessage]);

//     const handleDeleteMessage = useCallback((correlationId: string) => {
//         setMessages(prevMessages => prevMessages.filter(message => message.correlationId !== correlationId));
//     }, []);

//     return (
//         <div className="flex flex-col h-full bg-gray-50">
//             <div className="flex-1 overflow-auto p-4">
//                 {messages.map((message) => (
//                     <div
//                         key={message.correlationId}
//                         className={`relative max-w-[70%] mb-4 group ${message.role === 'user' ? 'ml-auto' : 'mr-auto'
//                             }`}
//                     >
//                         <Card
//                             className={`cursor-pointer transition-shadow duration-200 ${message.role === 'user'
//                                     ? 'bg-blue-600 text-white hover:shadow-lg'
//                                     : 'bg-white hover:shadow-md'
//                                 }`}
//                             onClick={() => onMessageClick?.(message.correlationId)}
//                         >
//                             <CardHeader>
//                                 <div className="p-2">{message.content}</div>
//                             </CardHeader>
//                         </Card>
//                         <Button
//                             icon={<Delete24Regular />}
//                             appearance="subtle"
//                             className={`absolute -top-2 -right-2 opacity-0 group-hover:opacity-100 transition-opacity duration-200 
//                                 ${message.role === 'user' ? 'text-white' : 'text-gray-600'}`}
//                             onClick={() => handleDeleteMessage(message.correlationId)}
//                             aria-label="Delete message"
//                         />
//                     </div>
//                 ))}
//                 {streamingMessage && (
//                     <div className="max-w-[70%] mr-auto mb-4">
//                         <Card className="bg-white">
//                             <CardHeader>
//                                 <div className="p-2">{streamingMessage}</div>
//                             </CardHeader>
//                         </Card>
//                     </div>
//                 )}
//                 <div ref={messagesEndRef} />
//             </div>
//             <form onSubmit={handleSendMessage} className="p-4 bg-white border-t">
//                 <div className="flex gap-2">
//                     <Input
//                         value={newMessage}
//                         onChange={(e) => setNewMessage(e.target.value)}
//                         placeholder="Type your message here..."
//                         className="flex-1"
//                         disabled={isLoading}
//                     />
//                     <Button
//                         icon={<Send24Regular />}
//                         type="submit"
//                         appearance="primary"
//                         disabled={isLoading}
//                     >
//                         Send
//                     </Button>
//                 </div>
//             </form>
//         </div>
//     );
// };

// export default ChatSection;