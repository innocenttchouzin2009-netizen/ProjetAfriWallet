"use client";

import { useEffect, useState } from 'react';

type AdminOrderRow = {
  id: string;
  customer: string;
  channel: string;
  total: string;
  status: string;
  statusCode: string;
  createdAt: string;
};

export default function AdminRecentOrders() {
  const [orders, setOrders] = useState<AdminOrderRow[]>([]);
  const [status, setStatus] = useState('all');
  const [channel, setChannel] = useState('all');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let mounted = true;

    const loadOrders = async () => {
      setLoading(true);

      const search = new URLSearchParams();
      search.set('limit', '8');
      if (status !== 'all') search.set('status', status);
      if (channel !== 'all') search.set('channel', channel);

      const response = await fetch(`/api/admin/orders?${search.toString()}`);
      const data = response.ok ? await response.json() : [];

      if (mounted) {
        setOrders(Array.isArray(data) ? data : []);
        setLoading(false);
      }
    };

    loadOrders();

    return () => {
      mounted = false;
    };
  }, [status, channel]);

  return (
    <section className="rounded-[32px] border border-white/10 bg-white/5 p-6">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm uppercase tracking-[0.3em] text-[#C8A45C]">Commandes</p>
          <h2 className="mt-2 text-2xl font-black text-white">Récentes</h2>
        </div>
        <div className="flex items-center gap-2">
          <select
            value={channel}
            onChange={(event) => setChannel(event.target.value)}
            className="rounded-full border border-white/10 bg-black/30 px-3 py-2 text-xs text-white/80"
          >
            <option value="all">Canal: tous</option>
            <option value="online">Online</option>
            <option value="pos">Boutique</option>
          </select>
          <select
            value={status}
            onChange={(event) => setStatus(event.target.value)}
            className="rounded-full border border-white/10 bg-black/30 px-3 py-2 text-xs text-white/80"
          >
            <option value="all">Statut: tous</option>
            <option value="CONFIRMED">Confirmee</option>
            <option value="READY">Prete</option>
            <option value="SHIPPED">Expediee</option>
            <option value="DELIVERED">Livree</option>
            <option value="CANCELED">Annulee</option>
          </select>
        </div>
      </div>

      <div className="mt-6 space-y-3">
        {loading ? (
          <div className="rounded-[20px] border border-white/10 bg-black/20 px-4 py-6 text-center text-sm text-white/60">
            Chargement des commandes...
          </div>
        ) : orders.length === 0 ? (
          <div className="rounded-[20px] border border-white/10 bg-black/20 px-4 py-6 text-center text-sm text-white/60">
            Aucune commande pour ce filtre.
          </div>
        ) : (
          orders.map((order) => (
            <div key={order.id} className="flex items-center justify-between rounded-[20px] border border-white/10 bg-black/20 px-4 py-4">
              <div>
                <p className="font-semibold text-white">{order.id}</p>
                <p className="text-sm text-white/60">{order.customer} • {order.channel}</p>
              </div>
              <div className="text-right">
                <p className="font-semibold text-white">{order.total}</p>
                <p className="text-sm text-[#C8A45C]">{order.status}</p>
              </div>
            </div>
          ))
        )}
      </div>
    </section>
  );
}
