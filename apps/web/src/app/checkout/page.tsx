"use client";

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useCart } from '@/hooks/useCart';
import CheckoutForm from '@/features/checkout/components/CheckoutForm';
import { useCheckout } from '@/features/checkout/hooks/useCheckout';

export default function CheckoutPage() {
  const router = useRouter();
  const { itemCount } = useCart();
  const { state } = useCheckout();

  useEffect(() => {
    if (state.isComplete) {
      router.replace('/checkout/success');
    }
  }, [state.isComplete, router]);

  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-28 text-white md:px-16">
      <p className="text-sm uppercase tracking-[0.4em] text-[#C8A45C]">Checkout</p>
      <h1 className="mt-4 text-5xl font-black">Finalise ta commande</h1>
      <p className="mt-6 max-w-2xl text-white/70">
        Remplis les informations ci-dessous pour valider ta commande et payer via Stripe ou PayPal.
      </p>

      {itemCount === 0 ? (
        <div className="mt-10 rounded-[32px] border border-white/10 bg-white/5 p-8 text-white/70">
          Ton panier est vide. Ajoute d’abord des articles pour accéder au checkout.
        </div>
      ) : (
        <div className="mt-10">
          <CheckoutForm />
        </div>
      )}
    </main>
  );
}
