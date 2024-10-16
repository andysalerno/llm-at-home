import React, { useState, useEffect } from 'react';
import { ChevronDownIcon, ChevronRightIcon } from '@heroicons/react/24/solid';

interface TreeNodeProps {
    label: string;
    children: any;
    onSelect: () => void;
}

function expect<T>(value: T | undefined, message = "Value was undefined"): T {
    if (value === undefined) {
        throw new Error(message);
    }
    return value;
}

const TreeNode: React.FC<TreeNodeProps> = ({ label, children, onSelect }) => {
    const [isOpen, setIsOpen] = useState(false);

    return (
        <div>
            <div
                className="flex items-center cursor-pointer hover:bg-blue-100 p-2 rounded transition-colors duration-0 ease-in-out"
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

interface LlmRequest {
    id: string;
    input: string;
    output: string;
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

interface SelectedItem {
    type: 'session' | 'message' | 'request';
    id: string;
}

const DebugSection: React.FC<DebugSectionProps> = ({ focusedMessageId }) => {
    const [data, setData] = useState<DebugData>({ sessions: [] });
    const [selectedItem, setSelectedItem] = useState<SelectedItem | null>(null);
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

    useEffect(() => {
        if (focusedMessageId) {
            // const message = data.sessions.flatMap(s => s.messages).find(m => m.correlationId === focusedMessageId);
            setSelectedItem({ type: 'message', id: focusedMessageId });
        }
    }, [focusedMessageId, data]);

    const renderTree = (data: DebugData) => {
        return (
            <div>
                {data.sessions.map(session => (
                    <TreeNode
                        label={`Session: ${session.id}`}
                        onSelect={() => setSelectedItem({ type: 'session', id: session.id })}
                    >
                        {session.messages.map(message => (
                            <TreeNode
                                label={`Message: ${message.content.substring(0, 20)}...`}
                                onSelect={() => setSelectedItem({ type: 'message', id: message.id })}
                            >
                                {message.llmRequests.map(request => (
                                    <TreeNode
                                        children={[]}
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
        if (!selectedItem) return <div className="text-gray-500 italic">Select an item to view details</div>;

        let content = '';
        if (selectedItem.type === 'session') {
            let item = data.sessions.find(s => s.id === selectedItem.id);
            item = expect(item);
            content = `Session ID: ${item.id}\n\nMessages: ${item.messages.length}\nTotal Requests: ${item.messages.reduce((total, msg) => total + msg.llmRequests.length, 0)}`;
        } else if (selectedItem.type === 'message') {
            let item = data.sessions.flatMap(s => s.messages).find(m => m.id === selectedItem.id);
            item = expect(item);
            content = `Message ID: ${item.id}\nCorrelation ID:${item.correlationId}\n\n${item.content}`;
        } else if (selectedItem.type === 'request') {
            let item = data.sessions.flatMap(s => s.messages).flatMap(m => m.llmRequests).find(r => r.id === selectedItem.id);
            item = expect(item);
            content = `Request ID: ${item.id}\n\n\n${item.input}${item.output}`;
        }

        return (
            <pre className="font-mono text-sm bg-gray-100 p-4 rounded-md shadow-inner h-full overflow-auto whitespace-pre-wrap">
                {content}
            </pre>
        );
    };

    if (isLoading) return <div>Loading...</div>;
    if (error) return <div>Error: {error}</div>;

    return (
        <div className="flex h-full">
            <div className="w-1/3 overflow-auto border-r p-4 bg-gray-50">
                <h2 className="text-xl font-bold mb-4">Navigation</h2>
                {renderTree(data)}
            </div>
            <div className="w-2/3 p-4 overflow-auto bg-white">
                <h2 className="text-xl font-bold mb-4">Detail View</h2>
                <div className="h-[calc(100%-2rem)] bg-gray-100 rounded-md shadow-inner">
                    {renderDetail()}
                </div>
            </div>
        </div>
    );
};

export default DebugSection;