import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { ChatBubbleLeftRightIcon, BugAntIcon, Bars3Icon, Bars3BottomLeftIcon, XMarkIcon } from '@heroicons/react/24/outline';

interface NavItemProps {
    to: string;
    icon: React.ComponentType<React.SVGProps<SVGSVGElement>>;
    children: any;
}

const Sidebar = () => {
    const location = useLocation();

    const NavItem: React.FC<NavItemProps> = ({ to, icon: Icon, children }) => {
        const isActive = location.pathname === to;
        return (
            <Link
                to={to}
                className={`flex items-center px-6 py-2 mt-4 transition-colors`}
            >
                <Icon className="w-6 h-6" />
                <span className={`mx-3 hidden`}>{children}</span>
            </Link>
        );
    };

    return (
        <div className={`fixed inset-y-0 left-0 z-30 transition-all w-20`}>
            <div className="h-full overflow-y-auto">
                <nav className="mt-10">
                    <NavItem to="/" icon={ChatBubbleLeftRightIcon} children="Chat" />
                    <NavItem to="/text" icon={Bars3BottomLeftIcon} children="Text" />
                    <NavItem to="/debug" icon={BugAntIcon} children="Debug" />
                </nav>
            </div>
        </div>
    );
};

export default Sidebar;