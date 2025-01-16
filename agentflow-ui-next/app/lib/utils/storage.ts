// src/lib/utils/storage.ts
export class StorageManager {
    static getItem<T>(key: string, defaultValue: T): T {
        if (typeof window === 'undefined') {
            return defaultValue;
        }

        try {
            const item = window.localStorage.getItem(key);
            return item ? JSON.parse(item) : defaultValue;
        } catch (error) {
            console.error(`Error reading from localStorage: ${error}`);
            return defaultValue;
        }
    }

    static setItem(key: string, value: unknown): void {
        if (typeof window === 'undefined') {
            return;
        }

        try {
            window.localStorage.setItem(key, JSON.stringify(value));
        } catch (error) {
            console.error(`Error writing to localStorage: ${error}`);
        }
    }
}

// src/lib/utils/stream.ts
export interface StreamProcessor {
    processChunk: (chunk: string) => void;
    onError?: (error: Error) => void;
    onComplete?: () => void;
}

export async function processStream(
    response: Response,
    { processChunk, onError, onComplete }: StreamProcessor
): Promise<void> {
    if (!response.body) {
        throw new Error('No response body');
    }

    const reader = response.body
        .pipeThrough(new TextDecoderStream())
        .getReader();

    try {
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
                            processChunk(content);
                        }
                    } catch (error) {
                        console.error('Error parsing SSE data:', error);
                        if (error instanceof Error && onError) {
                            onError(error);
                        }
                    }
                }
            }
        }

        onComplete?.();
    } finally {
        reader.releaseLock();
    }
}