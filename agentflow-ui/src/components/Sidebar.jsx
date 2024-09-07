import React from 'react';
import { Link } from 'react-router-dom';
import { HomeIcon, CogIcon } from '@heroicons/react/24/outline';

const Sidebar = ({ isOpen, setIsOpen }) => {
    return (
        <>
            <div
                className={`fixed inset-0 z-20 transition-opacity bg-black opacity-50 lg:hidden ${isOpen ? 'block' : 'hidden'
                    }`}
                onClick={() => setIsOpen(false)}
            ></div>

            <div
                className={`fixed inset-y-0 left-0 z-30 w-64 overflow-y-auto transition duration-300 transform bg-gray-900 lg:translate-x-0 lg:static lg:inset-0 ${isOpen ? 'translate-x-0 ease-out' : '-translate-x-full ease-in'
                    }`}
            >
                <div className="flex items-center justify-center mt-8">
                    <div className="flex items-center">
                        <span className="text-white text-2xl mx-2 font-semibold">MyApp</span>
                    </div>
                </div>

                <nav className="mt-10">
                    <Link
                        className="flex items-center px-6 py-2 mt-4 text-gray-100 hover:bg-gray-700 hover:bg-opacity-25 hover:text-gray-100"
                        to="/"
                    >
                        <HomeIcon className="w-6 h-6" />
                        <span className="mx-3">Home</span>
                    </Link>
                    <Link
                        className="flex items-center px-6 py-2 mt-4 text-gray-100 hover:bg-gray-700 hover:bg-opacity-25 hover:text-gray-100"
                        to="/other"
                    >
                        <CogIcon className="w-6 h-6" />
                        <span className="mx-3">Other Section</span>
                    </Link>
                    <Link
                        className="flex items-center px-6 py-2 mt-4 text-gray-100 hover:bg-gray-700 hover:bg-opacity-25 hover:text-gray-100"
                        to="/debug"
                    >
                        <CogIcon className="w-6 h-6" />
                        <span className="mx-3">Debug Section</span>
                    </Link>
                </nav>
            </div>
        </>
    );
};

export default Sidebar;