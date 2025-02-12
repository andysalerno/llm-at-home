import { DebugSession } from '../../types';

export class DebugAPI {
    private static instance: DebugAPI;
    private baseUrl: string;

    private constructor() {
        this.baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:8003';
    }

    public static getInstance(): DebugAPI {
        if (!DebugAPI.instance) {
            DebugAPI.instance = new DebugAPI();
        }
        return DebugAPI.instance;
    }

    async getSessions(): Promise<DebugSession[]> {
        const response = await fetch(`${this.baseUrl}/transcripts`);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const data = await response.json();
        return data.sessions;
    }
}