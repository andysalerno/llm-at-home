// // src/stores/chatStore.ts
// import { create } from 'zustand';
// import { persist } from 'zustand/middleware';
// import { Message, ChatState } from '@/types';

// interface ChatStore extends ChatState {
//     addMessage: (message: Message) => void;
//     deleteMessage: (id: string) => void;
//     clearMessages: () => void;
//     setLoading: (isLoading: boolean) => void;
//     setError: (error: string | null) => void;
// }

// export const useChatStore = create<ChatStore>()(
//     persist(
//         (set) => ({
//             messages: [],
//             isLoading: false,
//             error: null,
//             addMessage: (message) =>
//                 set((state) => ({
//                     messages: [...state.messages, message],
//                     error: null
//                 })),
//             deleteMessage: (id) =>
//                 set((state) => ({
//                     messages: state.messages.filter((msg) => msg.id !== id)
//                 })),
//             clearMessages: () =>
//                 set({ messages: [], error: null }),
//             setLoading: (isLoading) =>
//                 set({ isLoading }),
//             setError: (error) =>
//                 set({ error })
//         }),
//         {
//             name: 'chat-storage',
//             skipHydration: true
//         }
//     )
// );

// // src/stores/configStore.ts
// import { create } from 'zustand';
// import { persist } from 'zustand/middleware';
// import { AppConfig, ThemeConfig } from '@/types';

// interface ConfigStore {
//     config: AppConfig;
//     theme: ThemeConfig;
//     updateConfig: (config: Partial<AppConfig>) => void;
//     updateTheme: (theme: Partial<ThemeConfig>) => void;
// }

// const DEFAULT_CONFIG: AppConfig = {
//     temperature: 0.7,
//     maxTokens: 2000,
//     stopSequences: [],
//     apiEndpoint: process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:8003'
// };

// const DEFAULT_THEME: ThemeConfig = {
//     colorMode: 'light',
//     density: 'comfortable'
// };

// export const useConfigStore = create<ConfigStore>()(
//     persist(
//         (set) => ({
//             config: DEFAULT_CONFIG,
//             theme: DEFAULT_THEME,
//             updateConfig: (newConfig) =>
//                 set((state) => ({
//                     config: { ...state.config, ...newConfig }
//                 })),
//             updateTheme: (newTheme) =>
//                 set((state) => ({
//                     theme: { ...state.theme, ...newTheme }
//                 }))
//         }),
//         {
//             name: 'app-config',
//             skipHydration: true
//         }
//     )
// );