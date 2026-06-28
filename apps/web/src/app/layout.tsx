import './globals.css';
import type { Metadata } from 'next';
import { type ReactNode } from 'react';
import Navbar from '@/components/Navbar';
import { CartProvider } from '@/providers/CartProvider';

export const metadata: Metadata = {
  title: 'Dope Cute Studio',
  description: 'Boutique de chapeaux avec style et qualité',
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="fr">
      <body>
        <CartProvider>
          <Navbar />
          {children}
        </CartProvider>
      </body>
    </html>
  );
}
