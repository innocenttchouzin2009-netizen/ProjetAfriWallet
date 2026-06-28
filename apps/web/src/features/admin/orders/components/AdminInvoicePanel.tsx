"use client";

import { useMemo, useState } from 'react';
import type { AdminOrder } from '../types/admin-order.types';

type Props = {
  orders: AdminOrder[];
};

type DocumentType = 'INVOICE' | 'DELIVERY_NOTE';

export default function AdminInvoicePanel({ orders }: Props) {
  const [orderId, setOrderId] = useState('');
  const [documentType, setDocumentType] = useState<DocumentType>('INVOICE');
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const eligibleOrders = useMemo(() => orders, [orders]);

  const onDownload = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!orderId) return;

    setLoading(true);
    setMessage(null);
    setError(null);

    try {
      const response = await fetch(`/api/admin/orders/${orderId}/invoice?document=${documentType}`);
      if (!response.ok) {
        const body = await response.json().catch(() => ({ message: 'Failed to generate document' }));
        throw new Error(body.message ?? 'Failed to generate document');
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download =
        documentType === 'INVOICE'
          ? `facture-${orderId}.pdf`
          : `bon-livraison-${orderId}.pdf`;
      a.click();
      window.URL.revokeObjectURL(url);

      setMessage(documentType === 'INVOICE' ? 'Facture telechargee.' : 'Bon de livraison telecharge.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown invoice error');
    } finally {
      setLoading(false);
    }
  };

  return (
    <section className="rounded-[32px] border border-white/10 bg-white/5 p-6">
      <p className="text-sm uppercase tracking-[0.3em] text-[#C8A45C]">Facturation PDF v1</p>
      <h2 className="mt-2 text-2xl font-black text-white">Facture & bon de livraison</h2>

      <form onSubmit={onDownload} className="mt-5 grid gap-4 md:grid-cols-[1fr_220px_auto] md:items-end">
        <label className="flex flex-col gap-2 text-sm text-white/70">
          Commande
          <select
            value={orderId}
            onChange={(event) => setOrderId(event.target.value)}
            className="rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white"
            required
          >
            <option value="">Selectionner une commande</option>
            {eligibleOrders.map((order) => (
              <option key={order.id} value={order.id}>
                {order.id} - {order.customer}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-2 text-sm text-white/70">
          Document
          <select
            value={documentType}
            onChange={(event) => setDocumentType(event.target.value as DocumentType)}
            className="rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white"
          >
            <option value="INVOICE">Facture PDF</option>
            <option value="DELIVERY_NOTE">Bon de livraison PDF</option>
          </select>
        </label>

        <button
          type="submit"
          disabled={loading || !orderId}
          className="rounded-full bg-[#C8A45C] px-6 py-3 font-bold text-black disabled:opacity-50"
        >
          {loading ? 'Generation...' : 'Telecharger'}
        </button>
      </form>

      {message ? <p className="mt-4 text-sm text-emerald-300">{message}</p> : null}
      {error ? <p className="mt-4 text-sm text-[#F0B86E]">{error}</p> : null}
    </section>
  );
}
