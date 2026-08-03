"use client";

import { useEffect, useState } from 'react';

type AdminOrder = {
  id: string;
  customer: string;
  status: 'CONFIRMED' | 'IN_PRODUCTION' | 'READY' | 'SHIPPED' | 'DELIVERED';
  total: string;
  channel: 'ONLINE' | 'POS';
  createdAt: string;
};

const STATUS_OPTIONS: Array<AdminOrder['status']> = ['CONFIRMED', 'IN_PRODUCTION', 'READY', 'SHIPPED', 'DELIVERED'];

interface MobileOrderListProps {
  onBack: () => void;
}

export default function MobileOrderList({ onBack }: MobileOrderListProps) {
  const [orders, setOrders] = useState<AdminOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState<string | null>(null);
  const [updatingId, setUpdatingId] = useState<string | null>(null);

  const loadOrders = async () => {
    setLoading(true);
    setMessage(null);
    try {
      const response = await fetch('/api/admin/orders?limit=30');
      if (!response.ok) throw new Error('Chargement commandes impossible');
      const data = (await response.json()) as AdminOrder[];
      setOrders(data);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erreur commandes');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadOrders();
  }, []);

  const updateStatus = async (id: string, status: AdminOrder['status']) => {
    setUpdatingId(id);
    setMessage(null);
    try {
      const response = await fetch(`/api/admin/orders/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ status }),
      });
      if (!response.ok) {
        const body = await response.json().catch(() => ({ message: 'Mise à jour statut échouée' }));
        throw new Error(body.message ?? 'Mise à jour statut échouée');
      }
      await loadOrders();
      setMessage(`Statut mis à jour: ${status}`);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erreur statut');
    } finally {
      setUpdatingId(null);
    }
  };

  return (
    <section className="space-y-3">
      <button onClick={onBack} className="rounded-full border border-black/10 bg-white px-4 py-2 text-sm font-semibold text-[#4a5d78]">Retour</button>

      <div className="rounded-[22px] border border-black/10 bg-white p-4 shadow-[0_10px_20px_rgba(0,0,0,0.06)]">
        <div className="flex items-center justify-between">
          <p className="text-sm font-semibold text-[#5d6f87]">Commandes récentes</p>
          <button onClick={() => void loadOrders()} className="rounded-full border border-black/10 px-3 py-1 text-xs font-semibold text-[#4f6078]">Rafraîchir</button>
        </div>

        {loading ? <p className="mt-3 text-sm text-[#647892]">Chargement...</p> : null}
        {message ? <p className="mt-3 text-sm text-[#44607f]">{message}</p> : null}

        <div className="mt-3 space-y-2">
          {orders.map((order) => (
            <article key={order.id} className="rounded-2xl border border-black/10 bg-[#f8fbff] p-3">
              <div className="flex items-center justify-between gap-2">
                <div>
                  <p className="text-sm font-semibold text-[#22324b]">{order.id.slice(0, 12)}</p>
                  <p className="text-xs text-[#60728a]">{order.customer} • {order.channel}</p>
                </div>
                <p className="text-sm font-bold text-[#1f2f47]">{order.total}</p>
              </div>

              <div className="mt-2 flex items-center gap-2">
                <select
                  value={order.status}
                  onChange={(event) => void updateStatus(order.id, event.target.value as AdminOrder['status'])}
                  disabled={updatingId === order.id}
                  className="w-full rounded-xl border border-black/10 bg-white px-3 py-2 text-xs font-semibold text-[#324763]"
                >
                  {STATUS_OPTIONS.map((status) => (
                    <option key={status} value={status}>{status}</option>
                  ))}
                </select>
              </div>
            </article>
          ))}

          {!loading && !orders.length ? <p className="text-sm text-[#647892]">Aucune commande.</p> : null}
        </div>
      </div>
    </section>
  );
}
