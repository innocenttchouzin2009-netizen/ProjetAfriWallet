"use client";

import Link from 'next/link';
import { useEffect } from 'react';
import { useCart } from '@/hooks/useCart';

export default function CheckoutSuccessPage() {
  const { clearCart } = useCart();

  useEffect(() => {
    clearCart();
  }, [clearCart]);

  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-28 text-white md:px-16">
      <div className="mx-auto max-w-3xl rounded-[32px] border border-white/10 bg-white/5 p-10 text-center">
        <p className="text-sm uppercase tracking-[0.4em] text-[#C8A45C]">Commande confirmée</p>
        <h1 className="mt-4 text-4xl font-black">Merci pour votre commande</h1>
        <p className="mt-6 text-white/70">
          Votre paiement a ete confirme. Un email de confirmation vous sera envoye tres vite.
        </p>
        <Link
          href="/"
          className="mt-8 inline-flex rounded-full bg-[#C8A45C] px-6 py-4 font-bold text-black"
        >
          Retour à l’accueil
        </Link>
      </div>
    </main>
  );
}
