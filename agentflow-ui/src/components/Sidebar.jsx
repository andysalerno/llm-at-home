import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { ChatBubbleLeftRightIcon, BugAntIcon, Bars3Icon, XMarkIcon } from '@heroicons/react/24/outline';

const Sidebar = ({ isOpen, setIsOpen }) => {
    const location = useLocation();

    const NavItem = ({ to, icon: Icon, children }) => {
        const isActive = location.pathname === to;
        return (
            <Link
                to={to}
                className={`flex items-center px-6 py-2 mt-4 transition-colors duration-300 ease-in-out ${isActive
                        ? 'text-white bg-blue-600'
                        : 'text-gray-100 hover:bg-gray-700 hover:bg-opacity-25 hover:text-gray-100'
                    }`}
            >
                <Icon className="w-6 h-6" />
                <span className={`mx-3 ${isOpen ? 'block' : 'hidden'}`}>{children}</span>
            </Link>
        );
    };

    return (
        <div className={`fixed inset-y-0 left-0 z-30 transition-all duration-300 ${isOpen ? 'w-64' : 'w-20'}`}>
            <div className="h-full bg-gray-900 overflow-y-auto">
                <div className={`flex items-center justify-between ${isOpen ? 'px-6' : 'px-4'} py-4`}>
                    <div className={`flex items-center ${isOpen ? 'justify-between w-full' : 'justify-center'}`}>
                        {isOpen && <span className="text-white text-2xl font-semibold">AI Chat App</span>}
                        <button
                            onClick={() => setIsOpen(!isOpen)}
                            className="text-white focus:outline-none"
                        >
                            {isOpen ? (
                                <XMarkIcon className="w-6 h-6" />
                            ) : (
                                <Bars3Icon className="w-6 h-6" />
                            )}
                        </button>
                    </div>
                </div>

                <nav className="mt-10">
                    <NavItem to="/" icon={ChatBubbleLeftRightIcon}>Chat</NavItem>
                    <NavItem to="/debug" icon={BugAntIcon}>Debug</NavItem>
                </nav>
            </div>
        </div>
    );
};

export default Sidebar;