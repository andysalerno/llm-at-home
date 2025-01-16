import { Message } from '../../types';
import { AppConfig } from '../../types/config';

export class ChatAPI {
    private static instance: ChatAPI;
    private baseUrl: string;

    private constructor() {
        this.baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:8003';
    }

    public static getInstance(): ChatAPI {
        if (!ChatAPI.instance) {
            ChatAPI.instance = new ChatAPI();
        }
        return ChatAPI.instance;
    }

    async streamCompletion(
        messages: Message[],
        config: Partial<AppConfig>,
        onChunk: (chunk: string) => void
    ): Promise<Response> {
        const response = await fetch(`${this.baseUrl}/v1/chat/completions`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                messages: messages.map(({ role, content }) => ({ role, content })),
                ...config,
            }),
        });

        if (!response.body) {
            throw new Error('No response body received');
        }

        const reader = response.body
            .pipeThrough(new TextDecoderStream())
            .getReader();

        while (true) {
            const { done, value } = await reader.read();
            if (done) break;

            const lines = value.split('\n').filter(line => line.trim());
            for (const line of lines) {
                if (line.startsWith('data: ')) {
                    const data = line.slice(6);
                    try {
                        const parsed = JSON.parse(data);
                        const content = parsed.choices[0].delta.content;
                        if (content) {
                            onChunk(content);
                        }
                    } catch (error) {
                        console.error('Error parsing SSE data:', error);
                    }
                }
            }
        }

        return response;
    }
}