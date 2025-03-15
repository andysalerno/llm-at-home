'use client'

import React, { useState, useEffect } from 'react';
import {
    Button,
    Text,
    Spinner,
    Card,
    // CardHeader,
    makeStyles,
    tokens,
    mergeClasses,
} from '@fluentui/react-components';
import {
    ChevronDown24Regular,
    ChevronRight24Regular
} from '@fluentui/react-icons';

interface LlmRequest {
    parentIncomingRequestId: string;
    input: LlmRequestInput[];
    output: string;
}

interface LlmRequestInput {
    role: string;
    content: string;
}

interface Message {
    incomingRequestId: string;
    content: string;
    conversationId: string;
    llmRequests: LlmRequest[];
}

interface Conversation {
    conversationId: string;
    messages: Message[];
}

interface DebugData {
    conversations: Conversation[];
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
    debugBackground: { backgroundColor: tokens.colorNeutralBackground1 },
});

const TreeNode: React.FC<TreeNodeProps> = ({ label, children, forceOpen, onSelect }) => {
    const [isOpen, setIsOpen] = useState<boolean>(forceOpen);

    return (
        <div className="my-0.5">
            <Button
                appearance="subtle"
                className="w-full flex items-center justify-start p-1 text-left h-6 overflow-hidden"
                onClick={() => {
                    setIsOpen(!isOpen);
                    if (onSelect) onSelect();
                }}
            >
                {children && (
                    isOpen ?
                        <ChevronDown24Regular className="w-4 h-4 mr-1 text-blue-600 flex-shrink-0" /> :
                        <ChevronRight24Regular className="w-4 h-4 mr-1 text-blue-600 flex-shrink-0" />
                )}
                <span className="truncate text-xs overflow-hidden whitespace-nowrap flex-1 flex items-center">{label}</span>
            </Button>
            {isOpen && children && (
                <div className="pl-3 ml-1">
                    {children}
                </div>
            )}
        </div>
    );
};

const DebugSection: React.FC<DebugSectionProps> = ({ focusedMessageId }) => {
    const [data, setData] = useState<DebugData>({ conversations: [] });
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

            // Find the message in our data structure
            for (let sessionIndex = 0; sessionIndex < data.conversations.length; sessionIndex++) {
                const session = data.conversations[sessionIndex];

                for (let messageIndex = 0; messageIndex < session.messages.length; messageIndex++) {
                    const message = session.messages[messageIndex];

                    if (message.incomingRequestId === focusedMessageId) {
                        console.log(`setting focused to message with id: ${focusedMessageId}`);

                        // Create composite ID for this message
                        const messageId = generateUniqueId('message', session.conversationId, message.incomingRequestId, messageIndex);
                        setSelectedItem({ type: 'message', id: messageId });
                        return;
                    }
                }
            }
        }
    }, [focusedMessageId, data]);

    const renderDetail = () => {
        if (!selectedItem) return (
            <Text>Select an item to view details</Text>
        );

        // Parse the composite ID to get its components
        const parsedId = parseUniqueId(selectedItem.id);

        let item;
        if (parsedId.type === 'session') {
            item = data.conversations.find(s => s.conversationId === parsedId.id);
            return (
                <Card className="p-4">
                    {/* <CardHeader>
                        <Text size={500} weight="semibold">Session Details</Text>
                    </CardHeader> */}
                    <Text>Session ID: {item?.conversationId}</Text>
                </Card>
            );
        } else if (parsedId.type === 'message') {
            // First find the correct session using the parentId
            const session = data.conversations.find(s => s.conversationId === parsedId.parentId);
            // Then get the message from that session
            item = session?.messages[parsedId.index] ||
                data.conversations
                    .flatMap(s => s.messages)
                    .find(m => m.incomingRequestId === parsedId.id);

            return (
                <Card className="p-4">
                    {/* <CardHeader>
                        <Text size={500} weight="semibold">Message Details</Text>
                    </CardHeader> */}
                    <Text>Messagez: {item?.content}</Text>
                </Card>
            );
        } else if (parsedId.type === 'request') {
            // First find the correct message using the parentId
            const message = data.conversations
                .flatMap(s => s.messages)
                .find(m => m.incomingRequestId === parsedId.parentId && m.llmRequests.length > 0);
            // Then get the request from that message
            item = message?.llmRequests[parsedId.index] ||
                data.conversations
                    .flatMap(s => s.messages)
                    .flatMap(m => m.llmRequests)
                    .find(r => r.parentIncomingRequestId === parsedId.id);

            return (
                <Card className="p-4">
                    {/* <CardHeader>
                        <Text size={500} weight="semibold">Request Details</Text>
                    </CardHeader> */}
                    <div className="font-mono text-sm p-4 rounded-md shadow-inner overflow-auto whitespace-pre-wrap">
                        <Text weight="semibold">Request ID: {item?.parentIncomingRequestId}</Text>
                        <div className="mt-4">
                            <Text weight="semibold">Prompt:</Text>
                            {item?.input?.map((input, i) => (
                                <div key={i} className="mt-2">
                                    <strong>{input.role}:</strong> {input.content}
                                </div>
                            )) || <div className="mt-2">No input available</div>}
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

    // Create unique IDs by combining parent IDs with the item's own ID
    const generateUniqueId = (type: string, parentId: string, itemId: string, index: number): string => {
        return `${type}:${parentId}:${itemId}:${index}`;
    };

    // Parse a composite ID back into its components
    const parseUniqueId = (compositeId: string): { type: string; id: string; parentId: string; index: number } => {
        const [type, parentId, id, indexStr] = compositeId.split(':');
        return {
            type,
            id,
            parentId,
            index: parseInt(indexStr, 10)
        };
    };

    return (
        <div className="flex h-full">
            <div className={mergeClasses("w-1/3 overflow-auto", classes.debugBackground)}>
                {data.conversations.map((session, sessionIndex) => {
                    const sessionId = generateUniqueId('session', 'root', session.conversationId, sessionIndex);

                    return (
                        <TreeNode
                            key={sessionId}
                            label={`${session.conversationId}`}
                            forceOpen={false}
                            onSelect={() => setSelectedItem({ type: 'session', id: sessionId })}
                        >
                            {session.messages.map((message, messageIndex) => {
                                const messageId = generateUniqueId('message', session.conversationId, message.incomingRequestId, messageIndex);

                                return (
                                    <TreeNode
                                        key={messageId}
                                        label={`${message.content.substring(0, 256)}...`}
                                        forceOpen={selectedItem?.type === 'message' && selectedItem.id === messageId}
                                        onSelect={() => setSelectedItem({ type: 'message', id: messageId })}
                                    >
                                        {message.llmRequests.map((request, requestIndex) => {
                                            const requestId = generateUniqueId('request', message.incomingRequestId, request.parentIncomingRequestId, requestIndex);

                                            return (
                                                <TreeNode
                                                    key={requestId}
                                                    label={`${requestId}`}
                                                    forceOpen={false}
                                                    onSelect={() => setSelectedItem({ type: 'request', id: requestId })}
                                                />
                                            );
                                        })}
                                    </TreeNode>
                                );
                            })}
                        </TreeNode>
                    );
                })}
            </div>
            <div className="w-2/3 p-4 overflow-auto">
                <div className="h-[calc(100%-2rem)]">
                    {renderDetail()}
                </div>
            </div>
        </div>
    );
};

export default DebugSection;