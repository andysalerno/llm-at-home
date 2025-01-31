'use client'

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { Text, Button, makeStyles, tokens, mergeClasses } from '@fluentui/react-components';
import {
    Navigation24Regular,
    DismissRegular,
    Chat24Regular,
    TextExpand24Regular,
    Bug24Regular
} from '@fluentui/react-icons';

interface NavigationProps {
    isOpen: boolean;
    onOpenChange: (isOpen: boolean) => void;
}

const useStyles = makeStyles({
    navigationBackground: { backgroundColor: tokens.colorNeutralBackground2 },
});

const navigationItems = [
    { href: '/chat', icon: Chat24Regular, label: 'Chat' },
    { href: '/completion', icon: TextExpand24Regular, label: 'Text Completion' },
    { href: '/debug', icon: Bug24Regular, label: 'Debug' },
];

export function Navigation({ isOpen, onOpenChange }: NavigationProps) {
    const classes = useStyles();
    const pathname = usePathname();

    return (
        <nav
            className={mergeClasses(`fixed inset-y-0 left-0 shadow-lg transition-[width] z-30
                ${isOpen ? 'w-64' : 'w-12'}`, classes.navigationBackground)}
        >
            <div className="flex justify-between p-4">
                <div />
                <Button
                    icon={isOpen ? <DismissRegular /> : <Navigation24Regular />}
                    appearance="subtle"
                    onClick={() => onOpenChange(!isOpen)}
                    aria-label={isOpen ? 'Collapse navigation' : 'Expand navigation'}
                />
            </div>
            <div className="py-6">
                {navigationItems.map(({ href, icon: Icon, label }) => {
                    const isActive = pathname === href;
                    return (
                        <Link
                            key={href}
                            href={href}
                            className={`flex w-full px-4 py-2 mb-1 text-sm transition-colors`}
                        >
                            <Icon className="shrink-0" />
                            {isOpen && <span className="ml-3">{label}</span>}
                        </Link>
                    );
                })}
            </div>
        </nav>
    );
}