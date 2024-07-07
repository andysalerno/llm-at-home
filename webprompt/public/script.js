document.addEventListener('DOMContentLoaded', () => {
    const textArea = document.getElementById('textArea');
    const sendButton = document.getElementById('sendButton');

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

        try {
            const response = await fetch('http://your-local-llm-api-endpoint/v1/chat/completions', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    model: 'your-model-name',
                    messages: [{ role: 'user', content: userInput }],
                    stream: true,
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
                        if (jsonData.choices && jsonData.choices[0].delta.content) {
                            accumulatedResponse += jsonData.choices[0].delta.content;
                            updateTextArea(accumulatedResponse);
                        }
                    }
                }
            }
        } catch (error) {
            console.error('Error:', error);
            textArea.value += '\n\nAn error occurred while fetching the response.';
        }
    }

    function updateTextArea(newContent) {
        const userInput = textArea.value;
        const formattedResponse = marked.parse(newContent);
        textArea.value = `${userInput}\n\n<div class="llm-response">${formattedResponse}</div>`;
        textArea.scrollTop = textArea.scrollHeight;
    }
});