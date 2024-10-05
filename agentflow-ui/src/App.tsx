import React, { useState } from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import Sidebar from './components/Sidebar';
import ChatSection from './components/ChatSection';
import DebugSection from './components/DebugSection';
import SplitView from './components/SplitView';
import TextCompletionSection from './components/TextCompletionSection';

const App: React.FC = () => {
    const [sidebarOpen, setSidebarOpen] = useState<boolean>(true);

    return (
        <Router>
            <div className="flex h-screen bg-gray-100">
                <Sidebar isOpen={sidebarOpen} setIsOpen={setSidebarOpen} />
                <div className={`flex-1 flex flex-col overflow-hidden transition-all duration-0 ${sidebarOpen ? 'ml-64' : 'ml-20'}`}>
                    <main className="flex-1 overflow-x-hidden overflow-y-auto bg-gray-200">
                        <div className="mx-auto h-full">
                            <Routes>
                                <Route path="/" element={
                                    <SplitView
                                        left={<ChatSection onMessageClick={null} />}
                                        right={<DebugSection focusedMessageId={null} />} />} />
                                <Route path="/debug" element={<DebugSection focusedMessageId={null} />} />
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