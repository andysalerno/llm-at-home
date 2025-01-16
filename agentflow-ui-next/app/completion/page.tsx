'use client'

import { useState } from 'react'
import AppShell from '../components/layout/AppShell'
import TextCompletionSection from '../components/completion/TextCompletionSection'
import SplitPane from '../components/layout/SplitPane'
import ConfigPanel from '../components/completion/ConfigPanel'

export default function TextCompletion() {
    const [temperature, setTemperature] = useState<number>(0.7);
    const [maxTokens, setMaxTokens] = useState<number>(2048);
    const [stopTokens, setStopTokens] = useState<string[]>([]);

    const handleCompletion = (completedText: string) => {
        // Handle completed text if needed
        console.log('Completion finished:', completedText);
    };

    return (
        <AppShell>
            <SplitPane
                left={
                    <TextCompletionSection
                        onCompletion={handleCompletion}
                    />
                }
                right={
                    <ConfigPanel
                        temperature={temperature}
                        setTemperature={setTemperature}
                        maxTokens={maxTokens}
                        setMaxTokens={setMaxTokens}
                        stopTokens={stopTokens}
                        setStopTokens={setStopTokens}
                    />
                }
            />
        </AppShell>
    )
}