import type { ReactNode } from 'react';

export const metadata = {
  title: 'Admin',
  description: 'Dope Cute Studio admin panel',
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
