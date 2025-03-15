'use client'

import React, { useState } from 'react';
import {
    Text,
    Slider,
    Input,
    Textarea,
    Combobox,
    Option,
    Label,
} from '@fluentui/react-components';
import { useConfig } from '@/app/hooks/useConfig';

const ConfigSection: React.FC = () => {
    const { config, updateConfig } = useConfig();
    const [temperature, setTemperature] = useState<number>(0.7);
    const [maxTokens, setMaxTokens] = useState<number>(2048);
    const [stopTokens, setStopTokens] = useState<string[]>([]);

    const handleStopTokensChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
        const tokens = e.target.value.split(',').map(token => token.trim());
        setStopTokens(tokens.filter(token => token !== ''));
        updateConfig({ apiEndpoint: tokens[0] })
    };

    return (
        <div className='h-full p-6'>
            <Text size={600} weight="semibold">Configuration</Text>
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
                    <Combobox
                        placeholder='Select a prompt strategy plz'
                    >
                        {["a", "b", "c"].map((option) => (
                            <Option key={option}>
                                {option}
                            </Option>
                        ))}
                    </Combobox>
                </div>

                <div>
                    <Label className="block mb-2">
                        Max Tokens: {maxTokens}
                    </Label>
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
                    <Label className="block mb-2">
                        Stop Sequences
                    </Label>
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

                <div>
                    <Label className="block mb-2">
                        Chat Completions Endpoint
                    </Label>
                    <Input
                        value={config.apiEndpoint}
                        onChange={(e) => updateConfig({ apiEndpoint: e.target.value })}
                        placeholder="Enter api endpoint"
                        className="w-full"
                    />
                </div>
            </div>
        </div >
    );
};

export default ConfigSection;