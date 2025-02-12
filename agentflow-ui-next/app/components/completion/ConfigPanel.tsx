'use client'

import React from 'react';
import {
    Card,
    CardHeader,
    Text,
    Slider,
    Input,
    Textarea,
} from '@fluentui/react-components';

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
        <Card className="h-full">
            <CardHeader>
                <Text size={600} weight="semibold">Configuration</Text>
            </CardHeader>
            <div className="p-6 space-y-6">
                <div>
                    <Text weight="medium" className="block mb-2">
                        Temperature: {temperature.toFixed(1)}
                    </Text>
                    <Slider
                        min={0}
                        max={2}
                        step={0.1}
                        value={temperature}
                        onChange={(_, data) => setTemperature(data.value)}
                    />
                    <div className="flex justify-between text-sm text-gray-500 mt-1">
                        <Text>Precise</Text>
                        <Text>Creative</Text>
                    </div>
                </div>

                <div>
                    <Text weight="medium" className="block mb-2">
                        Max Tokens: {maxTokens}
                    </Text>
                    <Input
                        type="number"
                        value={maxTokens.toString()}
                        onChange={(e) => setMaxTokens(parseInt(e.target.value))}
                        min={1}
                        max={4000}
                        className="w-full"
                    />
                </div>

                <div>
                    <Text weight="medium" className="block mb-2">
                        Stop Sequences
                    </Text>
                    <Textarea
                        value={stopTokens.join(', ')}
                        onChange={handleStopTokensChange}
                        placeholder="Enter comma-separated stop sequences"
                        className="w-full"
                        rows={3}
                    />
                    <Text size={200} className="mt-1 text-gray-500">
                        Comma-separated list of sequences where the API will stop generating further tokens
                    </Text>
                </div>
            </div>
        </Card>
    );
};

export default ConfigPanel;