'use client'

import Link from 'next/link'
import { usePathname } from 'next/navigation'
import { makeStyles, shorthands, tokens } from '@fluentui/react-components'
import {
    Chat24Regular,
    TextExpand24Regular,
    BugRegular,
    Navigation24Regular,
    DismissRegular
} from '@fluentui/react-icons'

interface SidebarProps {
    isOpen: boolean;
    setIsOpen: (isOpen: boolean) => void;
}

const useStyles = makeStyles({
    sidebar: {
        position: 'fixed',
        top: 0,
        bottom: 0,
        left: 0,
        zIndex: 30,
        transition: 'width 0.3s',
        backgroundColor: tokens.colorNeutralBackground1,
        boxShadow: tokens.shadow4,
    },
    header: {
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        ...shorthands.padding('16px'),
        borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    },
    title: {
        fontSize: tokens.fontSizeBase600,
        fontWeight: tokens.fontWeightSemibold,
        color: tokens.colorNeutralForeground1,
    },
    nav: {
        ...shorthands.padding('16px', '8px'),
    },
    navLink: {
        display: 'flex',
        alignItems: 'center',
        ...shorthands.padding('8px', '16px'),
        ...shorthands.margin('4px', '0'),
        ...shorthands.borderRadius('4px'),
        color: tokens.colorNeutralForeground1,
        textDecoration: 'none',
        transition: 'all 0.2s',
        ':hover': {
            backgroundColor: tokens.colorNeutralBackground1Hover,
        },
    },
    navLinkActive: {
        backgroundColor: tokens.colorNeutralBackground1Selected,
        color: tokens.colorBrandForeground1,
        ':hover': {
            backgroundColor: tokens.colorNeutralBackground1Selected,
        },
    },
    navLinkText: {
        marginLeft: '12px',
    },
    toggleButton: {
        background: 'none',
        border: 'none',
        color: tokens.colorNeutralForeground1,
        cursor: 'pointer',
        ...shorthands.padding('4px'),
        ':hover': {
            color: tokens.colorNeutralForeground1Hover,
        },
    }
});

interface NavItemProps {
    href: string;
    icon: React.ReactNode;
    children: React.ReactNode;
    isActive: boolean;
    showText: boolean;
}

const Sidebar: React.FC<SidebarProps> = ({ isOpen, setIsOpen }) => {
    const styles = useStyles();
    const pathname = usePathname();

    const NavItem = ({ href, icon, children, isActive, showText }: NavItemProps) => {
        return (
            <Link
                href={href}
                className={`${styles.navLink} ${isActive ? styles.navLinkActive : ''}`}
            >
                {icon}
                {showText && <span className={styles.navLinkText}>{children}</span>}
            </Link>
        );
    };

    return (
        <div className={styles.sidebar} style={{ width: isOpen ? '256px' : '48px' }}>
            <div className={styles.header}>
                {isOpen && <span className={styles.title}>AI Assistant</span>}
                <button
                    className={styles.toggleButton}
                    onClick={() => setIsOpen(!isOpen)}
                >
                    {isOpen ? <DismissRegular /> : <Navigation24Regular />}
                </button>
            </div>

            <nav className={styles.nav}>
                <NavItem
                    href="/"
                    icon={<Chat24Regular />}
                    isActive={pathname === '/'}
                    showText={isOpen}
                >
                    Chat
                </NavItem>
                <NavItem
                    href="/completion"
                    icon={<TextExpand24Regular />}
                    isActive={pathname === '/completion'}
                    showText={isOpen}
                >
                    Text Completion
                </NavItem>
                <NavItem
                    href="/debug"
                    icon={<BugRegular />}
                    isActive={pathname === '/debug'}
                    showText={isOpen}
                >
                    Debug
                </NavItem>
            </nav>
        </div>
    );
}

export default Sidebar