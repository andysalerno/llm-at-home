'use client'

import React, { useState, useCallback, useEffect } from 'react';
import { Button } from '@fluentui/react-components';
import {
    ChevronLeft24Regular,
    ChevronRight24Regular
} from '@fluentui/react-icons';

interface SplitViewProps {
    left: React.ReactNode;
    right: React.ReactNode;
}

const SplitView: React.FC<SplitViewProps> = ({ left, right }) => {
    const [isRightVisible, setIsRightVisible] = useState<boolean>(true);
    const [leftWidth, setLeftWidth] = useState<number>(50);
    const [isDragging, setIsDragging] = useState<boolean>(false);

    const handleMouseDown = useCallback((e: React.MouseEvent) => {
        e.preventDefault();
        setIsDragging(true);
    }, []);

    const handleMouseUp = useCallback(() => {
        setIsDragging(false);
    }, []);

    const handleMouseMove = useCallback((e: MouseEvent) => {
        if (isDragging) {
            const container = document.getElementById('split-view-container');
            if (container) {
                const containerRect = container.getBoundingClientRect();
                const newLeftWidth = ((e.clientX - containerRect.left) / containerRect.width) * 100;
                setLeftWidth(Math.min(Math.max(newLeftWidth, 20), 80));
            }
        }
    }, [isDragging]);

    useEffect(() => {
        if (isDragging) {
            document.addEventListener('mousemove', handleMouseMove);
            document.addEventListener('mouseup', handleMouseUp);

            return () => {
                document.removeEventListener('mousemove', handleMouseMove);
                document.removeEventListener('mouseup', handleMouseUp);
            };
        }
    }, [isDragging, handleMouseMove, handleMouseUp]);

    return (
        <div id="split-view-container" className="flex h-full relative">
            <div
                style={{ width: isRightVisible ? `${leftWidth}%` : '100%' }}
                className="transition-[width] duration-300 overflow-auto"
            >
                {left}
            </div>

            {isRightVisible && (
                <>
                    <div
                        className="w-1 bg-gray-300 cursor-col-resize hover:bg-gray-400 active:bg-gray-500 transition-colors"
                        onMouseDown={handleMouseDown}
                    />
                    <div
                        style={{ width: `${100 - leftWidth}%` }}
                        className="transition-[width] duration-300 overflow-auto"
                    >
                        {right}
                    </div>
                </>
            )}

            <Button
                icon={isRightVisible ? <ChevronRight24Regular /> : <ChevronLeft24Regular />}
                appearance="subtle"
                className="absolute right-0 top-1/2 -translate-y-1/2 bg-blue-600 text-white hover:bg-blue-700 rounded-l-md rounded-r-none"
                onClick={() => setIsRightVisible(!isRightVisible)}
                aria-label={isRightVisible ? "Hide right panel" : "Show right panel"}
            />
        </div>
    );
};

export default SplitView;