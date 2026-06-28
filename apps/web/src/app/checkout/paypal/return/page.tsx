"use client";

import { Suspense, useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';

type CaptureState = 'loading' | 'success' | 'error';

function PaypalReturnContent() {
  const params = useSearchParams();
  const [state, setState] = useState<CaptureState>('loading');
  const [message, setMessage] = useState('Confirmation du paiement en cours...');

  const orderId = useMemo(() => params.get('orderId') ?? '', [params]);
  const paypalOrderId = useMemo(() => params.get('token') ?? '', [params]);

  useEffect(() => {
    let isMounted = true;

    async function capture() {
      if (!orderId || !paypalOrderId) {
        if (isMounted) {
          setState('error');
          setMessage('Informations de paiement manquantes.');
        }
        return;
      }

      try {
        const response = await fetch('/api/payments/capture/paypal', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ orderId, paypalOrderId }),
        });

        if (!response.ok) {
          const payload = (await response.json()) as { message?: string };
          throw new Error(payload.message ?? 'Capture PayPal impossible.');
        }

        if (!isMounted) return;
        setState('success');
        setMessage('Paiement confirme. Redirection...');
        window.setTimeout(() => {
          window.location.href = '/checkout/success';
        }, 800);
      } catch (error) {
        if (!isMounted) return;
        setState('error');
        setMessage(error instanceof Error ? error.message : 'Capture PayPal impossible.');
      }
    }

    capture();

    return () => {
      isMounted = false;
    };
  }, [orderId, paypalOrderId]);

  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-28 text-white md:px-16">
      <div className="mx-auto max-w-3xl rounded-[32px] border border-white/10 bg-white/5 p-10 text-center">
        <p className="text-sm uppercase tracking-[0.4em] text-[#C8A45C]">Retour PayPal</p>
        <h1 className="mt-4 text-4xl font-black">
          {state === 'loading' ? 'Validation en cours' : state === 'success' ? 'Paiement valide' : 'Validation echouee'}
        </h1>
        <p className="mt-6 text-white/70">{message}</p>
        {state === 'error' ? (
          <Link href="/checkout" className="mt-8 inline-flex rounded-full bg-[#C8A45C] px-6 py-4 font-bold text-black">
            Revenir au checkout
          </Link>
        ) : null}
      </div>
    </main>
  );
}

export default function PaypalReturnPage() {
  return (
    <Suspense
      fallback={
        <main className="min-h-screen bg-[#0D0D0D] px-6 py-28 text-white md:px-16">
          <div className="mx-auto max-w-3xl rounded-[32px] border border-white/10 bg-white/5 p-10 text-center">
            <p className="text-sm uppercase tracking-[0.4em] text-[#C8A45C]">Retour PayPal</p>
            <h1 className="mt-4 text-4xl font-black">Chargement...</h1>
          </div>
        </main>
      }
    >
      <PaypalReturnContent />
    </Suspense>
  );
}
