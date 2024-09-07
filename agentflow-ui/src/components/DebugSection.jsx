import React, { useState, useEffect } from 'react';
import { ChevronDownIcon, ChevronRightIcon } from '@heroicons/react/24/solid';

const TreeNode = ({ label, children, onSelect }) => {
    const [isOpen, setIsOpen] = useState(false);

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

const DebugSection = () => {
    const [data, setData] = useState({ sessions: [] });
    const [selectedItem, setSelectedItem] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => {
        const fetchData = async () => {
            setIsLoading(true);
            try {
                const response = await fetch('http://nzxt.local:8003/transcripts');
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                const result = await response.json();
                setData(result);
                setIsLoading(false);
            } catch (e) {
                console.error("An error occurred while fetching the data: ", e);
                setError(e.message);
                setIsLoading(false);
            }
        };

        fetchData();
    }, []);

    const renderTree = (data) => {
        return (
            <div>
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
        );
    };

    const renderDetail = () => {
        if (!selectedItem) return <div>Select an item to view details</div>;

        let item;
        if (selectedItem.type === 'session') {
            item = data.sessions.find(s => s.id === selectedItem.id);
            return <div>Session ID: {item.id}</div>;
        } else if (selectedItem.type === 'message') {
            item = data.sessions.flatMap(s => s.messages).find(m => m.id === selectedItem.id);
            return <div>Message: {item.content}</div>;
        } else if (selectedItem.type === 'request') {
            item = data.sessions.flatMap(s => s.messages).flatMap(m => m.llmRequests).find(r => r.id === selectedItem.id);
            return (
                <div>
                    <h3>Request: {item.id}</h3>
                    <p><strong>Prompt:</strong> {item.prompt}</p>
                    <p><strong>Response:</strong> {item.response}</p>
                </div>
            );
        }
    };

    if (isLoading) return <div>Loading...</div>;
    if (error) return <div>Error: {error}</div>;

    return (
        <div className="flex h-full">
            <div className="w-1/3 overflow-auto border-r p-4 bg-gray-50">
                <h2 className="text-xl font-bold mb-4">Navigation</h2>
                {renderTree(data)}
            </div>
            <div className="w-2/3 p-4 overflow-auto">
                <h2 className="text-xl font-bold mb-4">Detail View</h2>
                {renderDetail()}
            </div>
        </div>
    );
};

export default DebugSection;