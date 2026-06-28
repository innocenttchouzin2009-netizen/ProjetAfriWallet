"use client";

import Link from 'next/link';
import { Suspense, useMemo } from 'react';
import { useSearchParams } from 'next/navigation';

function CheckoutCancelContent() {
  const params = useSearchParams();

  const provider = useMemo(() => {
    const value = params.get('provider');
    return value === 'stripe' || value === 'paypal' ? value : null;
  }, [params]);

  const orderId = useMemo(() => params.get('orderId'), [params]);
  const reason = useMemo(() => params.get('reason'), [params]);

  const providerLabel = provider === 'stripe' ? 'Stripe' : provider === 'paypal' ? 'PayPal' : 'paiement';

  const description =
    reason === 'stripe_redirect_failed'
      ? `La redirection ${providerLabel} a echoue. Aucun debit n'a ete confirme.`
      : `Le paiement ${providerLabel} a ete interrompu. Aucun debit n'a ete confirme.`;

  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-28 text-white md:px-16">
      <div className="mx-auto max-w-3xl rounded-[32px] border border-white/10 bg-white/5 p-10 text-center">
        <p className="text-sm uppercase tracking-[0.4em] text-[#C8A45C]">Paiement annule</p>
        <h1 className="mt-4 text-4xl font-black">La transaction a ete interrompue</h1>
        <p className="mt-6 text-white/70">{description}</p>
        {orderId ? <p className="mt-3 text-xs uppercase tracking-[0.2em] text-white/50">Commande: {orderId}</p> : null}
        <Link href="/checkout" className="mt-8 inline-flex rounded-full bg-[#C8A45C] px-6 py-4 font-bold text-black">
          Revenir au checkout
        </Link>
      </div>
    </main>
  );
}

export default function CheckoutCancelPage() {
  return (
    <Suspense
      fallback={
        <main className="min-h-screen bg-[#0D0D0D] px-6 py-28 text-white md:px-16">
          <div className="mx-auto max-w-3xl rounded-[32px] border border-white/10 bg-white/5 p-10 text-center">
            <p className="text-sm uppercase tracking-[0.4em] text-[#C8A45C]">Paiement annule</p>
            <h1 className="mt-4 text-4xl font-black">Chargement...</h1>
          </div>
        </main>
      }
    >
      <CheckoutCancelContent />
    </Suspense>
  );
}
