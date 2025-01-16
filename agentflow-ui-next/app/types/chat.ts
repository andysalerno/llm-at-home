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