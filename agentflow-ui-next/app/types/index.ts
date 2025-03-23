export interface Message {
    id: string;
    role: 'user' | 'assistant' | 'system';
    content: string;
    timestamp: string;
    conversationId: string;
}

// Read-only static list of strings
export const INSTRUCTION_STRATEGIES =
    [
        'TopLevelSystemMessage',

        'InlineUserMessage',

        'InlineSystemMessage',

        'AppendedToUserMessage',

        'PrecedingLastUserMessage',
    ] as const;
