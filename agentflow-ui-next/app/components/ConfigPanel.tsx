'use client'

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
        setStopTokens(tokens.filter(token => token !== ''));
    };

    return (
        <div className="bg-white p-6 rounded-lg shadow-sm h-full">
            <h2 className="text-lg font-semibold mb-6">Configuration</h2>

            <div className="space-y-6">
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                        Temperature: {temperature.toFixed(1)}
                    </label>
                    <input
                        type="range"
                        min="0"
                        max="2"
                        step="0.1"
                        value={temperature}
                        onChange={(e) => setTemperature(parseFloat(e.target.value))}
                        className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer"
                    />
                    <div className="flex justify-between text-xs text-gray-500 mt-1">
                        <span>Precise</span>
                        <span>Creative</span>
                    </div>
                </div>

                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                        Max Tokens: {maxTokens}
                    </label>
                    <input
                        type="number"
                        value={maxTokens}
                        onChange={(e) => setMaxTokens(parseInt(e.target.value))}
                        min="1"
                        max="4000"
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
                    />
                </div>

                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                        Stop Sequences
                    </label>
                    <textarea
                        value={stopTokens.join(', ')}
                        onChange={handleStopTokensChange}
                        placeholder="Enter comma-separated stop sequences"
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
                        rows={3}
                    />
                    <p className="mt-1 text-xs text-gray-500">
                        Comma-separated list of sequences where the API will stop generating further tokens
                    </p>
                </div>
            </div>
        </div>
    );
};

export default ConfigPanel;