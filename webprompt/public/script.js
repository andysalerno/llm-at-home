document.addEventListener('DOMContentLoaded', () => {
    const textArea = document.getElementById('textArea');
    const sendButton = document.getElementById('sendButton');
    const apiEndpointInput = document.getElementById('apiEndpoint');
    const stopStringInput = document.getElementById('stopString');

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
                    max_tokens: 150,
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
                        if (jsonData.content) {
                            accumulatedResponse += jsonData.content;
                            updateTextArea(textBeforeCursor, accumulatedResponse, textAfterCursor);
                        }
                    }
                }
            }
        } catch (error) {
            console.error('Error:', error);
            textArea.value += '\n\nAn error occurred while fetching the response.';
        }
    }

    function updateTextArea(beforeText, newContent, afterText) {
        const formattedResponse = `<span class="llm-response">${newContent}</span>`;
        textArea.value = `${beforeText}${formattedResponse}${afterText}`;
        textArea.scrollTop = textArea.scrollHeight;
    }
});