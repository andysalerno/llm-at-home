import { DebugSession } from '../../types';

export class DebugAPI {
    private static instance: DebugAPI;

    private constructor() { }

    public static getInstance(): DebugAPI {
        if (!DebugAPI.instance) {
            DebugAPI.instance = new DebugAPI();
        }
        return DebugAPI.instance;
    }

    async getSessions(): Promise<DebugSession[]> {
        const response = await fetch('/api/debug-data');
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const data = await response.json();
        return data.sessions;
    }
}