// src/types/index.ts

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

export interface AppConfig {
    temperature: number;
    maxTokens: number;
    stopSequences: string[];
    apiEndpoint: string;
}

export interface ThemeConfig {
    colorMode: 'light' | 'dark';
    density: 'comfortable' | 'compact';
}

export interface LLMRequest {
    id: string;
    timestamp: string;
    prompt: string;
    response: string;
    config: Partial<AppConfig>;
}