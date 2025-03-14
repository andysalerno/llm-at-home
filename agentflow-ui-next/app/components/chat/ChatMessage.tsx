'use client'

import { memo } from 'react';
import { Button, Card, CardHeader } from '@fluentui/react-components';
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
    userMessage: { backgroundColor: tokens.colorNeutralBackground1Hover },
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
                style={{ maxWidth: '100%' }}
                className={mergeClasses(
                    'cursor-pointer max-w-full',
                    isUser
                        ? mergeClasses('hover:shadow-lg', classes.userMessage)
                        : mergeClasses('hover:shadow-md', classes.botMessageColor)
                )}
                onClick={() => message.id && onClick?.(message.id)}
            >
                <CardHeader
                    className='max-w-full'
                    style={{ maxWidth: '100%' }}
                    description={
                        // <Body1
                        //     as='div'
                        //     styles={{ root: { maxWidth: '100%' } }}
                        //     className='max-w-full'>
                        //     {/* <div className="p-2 max-w-full">{message.content}</div> */}
                        // </Body1>
                        <div className="p-2 max-w-full TextMessage" style={{ maxWidth: '100%' }} dangerouslySetInnerHTML={{ __html: marked.parse(message.content, { gfm: true }) }}></div>
                    } />
            </Card>
            {onDelete && (
                <Button
                    icon={<Delete24Regular />}
                    appearance="subtle"
                    className={mergeClasses(
                        'absolute top-0 right-0 opacity-0 group-hover:opacity-100'
                    )}
                    onClick={() => onDelete(message.id)}
                    aria-label="Delete message"
                />
            )}
        </div>
    );
});