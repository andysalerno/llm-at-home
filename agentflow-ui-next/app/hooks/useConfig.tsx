'use client';

import { useState, useEffect, createContext, useContext, ReactNode } from 'react';

interface ChatConfig {
  apiEndpoint: string;
  // Add other configuration options here as needed
}

const DEFAULT_CONFIG: ChatConfig = {
  apiEndpoint: 'http://nzxt.local:8003/v1/chat/completions',
  // Set defaults for other options
};

const CONFIG_STORAGE_KEY = 'chatConfig';

// Create a context for the configuration
type ConfigContextType = {
  config: ChatConfig;
  updateConfig: (newConfig: Partial<ChatConfig>) => void;
};

// Create the context with undefined as default value
const ConfigContext = createContext<ConfigContextType | undefined>(undefined);

// Provider component for the configuration context
// export function ConfigProvider({ children }: { children: ReactNode }) {
export function ConfigProvider({ children }: { children: ReactNode }): JSX.Element {
  const [config, setConfig] = useState<ChatConfig>(DEFAULT_CONFIG);

  // Load configuration from localStorage on mount
  useEffect(() => {
    const storedConfig = localStorage.getItem(CONFIG_STORAGE_KEY);
    if (storedConfig) {
      try {
        setConfig(prev => ({ ...prev, ...JSON.parse(storedConfig) }));
      } catch (error) {
        console.error('Error parsing stored config:', error);
      }
    }
  }, []);

  // Save configuration to localStorage when it changes
  useEffect(() => {
    localStorage.setItem(CONFIG_STORAGE_KEY, JSON.stringify(config));
  }, [config]);

  const updateConfig = (newConfig: Partial<ChatConfig>) => {
    console.log('Updating config:', newConfig);
    setConfig(prev => ({ ...prev, ...newConfig }));
  };

  return (
    <ConfigContext.Provider value={{ config, updateConfig }}>
      {children}
    </ConfigContext.Provider>
  );
}

// Hook for components to use the configuration
export function useConfig() {
  const context = useContext(ConfigContext);
  if (context === undefined) {
    throw new Error('useConfig must be used within a ConfigProvider');
  }
  return context;
}