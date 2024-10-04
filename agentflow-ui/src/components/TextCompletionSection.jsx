import React, { useState, useCallback } from 'react';

const TextCompletionSection = ({ onCompletion }) => {
    const [inputText, setInputText] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    const handleInputChange = (e) => {
        setInputText(e.target.value);
    };

    const handleSubmit = useCallback(async (e) => {
        e.preventDefault();
        if (inputText.trim() === '') return;

        setIsLoading(true);

        try {
            const response = await fetch(
                'http://nzxt.local:8003/completion',
                {
                    method: 'POST',
                    headers: { "X-Correlation-ID": correlationId },
                    body: JSON.stringify({
                        messages: [...messages, userMessage].map(msg => ({ role: msg.role, content: msg.content })),
                        model: "gpt-3.5-turbo",
                        stream: true
                    })
                });

            const completedText = inputText + response.data.completion;
            setInputText(completedText);

            if (onCompletion) {
                onCompletion(completedText);
            }
        } catch (error) {
            console.error('Error sending text completion request:', error);
            // You might want to show an error message to the user here
        } finally {
            setIsLoading(false);
        }
    }, [inputText, onCompletion]);

    return (
        <div className="flex flex-col h-full bg-gray-100 p-4">
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
};

export default TextCompletionSection;