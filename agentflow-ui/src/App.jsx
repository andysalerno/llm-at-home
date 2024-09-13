import React, { useState } from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import Sidebar from './components/Sidebar';
import SplitView from './components/SplitView';
import TextEditor from './components/TextEditor';
import DebugSection from './components/DebugSection';
import ChatSection from './components/ChatSection';

const App = () => {
    const [sidebarOpen, setSidebarOpen] = useState(false);

    return (
        <Router>
            <div className="flex h-screen bg-gray-100">
                <Sidebar isOpen={sidebarOpen} setIsOpen={setSidebarOpen} />
                <div className="flex-1 flex flex-col overflow-hidden">
                    <header className="bg-white shadow-sm z-10">
                        <div className="max-w-7xl mx-auto py-4 px-4">
                            <h1 className="text-lg font-semibold text-gray-900">Agentflow</h1>
                        </div>
                    </header>
                    <main className="flex-1 overflow-x-hidden overflow-y-auto bg-gray-200">
                        <div className="container mx-auto h-full">
                            <Routes>
                                <Route path="/" element={<SplitView left={ChatSection} right={DebugSection} />} />
                                <Route path="/debug" element={<DebugSection />} />
                            </Routes>
                        </div>
                    </main>
                </div>
            </div>
        </Router>
    );
};

export default App;