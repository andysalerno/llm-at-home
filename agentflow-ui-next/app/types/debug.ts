import { Message } from "./chat";
import { AppConfig } from "./config";

export interface LLMRequest {
    id: string;
    timestamp: string;
    prompt: string;
    response: string;
    config: Partial<AppConfig>;
}

export interface DebugSession {
    id: string;
    startTime: string;
    messages: Message[];
    llmRequests: LLMRequest[];
}