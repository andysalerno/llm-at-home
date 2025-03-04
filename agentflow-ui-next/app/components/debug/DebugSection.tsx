'use client'

import React, { useState, useEffect } from 'react';
import {
    Button,
    Text,
    Spinner,
    Card,
    CardHeader,
    makeStyles,
    tokens,
    mergeClasses,
} from '@fluentui/react-components';
import {
    ChevronDown24Regular,
    ChevronRight24Regular
} from '@fluentui/react-icons';

interface LlmRequest {
    id: string;
    input: string;
    output: string;
}

interface Message {
    id: string;
    content: string;
    conversationId: string;
    llmRequests: LlmRequest[];
}

interface Session {
    conversationId: string;
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
    forceOpen: boolean;
    onSelect?: () => void;
}

const useStyles = makeStyles({
    debugBackground: { backgroundColor: tokens.colorNeutralBackground2 },
});

const TreeNode: React.FC<TreeNodeProps> = ({ label, children, forceOpen, onSelect }) => {
    const [isOpen, setIsOpen] = useState<boolean>(forceOpen);

    return (
        <div className="my-1">
            <Button
                appearance="subtle"
                className="w-1/2 flex p-2 rounded text-left"
                onClick={() => {
                    setIsOpen(!isOpen);
                    if (onSelect) onSelect();
                }}
            >
                {children && (
                    isOpen ?
                        <ChevronDown24Regular className="w-5 h-5 mr-2 text-blue-600" /> :
                        <ChevronRight24Regular className="w-5 h-5 mr-2 text-blue-600" />
                )}
                <Text>{label}</Text>
            </Button>
            {isOpen && children && (
                <div className="pl-4 ml-2">
                    {children}
                </div>
            )}
        </div>
    );
};

const DebugSection: React.FC<DebugSectionProps> = ({ focusedMessageId }) => {
    const [data, setData] = useState<DebugData>({ sessions: [] });
    const [selectedItem, setSelectedItem] = useState<{ type: string; id: string } | null>(null);
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);
    const classes = useStyles();

    console.log(`selectedItem is ${selectedItem?.type} ${selectedItem?.id}`);

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
            } catch (e) {
                console.error("An error occurred while fetching the data: ", e);
                setError(e instanceof Error ? e.message : String(e));
            } finally {
                setIsLoading(false);
            }
        };

        fetchData();
    }, []);

    useEffect(() => {
        if (focusedMessageId) {
            console.log(`looking for message with id: ${focusedMessageId}`);
            const message = data.sessions.flatMap(s => s.messages).find(m => m.id === focusedMessageId);
            if (message) {
                console.log(`setting focused to message with id: ${focusedMessageId}`);
                setSelectedItem({ type: 'message', id: message.id });
            }
        }
    }, [focusedMessageId, data]);

    const renderDetail = () => {
        if (!selectedItem) return (
            <Text>Select an item to view details</Text>
        );

        let item;
        if (selectedItem.type === 'session') {
            item = data.sessions.find(s => s.conversationId === selectedItem.id);
            return (
                <Card className="p-4">
                    <CardHeader>
                        <Text size={500} weight="semibold">Session Details</Text>
                    </CardHeader>
                    <Text>Session ID: {item?.conversationId}</Text>
                </Card>
            );
        } else if (selectedItem.type === 'message') {
            item = data.sessions.flatMap(s => s.messages).find(m => m.id === selectedItem.id);
            return (
                <Card className="p-4">
                    <CardHeader>
                        <Text size={500} weight="semibold">Message Details</Text>
                    </CardHeader>
                    <Text>Message: {item?.content}</Text>
                </Card>
            );
        } else if (selectedItem.type === 'request') {
            item = data.sessions.flatMap(s => s.messages).flatMap(m => m.llmRequests).find(r => r.id === selectedItem.id);
            return (
                <Card className="p-4">
                    <CardHeader>
                        <Text size={500} weight="semibold">Request Details</Text>
                    </CardHeader>
                    <div className="font-mono text-sm p-4 rounded-md shadow-inner overflow-auto whitespace-pre-wrap">
                        <Text weight="semibold">Request ID: {item?.id}</Text>
                        <div className="mt-4">
                            <Text weight="semibold">Prompt:</Text>
                            <div className="mt-2">{item?.input}</div>
                        </div>
                        <div className="mt-4">
                            <Text weight="semibold">Response:</Text>
                            <div className="mt-2">{item?.output}</div>
                        </div>
                    </div>
                </Card>
            );
        }
    };

    if (isLoading) return (
        <div className="flex items-center justify-center h-full">
            <Spinner size="large" />
        </div>
    );

    if (error) return (
        <div className={mergeClasses("h-full p-4 text-red-600", classes.debugBackground)}>
            Error: {error}
        </div>
    );

    return (
        <div className="flex h-full">
            <div className={mergeClasses("w-1/3 overflow-auto p-4", classes.debugBackground)}>
                <Text size={600} weight="semibold" className="mb-4 block">
                    Navigation
                </Text>
                {data.sessions.map(session => (
                    <TreeNode
                        key={session.conversationId}
                        label={`Session: ${session.conversationId}`}
                        forceOpen={false}
                        onSelect={() => setSelectedItem({ type: 'session', id: session.conversationId })}
                    >
                        {session.messages.map(message => (
                            <TreeNode
                                key={message.id}
                                label={`${message.content.substring(0, 20)}...`}
                                forceOpen={selectedItem?.type === 'message' && selectedItem.id === message.id}
                                onSelect={() => setSelectedItem({ type: 'message', id: message.id })}
                            >
                                {message.llmRequests.map(request => (
                                    <TreeNode
                                        key={request.id}
                                        label={`${request.id}`}
                                        forceOpen={false}
                                        onSelect={() => setSelectedItem({ type: 'request', id: request.id })}
                                    />
                                ))}
                            </TreeNode>
                        ))}
                    </TreeNode>
                ))}
            </div>
            <div className="w-2/3 p-4 overflow-auto">
                <Text size={600} weight="semibold" className="mb-4 block">
                    Detail View
                </Text>
                <div className="h-[calc(100%-2rem)]">
                    {renderDetail()}
                </div>
            </div>
        </div>
    );
};

export default DebugSection;