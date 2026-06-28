"use client";

import { useMemo, useState } from 'react';
import type { AdminOrder } from '../types/admin-order.types';

type Props = {
  orders: AdminOrder[];
  onShipped: () => Promise<void> | void;
};

type Carrier = 'DHL' | 'DPD' | 'UPS';

export default function AdminShipOrderPanel({ orders, onShipped }: Props) {
  const shipCandidates = useMemo(
    () => orders.filter((order) => order.status !== 'SHIPPED' && order.status !== 'DELIVERED'),
    [orders],
  );

  const [orderId, setOrderId] = useState('');
  const [carrier, setCarrier] = useState<Carrier>('DHL');
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const selectedOrder = shipCandidates.find((order) => order.id === orderId);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!orderId) return;

    setLoading(true);
    setError(null);
    setMessage(null);

    try {
      const response = await fetch(`/api/admin/orders/${orderId}/ship`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ carrier }),
      });

      const payload = (await response.json()) as {
        message?: string;
        shipment?: { trackingNumber?: string; carrier?: string };
      };

      if (!response.ok) {
        throw new Error(payload.message ?? 'Failed to create shipment');
      }

      setMessage(
        `Expedition creee via ${payload.shipment?.carrier ?? carrier} - suivi ${payload.shipment?.trackingNumber ?? 'N/A'}`,
      );
      await onShipped();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown shipping error');
    } finally {
      setLoading(false);
    }
  };

  return (
    <section className="rounded-[32px] border border-white/10 bg-white/5 p-6">
      <p className="text-sm uppercase tracking-[0.3em] text-[#C8A45C]">Expedition v1</p>
      <h2 className="mt-2 text-2xl font-black text-white">Generer une expedition</h2>

      <form onSubmit={submit} className="mt-5 grid gap-4 md:grid-cols-[1fr_180px_auto] md:items-end">
        <label className="flex flex-col gap-2 text-sm text-white/70">
          Commande
          <select
            value={orderId}
            onChange={(event) => setOrderId(event.target.value)}
            className="rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white"
            required
          >
            <option value="">Selectionner une commande</option>
            {shipCandidates.map((candidate) => (
              <option key={candidate.id} value={candidate.id}>
                {candidate.id} - {candidate.customer}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-2 text-sm text-white/70">
          Transporteur
          <select
            value={carrier}
            onChange={(event) => setCarrier(event.target.value as Carrier)}
            className="rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white"
          >
            <option value="DHL">DHL</option>
            <option value="DPD">DPD</option>
            <option value="UPS">UPS</option>
          </select>
        </label>

        <button
          type="submit"
          disabled={loading || !orderId}
          className="rounded-full bg-[#C8A45C] px-6 py-3 font-bold text-black disabled:opacity-50"
        >
          {loading ? 'Generation...' : 'Expedier'}
        </button>
      </form>

      {selectedOrder?.shipment ? (
        <p className="mt-4 text-xs text-white/60">
          Tracking actuel: {selectedOrder.shipment.carrier} - {selectedOrder.shipment.trackingNumber}
        </p>
      ) : null}

      {message ? <p className="mt-4 text-sm text-emerald-300">{message}</p> : null}
      {error ? <p className="mt-4 text-sm text-[#F0B86E]">{error}</p> : null}
    </section>
  );
}
