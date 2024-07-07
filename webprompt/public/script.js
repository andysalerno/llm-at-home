document.addEventListener('DOMContentLoaded', () => {
    const textArea = document.getElementById('textArea');
    const sendButton = document.getElementById('sendButton');
    const apiEndpointInput = document.getElementById('apiEndpoint');
    const stopStringInput = document.getElementById('stopString');
    const userPrefixAutoAdd = document.getElementById('userPrefixAutoAdd');

    // Load saved values from localStorage
    apiEndpointInput.value = localStorage.getItem('apiEndpoint') || '';
    stopStringInput.value = localStorage.getItem('stopString') || '';
    userPrefixAutoAdd.value = localStorage.getItem('userPrefixAutoAdd') || '';

    // Save values to localStorage when they change
    apiEndpointInput.addEventListener('change', () => {
        localStorage.setItem('apiEndpoint', apiEndpointInput.value);
    });

    stopStringInput.addEventListener('change', () => {
        localStorage.setItem('stopString', stopStringInput.value);
    });

    userPrefixAutoAdd.addEventListener('change', () => {
        localStorage.setItem('userPrefixAutoAdd', userPrefixAutoAdd.value);
    });

    sendButton.addEventListener('click', sendMessage);
    textArea.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && e.ctrlKey) {
            e.preventDefault();
            sendMessage();
        }
    });

    async function sendMessage() {
        const userInput = textArea.value;
        if (!userInput.trim()) return;

        const apiEndpoint = apiEndpointInput.value.trim();
        if (!apiEndpoint) {
            alert('Please enter an API endpoint URI');
            return;
        }

        const stopString = stopStringInput.value.trim();

        const cursorPosition = textArea.selectionStart;
        const textBeforeCursor = userInput.substring(0, cursorPosition);
        const textAfterCursor = userInput.substring(cursorPosition);

        try {
            const response = await fetch(apiEndpoint, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    model: 'gpt-4',
                    prompt: textBeforeCursor,
                    max_tokens: 512,
                    stream: true,
                    stop: stopString ? [stopString] : undefined,
                }),
            });

            if (!response.ok) {
                throw new Error('API request failed');
            }

            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let accumulatedResponse = '';

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                const chunk = decoder.decode(value);
                const lines = chunk.split('\n');

                for (const line of lines) {
                    if (line.startsWith('data:')) {
                        const jsonData = JSON.parse(line.slice(5));
                        if (jsonData.stop === false && jsonData.content) {
                            accumulatedResponse += jsonData.content;
                            updateTextArea(textBeforeCursor, accumulatedResponse, textAfterCursor);
                        }
                    }
                }
            }
        } catch (error) {
            console.error('Error:', error);
            textArea.value += '\n\n<error>';
        }

        appendTextArea(stopString);
        appendTextArea(userPrefixAutoAdd.value.replace("\\n", "\n"));
    }

    function updateTextArea(beforeText, newContent, afterText) {
        textArea.value = `${beforeText}${newContent}${afterText}`;
        textArea.scrollTop = textArea.scrollHeight;
    }

    function appendTextArea(text) {
        textArea.value = `${textArea.value}${text}`;
        textArea.scrollTop = textArea.scrollHeight;
    }
});