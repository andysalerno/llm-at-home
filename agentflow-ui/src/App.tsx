import React, { useCallback, useState } from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import Sidebar from './components/Sidebar';
import ChatSection from './components/ChatSection';
import DebugSection from './components/DebugSection';
import SplitView from './components/SplitView';
import TextCompletionSection from './components/TextCompletionSection';

const App: React.FC = () => {
    const [focusedMessageId, setFocusedMessageId] = useState<string>('');
    const [isSplitViewVisible, setIsSplitViewVisible] = useState<boolean>(false);

    const setAndOpenMessageId = useCallback((id: string) => {
        setIsSplitViewVisible(true);
        setFocusedMessageId(id);
    }, []);

    return (
        <Router>
            <div className="flex h-screen bg-gray-100">
                <Sidebar />
                <div className={`flex-1 flex flex-col overflow-hidden transition-all duration-0 ml-20`}>
                    <main className="flex-1 overflow-x-hidden overflow-y-auto bg-gray-200">
                        <div className="mx-auto h-full">
                            <Routes>
                                <Route path="/" element={
                                    <SplitView
                                        left={<ChatSection setFocusedMessageId={setAndOpenMessageId} />}
                                        right={<DebugSection focusedMessageId={focusedMessageId} />}
                                        isSplitVisible={isSplitViewVisible}
                                        setIsSplitVisible={setIsSplitViewVisible}
                                    />} />
                                <Route path="/debug" element={<DebugSection focusedMessageId={focusedMessageId} />} />
                                <Route path="/text" element={<TextCompletionSection />} />
                            </Routes>
                        </div>
                    </main>
                </div>
            </div>
        </Router>
    );
};

export default App;