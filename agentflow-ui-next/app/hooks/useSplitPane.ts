import { useState, useCallback, useEffect } from 'react';

interface UseSplitPaneProps {
    defaultSize?: number;
    minSize?: number;
    maxSize?: number;
    onChange?: (size: number) => void;
}

export function useSplitPane({
    minSize = 20,
    maxSize = 80,
    defaultSize = 50,
    onChange
}: UseSplitPaneProps = {}) {
    const [size, setSize] = useState(defaultSize);
    const [isDragging, setIsDragging] = useState(false);

    const handleMouseDown = useCallback((e: React.MouseEvent) => {
        e.preventDefault();
        setIsDragging(true);
    }, []);

    const handleMouseUp = useCallback(() => {
        setIsDragging(false);
    }, []);

    const handleMouseMove = useCallback((e: MouseEvent) => {
        if (!isDragging) return;

        const container = document.getElementById('split-pane-container');
        if (!container) return;

        const containerRect = container.getBoundingClientRect();
        const newSize = ((e.clientX - containerRect.left) / containerRect.width) * 100;
        const clampedSize = Math.min(Math.max(newSize, minSize), maxSize);

        setSize(clampedSize);
        onChange?.(clampedSize);
    }, [isDragging, minSize, maxSize, onChange]);

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

    return {
        size,
        isDragging,
        handleMouseDown
    };
}