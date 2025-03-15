import type { Metadata } from 'next'
import { Inter } from 'next/font/google'
import './globals.css'
import FluentUIProvider from './components/providers/FluentUIProvider'
import { ReactNode } from 'react';
import { ConfigProvider } from './hooks/useConfig';

const inter = Inter({ subsets: ['latin'] })

export const metadata: Metadata = {
  title: 'AI Assistant',
  description: 'AI Chat and Debug Interface',
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en">
      <body className={inter.className}>
        <ConfigProvider>
          <FluentUIProvider>
            {children}
          </FluentUIProvider>
        </ConfigProvider>
      </body>
    </html>
  )
}