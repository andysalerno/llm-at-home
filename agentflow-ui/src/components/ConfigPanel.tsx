import React from 'react';

interface ConfigPanelProps {
    temperature: number;
    setTemperature: (value: number) => void;
    maxTokens: number;
    setMaxTokens: (value: number) => void;
    stopTokens: string[];
    setStopTokens: (value: string[]) => void;
}

const ConfigPanel: React.FC<ConfigPanelProps> = ({
    temperature,
    setTemperature,
    maxTokens,
    setMaxTokens,
    stopTokens,
    setStopTokens,
}) => {
    const handleStopTokensChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
        const tokens = e.target.value.split(',').map(token => token.trim());
        setStopTokens(tokens);
    };

    return (
        <div className="p-4 rounded-lg shadow">
            <h2 className="text-lg font-semibold mb-4">Configuration</h2>

            <div className="mb-4">
                <label className="block text-sm font-medium">Temperature: {temperature}</label>
                <input
                    type="range"
                    min="0"
                    max="1"
                    step="0.1"
                    value={temperature}
                    onChange={(e) => setTemperature(parseFloat(e.target.value))}
                    className="w-full"
                />
            </div>

            <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700">Max Tokens: {maxTokens}</label>
                <input
                    type="number"
                    value={maxTokens}
                    onChange={(e) => setMaxTokens(parseInt(e.target.value))}
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-300 focus:ring focus:ring-indigo-200 focus:ring-opacity-50"
                />
            </div>

            <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700">Stop Tokens (comma-separated):</label>
                <textarea
                    value={stopTokens.join(', ')}
                    onChange={handleStopTokensChange}
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-300 focus:ring focus:ring-indigo-200 focus:ring-opacity-50"
                    rows={3}
                />
            </div>
        </div>
    );
};

export default ConfigPanel;