import { makeStyles, mergeClasses, tokens } from "@fluentui/react-components";
import * as React from "react";
import {
    AppItem,
    Hamburger,
    NavDrawerBody,
    NavDrawerHeader,
    NavItem,
} from "@fluentui/react-nav-preview";
import { NavDrawer } from "./NavDrawerWrapper";
import { usePathname } from 'next/navigation'
import {
    Tooltip,
} from "@fluentui/react-components";
import {
    Board20Filled,
    Board20Regular,
    MegaphoneLoud20Filled,
    MegaphoneLoud20Regular,
    bundleIcon,
} from "@fluentui/react-icons";

const Dashboard = bundleIcon(Board20Filled, Board20Regular);
const Announcements = bundleIcon(MegaphoneLoud20Filled, MegaphoneLoud20Regular);

const useStyles = makeStyles({
    nav: { backgroundColor: tokens.colorNeutralBackground1 },
});

export const Navigation = () => {
    const [isOpen, setIsOpen] = React.useState(true);
    const pathname = usePathname();
    const styles = useStyles();

    // Map paths to their corresponding NavItem values
    const getSelectedValue = () => {
        const pathToValueMap: Record<string, string> = {
            '/chat': '1',
            '/completion': '2',
            '/debug': '3',
            '/config': '4',
            '/': '1'
        };

        return pathToValueMap[pathname] || 'home';
    };

    const selectedValue = getSelectedValue();

    const renderHamburgerWithToolTip = () => {
        return (
            <Tooltip content="Navigation" relationship="label">
                <Hamburger onClick={() => setIsOpen(!isOpen)} />
            </Tooltip>
        );
    };

    return (
        <div className={mergeClasses('ui-component flex h-screen', isOpen ? 'w-64' : 'w-14')}>
            <NavDrawer
                open={true}
                type={'inline'}
                multiple={true}
                selectedValue={selectedValue}
                className={styles.nav}
            >
                <NavDrawerHeader className={styles.nav}>{renderHamburgerWithToolTip()}</NavDrawerHeader>
                <NavDrawerBody className={styles.nav}>
                    <AppItem
                        as="a"
                        href={'/'}
                        className={styles.nav}
                    >
                        {isOpen ? 'AgentFlow UI!!' : 'AF'}
                    </AppItem>
                    <NavItem href={'/chat'} icon={<Dashboard />} value="1"
                        className={styles.nav}
                    >
                        {isOpen && 'Chat'}
                    </NavItem>
                    <NavItem href={'/completion'} icon={<Announcements />} value="2"
                        className={styles.nav}
                    >
                        {isOpen && 'Completion'}
                    </NavItem>
                    <NavItem href={'/debug'} icon={<Announcements />} value="3"
                        className={styles.nav}
                    >
                        {isOpen && 'Debug'}
                    </NavItem>
                    <NavItem href={'/config'} icon={<Announcements />} value="4"
                        className={styles.nav}
                    >
                        {isOpen && 'Config'}
                    </NavItem>
                </NavDrawerBody>
            </NavDrawer>
        </div>
    );
};