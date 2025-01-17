'use client'

import { memo } from 'react';
import { Button, Card, CardHeader } from '@fluentui/react-components';
import { Delete24Regular } from '@fluentui/react-icons';
import { Message } from '../../types';
import { mergeClasses } from '@fluentui/react-components';

interface ChatMessageProps {
    message: Message;
    onDelete?: (id: string) => void;
    onClick?: (id: string) => void;
}

export const ChatMessage = memo(function ChatMessage({
    message,
    onDelete,
    onClick
}: ChatMessageProps) {
    const isUser = message.role === 'user';

    return (
        <div
            className={mergeClasses(
                'relative max-w-[70%] mb-4 group',
                isUser ? 'ml-auto' : 'mr-auto'
            )}
        >
            <Card
                className={mergeClasses(
                    'cursor-pointer transition-shadow duration-200',
                    isUser
                        ? 'bg-brand-primary text-white hover:shadow-lg'
                        : 'bg-white hover:shadow-md'
                )}
                onClick={() => onClick?.(message.correlationId)}
            >
                <CardHeader>
                    {/* <div className="p-2">{message.content}</div> */}
                    {/* {message.content} */}
                    hi there
                </CardHeader>
            </Card>
            {onDelete && (
                <Button
                    icon={<Delete24Regular />}
                    appearance="subtle"
                    className={mergeClasses(
                        'absolute -top-2 -right-2 opacity-0 group-hover:opacity-100 transition-opacity duration-200',
                        isUser ? 'text-white' : 'text-neutral-600'
                    )}
                    onClick={() => onDelete(message.correlationId)}
                    aria-label="Delete message"
                />
            )}
        </div>
    );
});