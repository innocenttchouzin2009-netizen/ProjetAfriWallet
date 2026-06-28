"use client";

import Link from "next/link";
import { useState } from 'react';
import CartDrawer from '@/components/CartDrawer';
import { useCart } from '@/hooks/useCart';

export default function Navbar() {
  const { itemCount } = useCart();
  const [drawerOpen, setDrawerOpen] = useState(false);

  return (
    <header className="fixed top-0 z-50 flex w-full items-center justify-between border-b border-white/10 bg-black/70 px-6 py-4 text-white backdrop-blur-md">
      <Link href="/" className="leading-none">
        <div className="text-xl font-black tracking-[0.25em]">DOPE&CUTE</div>
        <div className="text-right text-[10px] uppercase tracking-[0.5em] text-[#C8A45C]">
          studio
        </div>
      </Link>
      <nav className="hidden items-center gap-8 text-sm text-white/70 md:flex">
        <Link href="/shop" className="hover:text-white">
          Boutique
        </Link>
        <Link href="/studio" className="hover:text-white">
          Studio
        </Link>
        <Link href="/pro" className="hover:text-white">
          Pro
        </Link>
        <Link href="/contact" className="hover:text-white">
          Contact
        </Link>
      </nav>
      <button
        type="button"
        onClick={() => setDrawerOpen(true)}
        className="flex items-center gap-2 rounded-full bg-[#C8A45C] px-5 py-2 text-sm font-bold text-black"
      >
        <span>Panier</span>
        <span className="rounded-full bg-black px-2 py-0.5 text-[11px] text-[#C8A45C]">
          {itemCount}
        </span>
      </button>
      <CartDrawer open={drawerOpen} onClose={() => setDrawerOpen(false)} />
    </header>
  );
}
