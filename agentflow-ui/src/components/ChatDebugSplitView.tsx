import React, { useState } from 'react';
import ChatSection from './ChatSection';
import DebugSection from './DebugSection';
import SplitView from './SplitView';

const ChatDebugSplitView = () => {
    const [focusedMessageId, setFocusedMessageId] = useState(null);

    return (
        <SplitView left={ChatSection} right={DebugSection} />
    );
};

export default ChatDebugSplitView;