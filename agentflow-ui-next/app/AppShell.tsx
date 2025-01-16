'use client'

import { useState } from 'react'
import { makeStyles, shorthands, tokens } from '@fluentui/react-components'
import Sidebar from './components/SideBar'

const useStyles = makeStyles({
    root: {
        display: 'flex',
        height: '100vh',
        backgroundColor: tokens.colorNeutralBackground2,
    },
    content: {
        display: 'flex',
        flexDirection: 'column',
        flexGrow: 1,
        overflow: 'hidden',
        transition: 'margin-left 0.3s',
    },
    header: {
        backgroundColor: tokens.colorNeutralBackground1,
        boxShadow: tokens.shadow4,
        zIndex: 10,
    },
    headerInner: {
        maxWidth: '1400px',
        marginLeft: 'auto',
        marginRight: 'auto',
        ...shorthands.padding('16px'),
    },
    headerTitle: {
        fontSize: tokens.fontSizeBase600,
        fontWeight: tokens.fontWeightSemibold,
        color: tokens.colorNeutralForeground1,
    },
    main: {
        flexGrow: 1,
        overflowX: 'hidden',
        overflowY: 'auto',
        backgroundColor: tokens.colorNeutralBackground3,
    },
    mainInner: {
        maxWidth: '1400px',
        marginLeft: 'auto',
        marginRight: 'auto',
        height: '100%',
        ...shorthands.padding('16px'),
    }
});

export default function AppShell({
    children,
}: {
    children: React.ReactNode
}) {
    const styles = useStyles();
    const [sidebarOpen, setSidebarOpen] = useState<boolean>(true);

    return (
        <div className={styles.root}>
            <Sidebar isOpen={sidebarOpen} setIsOpen={setSidebarOpen} />
            <div className={styles.content} style={{ marginLeft: sidebarOpen ? '256px' : '48px' }}>
                <header className={styles.header}>
                    <div className={styles.headerInner}>
                        <h1 className={styles.headerTitle}>AI Assistant</h1>
                    </div>
                </header>
                <main className={styles.main}>
                    <div className={styles.mainInner}>
                        {children}
                    </div>
                </main>
            </div>
        </div>
    )
}