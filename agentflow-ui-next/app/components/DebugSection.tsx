'use client'

import React, { useState, useEffect } from 'react';
import { ChevronDownIcon, ChevronRightIcon } from '@heroicons/react/24/solid';

interface LlmRequest {
    id: string;
    prompt: string;
    response: string;
}

interface Message {
    id: string;
    content: string;
    correlationId: string;
    llmRequests: LlmRequest[];
}

interface Session {
    id: string;
    messages: Message[];
}

interface DebugData {
    sessions: Session[];
}

interface DebugSectionProps {
    focusedMessageId: string | null;
}

interface TreeNodeProps {
    label: string;
    children?: React.ReactNode;
    onSelect?: () => void;
}

const TreeNode: React.FC<TreeNodeProps> = ({ label, children, onSelect }) => {
    const [isOpen, setIsOpen] = useState<boolean>(false);

    return (
        <div>
            <div
                className="flex items-center cursor-pointer hover:bg-blue-100 p-2 rounded transition-colors duration-150 ease-in-out"
                onClick={() => {
                    setIsOpen(!isOpen);
                    if (onSelect) onSelect();
                }}
            >
                {children && (
                    isOpen ? <ChevronDownIcon className="w-4 h-4 mr-2 text-blue-500" /> : <ChevronRightIcon className="w-4 h-4 mr-2 text-blue-500" />
                )}
                <span>{label}</span>
            </div>
            {isOpen && children && <div className="pl-4 border-l border-gray-200 ml-2">{children}</div>}
        </div>
    );
};

const DebugSection: React.FC<DebugSectionProps> = ({ focusedMessageId }) => {
    const [data, setData] = useState<DebugData>({ sessions: [] });
    const [selectedItem, setSelectedItem] = useState<{ type: string; id: string } | null>(null);
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const fetchData = async () => {
            setIsLoading(true);
            try {
                const response = await fetch('/api/debug-data');
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                const result = await response.json();
                setData(result);
                setIsLoading(false);
            } catch (e) {
                console.error("An error occurred while fetching the data: ", e);
                setError(e instanceof Error ? e.message : String(e));
                setIsLoading(false);
            }
        };

        fetchData();
    }, []);

    useEffect(() => {
        if (focusedMessageId) {
            const message = data.sessions.flatMap(s => s.messages).find(m => m.correlationId === focusedMessageId);
            if (message) {
                setSelectedItem({ type: 'message', id: message.id });
            }
        }
    }, [focusedMessageId, data]);

    const renderDetail = () => {
        if (!selectedItem) return <div>Select an item to view details</div>;

        let item;
        if (selectedItem.type === 'session') {
            item = data.sessions.find(s => s.id === selectedItem.id);
            return <div>Session ID: {item?.id}</div>;
        } else if (selectedItem.type === 'message') {
            item = data.sessions.flatMap(s => s.messages).find(m => m.id === selectedItem.id);
            return <div>Message: {item?.content}</div>;
        } else if (selectedItem.type === 'request') {
            item = data.sessions.flatMap(s => s.messages).flatMap(m => m.llmRequests).find(r => r.id === selectedItem.id);
            return (
                <pre className="font-mono text-sm bg-gray-100 p-4 rounded-md shadow-inner overflow-auto whitespace-pre-wrap">
                    <h3>Request: {item?.id}</h3>
                    <p><strong>Prompt:</strong> {item?.prompt}</p>
                    <p><strong>Response:</strong> {item?.response}</p>
                </pre>
            );
        }
    };

    if (isLoading) return <div>Loading...</div>;
    if (error) return <div>Error: {error}</div>;

    return (
        <div className="flex h-full">
            <div className="w-1/3 overflow-auto border-r p-4 bg-gray-50">
                <h2 className="text-xl font-bold mb-4">Navigation</h2>
                {data.sessions.map(session => (
                    <TreeNode
                        key={session.id}
                        label={`Session: ${session.id}`}
                        onSelect={() => setSelectedItem({ type: 'session', id: session.id })}
                    >
                        {session.messages.map(message => (
                            <TreeNode
                                key={message.id}
                                label={`Message: ${message.content.substring(0, 20)}...`}
                                onSelect={() => setSelectedItem({ type: 'message', id: message.id })}
                            >
                                {message.llmRequests.map(request => (
                                    <TreeNode
                                        key={request.id}
                                        label={`Request: ${request.id}`}
                                        onSelect={() => setSelectedItem({ type: 'request', id: request.id })}
                                    />
                                ))}
                            </TreeNode>
                        ))}
                    </TreeNode>
                ))}
            </div>
            <div className="w-2/3 p-4 overflow-auto bg-white">
                <h2 className="text-xl font-bold mb-4">Detail View</h2>
                <div className="h-[calc(100%-2rem)]">
                    {renderDetail()}
                </div>
            </div>
        </div>
    );
};

export default DebugSection;