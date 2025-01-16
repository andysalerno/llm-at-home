export interface Message {
    id: string;
    role: 'user' | 'assistant' | 'system';
    content: string;
    timestamp: string;
    correlationId: string;
}

export interface ChatState {
    messages: Message[];
    isLoading: boolean;
    error: string | null;
}

export interface ChatAPI {
    sendMessage: (message: string) => Promise<void>;
    deleteMessage: (id: string) => void;
    clearMessages: () => void;
}

export interface CompletionState {
    text: string;
    isLoading: boolean;
    error: string | null;
}

export interface DebugSession {
    id: string;
    messages: Message[];
    timestamp: string;
}