import React, { useState } from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import Sidebar from './components/Sidebar';
import ChatSection from './components/ChatSection';
import DebugSection from './components/DebugSection';
import SplitView from './components/SplitView';

const App = () => {
    const [sidebarOpen, setSidebarOpen] = useState(true);

    return (
        <Router>
            <div className="flex h-screen bg-gray-100">
                <Sidebar isOpen={sidebarOpen} setIsOpen={setSidebarOpen} />
                <div className={`flex-1 flex flex-col overflow-hidden transition-all duration-300 ${sidebarOpen ? 'ml-64' : 'ml-20'}`}>
                    <header className="bg-white shadow-sm z-10">
                        <div className="max-w-7xl py-4 px-4 sm:px-6 lg:px-8">
                            <h1 className="text-lg font-semibold text-gray-900">AI Chat App</h1>
                        </div>
                    </header>
                    <main className="flex-1 overflow-x-hidden overflow-y-auto bg-gray-200">
                        <div className="mx-auto h-full">
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