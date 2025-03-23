'use client'

import { Navigation } from './Navigation';
import { makeStyles, mergeClasses, tokens } from '@fluentui/react-components';

const useStyles = makeStyles({
    root: { backgroundColor: tokens.colorNeutralBackground2 },
});

interface AppShellProps {
    children: React.ReactNode;
}

export default function AppShell({ children }: AppShellProps) {
    const classes = useStyles();

    return (
        <div className={mergeClasses('ui-component', 'flex', 'h-screen', classes.root)}>
            <Navigation />
            <div
                className="ui-component flex flex-col flex-grow overflow-hidden"
            >
                <main className="ui-component flex-grow overflow-x-hidden overflow-y-auto h-full">
                    {children}
                </main>
            </div>
        </div>
    );
}