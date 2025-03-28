'use client'

import { useState } from 'react';
import { Button } from '@fluentui/react-components';
import {
    ChevronLeft24Regular,
    ChevronRight24Regular
} from '@fluentui/react-icons';
import { useSplitPane } from '../../hooks/useSplitPane';
import { mergeClasses } from '@fluentui/react-components';

interface SplitPaneProps {
    left: React.ReactNode;
    right: React.ReactNode;
    defaultLeftSize?: number;
    minLeftSize?: number;
    maxLeftSize?: number;
    onSizeChange?: (size: number) => void;
    className?: string;
}

export default function SplitPane({
    left,
    right,
    defaultLeftSize = 50,
    minLeftSize = 20,
    maxLeftSize = 80,
    onSizeChange,
    className
}: SplitPaneProps) {
    const [showRight, setShowRight] = useState(true);
    const { size, isDragging, handleMouseDown } = useSplitPane({
        defaultSize: defaultLeftSize,
        minSize: minLeftSize,
        maxSize: maxLeftSize,
        onChange: onSizeChange
    });

    return (
        <div
            id="split-pane-container"
            className={mergeClasses(
                'flex h-full relative',
                isDragging && 'select-none cursor-col-resize',
                className
            )}
        >
            <div
                className={mergeClasses(
                    'overflow-auto',
                    isDragging && 'pointer-events-none'
                )}
                style={{ width: showRight ? `${size}%` : '100%' }}
            >
                {left}
            </div>

            {showRight && (
                <>
                    <div
                        className={mergeClasses(
                            'w-1 cursor-col-resize transition-colors'
                        )}
                        onMouseDown={handleMouseDown}
                        role="separator"
                        aria-orientation="vertical"
                        aria-valuenow={size}
                        aria-valuemin={minLeftSize}
                        aria-valuemax={maxLeftSize}
                    />
                    <div
                        className={mergeClasses(
                            'transitionoverflow-auto',
                            isDragging && 'pointer-events-none'
                        )}
                        style={{ width: `${100 - size}%` }}
                    >
                        {right}
                    </div>
                </>
            )}

            <Button
                icon={showRight ? <ChevronRight24Regular /> : <ChevronLeft24Regular />}
                appearance="subtle"
                className="absolute right-0 top-1/2 -translate-y-1/2 hover:bg-brand-hover rounded-l-md rounded-r-none"
                onClick={() => setShowRight(!showRight)}
                aria-label={showRight ? "Hide right panel" : "Show right panel"}
            />
        </div>
    );
}