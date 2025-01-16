'use client'

import { useState } from 'react'
import AppShell from '../components/layout/AppShell'
import TextCompletionSection from '../components/TextCompletionSection'
import SplitView from '../components/SplitView'
import ConfigPanel from '../components/ConfigPanel'

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
            <SplitView
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