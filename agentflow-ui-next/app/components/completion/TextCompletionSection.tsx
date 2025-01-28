'use client'

import React, { useState, useCallback } from 'react';

interface TextCompletionSectionProps {
    onCompletion?: (text: string) => void;
}

const TextCompletionSection: React.FC<TextCompletionSectionProps> = ({ onCompletion }) => {
    const [inputText, setInputText] = useState<string>('');
    const [isLoading, setIsLoading] = useState<boolean>(false);

    const handleInputChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
        setInputText(e.target.value);
    };

    const handleSubmit = useCallback(async (e: React.FormEvent) => {
        e.preventDefault();
        if (inputText.trim() === '') return;

        setIsLoading(true);

        try {
            const response = await fetch('/api/completion', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    prompt: inputText,
                }),
            });

            if (!response.body) throw new Error('No response body');

            const reader = response.body
                .pipeThrough(new TextDecoderStream())
                .getReader();

            let fullCompletion = inputText;

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                const lines = value.split('\n').filter(line => line.trim() !== '');

                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        const data = line.slice(6);
                        if (data === '[DONE]') {
                            if (onCompletion) {
                                onCompletion(fullCompletion);
                            }
                        } else {
                            try {
                                const parsed = JSON.parse(data);
                                const content = parsed.choices[0].delta.content;
                                if (content) {
                                    fullCompletion += content;
                                    setInputText(fullCompletion);
                                }
                            } catch (error) {
                                console.error('Error parsing SSE data:', error);
                            }
                        }
                    }
                }
            }
        } catch (error) {
            console.error('Error in text completion:', error);
            // You might want to show an error message to the user here
        } finally {
            setIsLoading(false);
        }
    }, [inputText, onCompletion]);

    return (
        <div className="flex flex-col h-full">
            <textarea
                value={inputText}
                onChange={handleInputChange}
                placeholder="Enter your text here..."
                className="flex-1 p-4 border border-gray-300 rounded-t-md focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none font-mono text-sm"
                disabled={isLoading}
                spellCheck={false}
            />
            <button
                onClick={handleSubmit}
                className="w-full px-4 py-2 bg-blue-500 rounded-b-md hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-blue-300 transition-colors"
                disabled={isLoading}
            >
                {isLoading ? 'Generating...' : 'Complete Text'}
            </button>
        </div>
    );
};

export default TextCompletionSection;