'use client'

import { useState } from 'react';
import { Navigation } from './Navigation';
import { makeStyles, mergeClasses, tokens } from '@fluentui/react-components';

const useStyles = makeStyles({
    root: { backgroundColor: tokens.colorNeutralBackground2 },
});

interface AppShellProps {
    children: React.ReactNode;
}

export default function AppShell({ children }: AppShellProps) {
    const [isNavigationOpen, setIsNavigationOpen] = useState(true);

    const classes = useStyles();

    return (
        <div className={mergeClasses('ui-component', 'flex', 'h-screen', classes.root)}>
            <Navigation
                isOpen={isNavigationOpen}
                onOpenChange={setIsNavigationOpen}
            />
            <div
                className="ui-component flex flex-col flex-grow overflow-hidden transition-[margin-left]"
                style={{ marginLeft: isNavigationOpen ? '256px' : '48px' }}
            >
                <main className="ui-component flex-grow overflow-x-hidden overflow-y-auto h-full">
                    {children}
                </main>
            </div>
        </div>
    );
}