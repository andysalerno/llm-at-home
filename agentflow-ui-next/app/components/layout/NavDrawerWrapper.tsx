import * as React from "react";
import { NavDrawer as FluentNavDrawer } from "@fluentui/react-nav-preview";

// Define a simplified interface for NavDrawer
interface NavDrawerProps {
    children?: React.ReactNode;
    open?: boolean;
    type?: 'inline' | 'overlay';
    multiple?: boolean;
    selectedValue?: string;
}

// Create a wrapper component with basic typing.
// Without this wrapper, typescript complains about the NavDrawer component
// being too complex to fully evaluate.
export const NavDrawer: React.FC<NavDrawerProps> = (props) => {
    return <FluentNavDrawer {...props} />;
};
