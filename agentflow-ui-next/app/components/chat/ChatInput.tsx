'use client'

import { useState, useRef, useEffect } from 'react';
import {
    Input,
    Button,
    mergeClasses,
    // tokens
} from '@fluentui/react-components';
import {
    Send24Regular,
    Stop24Regular,
    BinRecycle20Regular
} from '@fluentui/react-icons';

interface ChatInputProps {
    onSubmit: (message: string) => void;
    onCancel?: () => void;
    onClear?: () => void;
    disabled?: boolean;
    className?: string;
    placeholder?: string;
}

export function ChatInput({
    onSubmit,
    onCancel,
    onClear,
    disabled = false,
    className,
    placeholder = "Type your message here..."
}: ChatInputProps) {
    const [message, setMessage] = useState('');
    const inputRef = useRef<HTMLInputElement>(null);
    const isStreaming = disabled;

    // Auto focus input on mount
    useEffect(() => {
        inputRef.current?.focus();
    }, []);

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        if (message.trim() && !disabled) {
            onSubmit(message);
            setMessage('');
        }
    };

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSubmit(e);
        }
    };

    return (
        <form
            onSubmit={handleSubmit}
            className={mergeClasses(
                'p-4 bg-card border-t border-divider',
                className
            )}
        >
            <div className="flex gap-2">
                <Input
                    ref={inputRef}
                    value={message}
                    onChange={(e) => setMessage(e.target.value)}
                    onKeyDown={handleKeyDown}
                    placeholder={placeholder}
                    disabled={disabled}
                    className="flex-1"
                    appearance="filled-darker"
                    size="large"
                />
                {isStreaming ? (
                    <Button
                        icon={<Stop24Regular />}
                        appearance="primary"
                        onClick={onCancel}
                        title="Stop generating"
                    >
                        Stop
                    </Button>
                ) : (
                    <Button
                        icon={<Send24Regular />}
                        appearance="primary"
                        type="submit"
                        disabled={!message.trim() || disabled}
                        title="Send message"
                    >
                        Send
                    </Button>
                )
                }
                <Button
                    icon={<BinRecycle20Regular />}
                    type="submit"
                    title="Send message"
                    onClick={onClear}
                >
                    Clear
                </Button>
            </div>
        </form>
    );
}