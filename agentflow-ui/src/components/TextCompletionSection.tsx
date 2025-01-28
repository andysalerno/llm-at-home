import React, { useState, useCallback } from 'react';
import ConfigPanel from './ConfigPanel';
import SplitView from './SplitView';

const TextCompletionSection: React.FC = ({ }) => {
    const [inputText, setInputText] = useState<string>('');
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [temperature, setTemperature] = useState<number>(0.7);
    const [maxTokens, setMaxTokens] = useState<number>(100);
    const [stopTokens, setStopTokens] = useState<string[]>([]);

    const handleInputChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
        setInputText(e.target.value);
    };

    const handleSubmit = useCallback(async (e: React.FormEvent) => {
        e.preventDefault();
        if (inputText.trim() === '') return;

        setIsLoading(true);

        try {
            const response = await fetch(
                'http://nzxt.local:8000/completion',
                {
                    method: 'POST',
                    headers: { "X-Correlation-ID": 'fake' },
                    body: JSON.stringify({
                        prompt: inputText,
                        model: "model",
                        max_tokens: 256,
                        stream: false
                    })
                });

            const body = await response.json();

            setInputText(inputText + body.content);
        } catch (error) {
            console.error('Error sending text completion request:', error);
            // You might want to show an error message to the user here
        } finally {
            setIsLoading(false);
        }
    }, [inputText]);

    const TextInputArea = (
        <div className="flex flex-col h-full p-4">
            <textarea
                value={inputText}
                onChange={handleInputChange}
                placeholder="Enter your text here..."
                className="flex-1 p-4 border border-gray-300 rounded-t-md focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
                disabled={isLoading}
            />
            <button
                onClick={handleSubmit}
                className="w-full px-4 py-2 bg-blue-500 text-white rounded-b-md hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-blue-300"
                disabled={isLoading}
            >
                {isLoading ? 'Generating...' : 'Complete Text'}
            </button>
        </div>
    );

    const ConfigPanelWrapper = (
        <ConfigPanel
            temperature={temperature}
            setTemperature={setTemperature}
            maxTokens={maxTokens}
            setMaxTokens={setMaxTokens}
            stopTokens={stopTokens}
            setStopTokens={setStopTokens}
        />
    );

    return (
        <SplitView
            left={TextInputArea}
            right={ConfigPanelWrapper}
            isSplitVisible={null}
            setIsSplitVisible={null}
        />
    );

};

export default TextCompletionSection;