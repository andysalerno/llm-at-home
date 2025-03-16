import { DrawerProps, mergeClasses } from "@fluentui/react-components";
import * as React from "react";
import {
    AppItem,
    Hamburger,
    NavDrawer,
    NavDrawerBody,
    NavDrawerHeader,
    NavDrawerProps,
    NavItem,
} from "@fluentui/react-nav-preview";

import {
    Tooltip,
    makeStyles,
    tokens,
} from "@fluentui/react-components";
import {
    Board20Filled,
    Board20Regular,
    MegaphoneLoud20Filled,
    MegaphoneLoud20Regular,
    bundleIcon,
    PersonCircle32Regular,
} from "@fluentui/react-icons";
import { useRouter } from "next/navigation";

const useStyles = makeStyles({
    root: {
        overflow: "hidden",
        display: "flex",
        height: "600px",
    },
    content: {
        flex: "1",
        padding: "16px",
        display: "grid",
        justifyContent: "flex-start",
        alignItems: "flex-start",
    },
    field: {
        display: "flex",
        marginTop: "4px",
        marginLeft: "8px",
        flexDirection: "column",
        gridRowGap: tokens.spacingVerticalS,
    },
});

const Dashboard = bundleIcon(Board20Filled, Board20Regular);
const Announcements = bundleIcon(MegaphoneLoud20Filled, MegaphoneLoud20Regular);

type DrawerType = Required<DrawerProps>["type"];

export const Navigation = (props: Partial<NavDrawerProps>) => {
    const styles = useStyles();

    const [isOpen, setIsOpen] = React.useState(true);
    const [enabledLinks, setEnabledLinks] = React.useState(true);
    const [type, setType] = React.useState<DrawerType>("inline");
    const [isMultiple, setIsMultiple] = React.useState(true);

    // Map paths to their corresponding NavItem values
    const getSelectedValue = () => {
        const pathname = window.location.pathname;

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
        // <div className={styles.root}>
        <div className={mergeClasses('ui-component flex h-screen', isOpen ? 'w-64' : 'w-14')}>
            <NavDrawer
                open={true}
                type={type}
                multiple={isMultiple}
                selectedValue={selectedValue}
            >
                <NavDrawerHeader>{renderHamburgerWithToolTip()}</NavDrawerHeader>
                <NavDrawerBody>
                    <AppItem
                        as="a"
                        href={'/'}
                    >
                        {isOpen ? 'AgentFlow UI!!' : 'AF'}
                    </AppItem>
                    <NavItem href={'/chat'} icon={<Dashboard />} value="1">
                        {isOpen && 'Chat'}
                    </NavItem>
                    <NavItem href={'/completion'} icon={<Announcements />} value="2">
                        {isOpen && 'Completion'}
                    </NavItem>
                    <NavItem href={'/debug'} icon={<Announcements />} value="3">
                        {isOpen && 'Debug'}
                    </NavItem>
                    <NavItem href={'/config'} icon={<Announcements />} value="4">
                        {isOpen && 'Config'}
                    </NavItem>
                </NavDrawerBody>
            </NavDrawer>
            {/* <div className={styles.content}>
                {!isOpen && renderHamburgerWithToolTip()}
            </div> */}
        </div>
    );
};