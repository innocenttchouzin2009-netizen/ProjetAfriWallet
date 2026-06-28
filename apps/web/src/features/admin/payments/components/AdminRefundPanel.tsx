"use client";

import { useMemo, useState } from 'react';
import { useAdminRefund } from '../hooks/useAdminRefund';

export default function AdminRefundPanel() {
  const { loading, error, result, submitRefund } = useAdminRefund();
  const [orderId, setOrderId] = useState('');
  const [amountEuros, setAmountEuros] = useState('');
  const [reason, setReason] = useState('');

  const amountCents = useMemo(() => {
    if (!amountEuros.trim()) return undefined;
    const value = Number(amountEuros);
    if (!Number.isFinite(value)) return NaN;
    return Math.round(value * 100);
  }, [amountEuros]);

  const onSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!orderId.trim()) {
      return;
    }

    await submitRefund({
      orderId: orderId.trim(),
      amountCents,
      reason: reason.trim() || undefined,
    });
  };

  return (
    <section className="rounded-[32px] border border-white/10 bg-black/40 p-8">
      <h2 className="text-2xl font-black text-white">Remboursements unifies</h2>
      <p className="mt-2 max-w-3xl text-sm text-white/70">
        Saisis un identifiant de commande, un montant optionnel (laisse vide pour remboursement total) et une raison.
        Le provider est detecte automatiquement depuis la commande.
      </p>

      <form onSubmit={onSubmit} className="mt-6 space-y-4">
        <div className="grid gap-4 md:grid-cols-2">
          <input
            value={orderId}
            onChange={(event) => setOrderId(event.target.value)}
            placeholder="Order ID"
            className="rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white"
            required
          />
          <input
            value={amountEuros}
            onChange={(event) => setAmountEuros(event.target.value)}
            placeholder="Montant en EUR (vide = total)"
            className="rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white"
            inputMode="decimal"
          />
        </div>

        <textarea
          value={reason}
          onChange={(event) => setReason(event.target.value)}
          placeholder="Raison du remboursement"
          className="min-h-[110px] w-full rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-white"
        />

        <button
          type="submit"
          disabled={loading || Number.isNaN(amountCents)}
          className="rounded-full bg-[#C8A45C] px-6 py-3 font-bold text-black disabled:opacity-50"
        >
          {loading ? 'Traitement...' : 'Lancer le remboursement'}
        </button>
      </form>

      {Number.isNaN(amountCents) ? (
        <p className="mt-4 rounded-2xl border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-200">
          Le montant doit etre un nombre valide.
        </p>
      ) : null}

      {error ? (
        <p className="mt-4 rounded-2xl border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-200">{error}</p>
      ) : null}

      {result ? (
        <div className="mt-4 rounded-2xl border border-emerald-500/30 bg-emerald-500/10 px-4 py-3 text-sm text-emerald-200">
          <p>Remboursement enregistre.</p>
          <p className="mt-1">Provider: {result.provider}</p>
          <p>Reference: {result.reference}</p>
          <p>Statut: {result.status}</p>
        </div>
      ) : null}
    </section>
  );
}
