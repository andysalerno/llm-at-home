'use client'

import { memo } from 'react';
import { Body1, Button, Card, CardHeader } from '@fluentui/react-components';
import { Delete24Regular } from '@fluentui/react-icons';
import { Message } from '../../types';
import { mergeClasses, makeStyles, tokens } from '@fluentui/react-components';
import { marked } from 'marked';

interface ChatMessageProps {
    message: Message;
    onDelete?: (id: string) => void;
    onClick?: (id: string) => void;
}

const useStyles = makeStyles({
    botMessageColor: { backgroundColor: tokens.colorBrandForeground1 },
});

export const ChatMessage = memo(function ChatMessage({
    message,
    onDelete,
    onClick
}: ChatMessageProps) {
    const classes = useStyles();
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
                    'cursor-pointer transition-shadow',
                    isUser
                        ? 'bg-brand-primary hover:shadow-lg'
                        : mergeClasses('hover:shadow-md', classes.botMessageColor)
                )}
                onClick={() => message.correlationId && onClick?.(message.correlationId)}
            >
                <CardHeader
                    header={
                        <Body1>
                            <div className="p-2" dangerouslySetInnerHTML={{ __html: marked.parse(message.content, { gfm: true }) }}></div>
                        </Body1>
                    } />
            </Card>
            {onDelete && (
                <Button
                    icon={<Delete24Regular />}
                    appearance="subtle"
                    className={mergeClasses(
                        'absolute -top-2 -right-2 opacity-0 group-hover:opacity-100 transition-opacity'
                    )}
                    onClick={() => onDelete(message.id)}
                    aria-label="Delete message"
                />
            )}
        </div>
    );
});