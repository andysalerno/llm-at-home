import React, { useState } from 'react';
import { ChevronLeftIcon, ChevronRightIcon } from '@heroicons/react/24/solid';

interface SplitViewProps {
    left: React.ReactElement;
    right: React.ReactElement;
}

const SplitView: React.FC<SplitViewProps> = ({ left, right }) => {
    const [isRightVisible, setIsRightVisible] = useState(false);

    return (
        <div className="flex h-full">
            <div className={`flex-grow transition-all duration-0 ${isRightVisible ? 'w-1/2' : 'w-full'}`}>
                {left}
            </div>
            <div
                className={`flex-grow transition-all duration-0 ${isRightVisible ? 'w-1/2' : 'w-0'} overflow-hidden`}
            >
                {isRightVisible && right}
            </div>
            <button
                onClick={() => setIsRightVisible(!isRightVisible)}
                className="absolute right-0 top-1/2 transform -translate-y-1/2 bg-blue-500 text-white p-2 rounded-l-md"
            >
                {isRightVisible ? <ChevronRightIcon className="h-6 w-6" /> : <ChevronLeftIcon className="h-6 w-6" />}
            </button>
        </div>
    );
};

export default SplitView;