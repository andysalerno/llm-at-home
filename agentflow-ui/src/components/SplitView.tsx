import React, { useState } from 'react';
import { ChevronLeftIcon, ChevronRightIcon } from '@heroicons/react/24/solid';

interface SplitViewProps {
    left: React.ReactElement;
    right: React.ReactElement;
    isSplitVisible: boolean | null;
    setIsSplitVisible: React.Dispatch<React.SetStateAction<boolean>> | null;
}

const SplitView: React.FC<SplitViewProps> = ({ left, right, isSplitVisible, setIsSplitVisible }) => {
    if (isSplitVisible === null) {
        [isSplitVisible, setIsSplitVisible] = useState(false);
    }

    if (!setIsSplitVisible) {
        throw new Error();
    }

    return (
        <div className="flex h-full">
            <div className={`flex-grow transition-all duration-0 ${isSplitVisible ? 'w-1/2' : 'w-full'}`}>
                {left}
            </div>
            <div
                className={`flex-grow transition-all duration-0 ${isSplitVisible ? 'w-1/2' : 'w-0'} overflow-hidden`}
            >
                {isSplitVisible && right}
            </div>
            <button
                onClick={() => setIsSplitVisible?.(!isSplitVisible)}
                className="absolute right-0 top-1/2 transform -translate-y-1/2 bg-blue-500 text-white p-2 rounded-l-md"
            >
                {isSplitVisible ? <ChevronRightIcon className="h-6 w-6" /> : <ChevronLeftIcon className="h-6 w-6" />}
            </button>
        </div>
    );
};

export default SplitView;