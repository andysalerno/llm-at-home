import React, { useState } from 'react';

const TextEditor = () => {
    const [text, setText] = useState('');

    return (
        <div className="mt-8">
            <textarea
                className="w-full h-[calc(100vh-12rem)] p-4 border rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={text}
                onChange={(e) => setText(e.target.value)}
                placeholder="Start typing here..."
            />
        </div>
    );
};

export default TextEditor;