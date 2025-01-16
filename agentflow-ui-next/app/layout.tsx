import type { Metadata } from 'next'
import { Inter } from 'next/font/google'
import './globals.css'
import FluentUIProvider from './components/FluentUIProvider'

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
        <FluentUIProvider>
          {children}
        </FluentUIProvider>
      </body>
    </html>
  )
}