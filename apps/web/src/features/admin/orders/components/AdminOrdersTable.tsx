"use client";

import type { AdminOrder, AdminOrdersFilters, AdminOrderStatus } from '../types/admin-order.types';

type Props = {
  orders: AdminOrder[];
  filters: AdminOrdersFilters;
  loading: boolean;
  error: string | null;
  updatingId: string | null;
  onFilterChange: (next: Partial<AdminOrdersFilters>) => void;
  onStatusChange: (orderId: string, status: AdminOrderStatus) => void;
};

const statusOptions: AdminOrderStatus[] = [
  'CONFIRMED',
  'IN_PRODUCTION',
  'READY',
  'SHIPPED',
  'DELIVERED',
];

export default function AdminOrdersTable({
  orders,
  filters,
  loading,
  error,
  updatingId,
  onFilterChange,
  onStatusChange,
}: Props) {
  return (
    <section className="rounded-[32px] border border-white/10 bg-white/5 p-6">
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <p className="text-sm uppercase tracking-[0.3em] text-[#C8A45C]">Commandes</p>
          <h2 className="mt-2 text-2xl font-black text-white">Online + POS</h2>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <select
            value={filters.channel}
            onChange={(event) => onFilterChange({ channel: event.target.value as AdminOrdersFilters['channel'] })}
            className="rounded-full border border-white/10 bg-black/30 px-4 py-2 text-sm text-white"
          >
            <option value="ALL">Canal: Tous</option>
            <option value="ONLINE">ONLINE</option>
            <option value="POS">POS</option>
          </select>

          <select
            value={filters.status}
            onChange={(event) => onFilterChange({ status: event.target.value as AdminOrdersFilters['status'] })}
            className="rounded-full border border-white/10 bg-black/30 px-4 py-2 text-sm text-white"
          >
            <option value="ALL">Statut: Tous</option>
            {statusOptions.map((status) => (
              <option key={status} value={status}>{status}</option>
            ))}
          </select>
        </div>
      </div>

      {loading ? <p className="mt-6 text-sm text-white/70">Chargement des commandes...</p> : null}
      {error ? <p className="mt-6 text-sm text-[#F0B86E]">Erreur: {error}</p> : null}

      <div className="mt-6 space-y-4">
        {!loading && orders.length === 0 ? (
          <div className="rounded-[20px] border border-white/10 bg-black/20 p-6 text-sm text-white/60">
            Aucune commande pour ces filtres.
          </div>
        ) : null}

        {orders.map((order) => (
          <article key={order.id} className="rounded-[24px] border border-white/10 bg-black/20 p-5">
            <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
              <div>
                <p className="text-sm text-[#C8A45C]">{order.id}</p>
                <p className="mt-1 font-semibold text-white">{order.customer}</p>
                <p className="mt-1 text-xs text-white/50">
                  {order.channel} • {new Date(order.createdAt).toLocaleString('fr-FR')}
                </p>
                {order.invoiceNumber ? (
                  <p className="mt-1 text-xs text-sky-300">Facture: {order.invoiceNumber}</p>
                ) : null}
                {order.shipment ? (
                  <p className="mt-1 text-xs text-emerald-300">
                    {order.shipment.carrier} • {order.shipment.trackingNumber} • {order.shipment.shippingStatus}
                  </p>
                ) : null}
              </div>

              <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
                <span className="text-sm font-semibold text-white">{order.total}</span>
                <select
                  value={order.status}
                  disabled={updatingId === order.id}
                  onChange={(event) => onStatusChange(order.id, event.target.value as AdminOrderStatus)}
                  className="rounded-full border border-white/10 bg-black/40 px-3 py-2 text-xs text-white disabled:opacity-50"
                >
                  {statusOptions.map((status) => (
                    <option key={status} value={status}>{status}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className="mt-4 overflow-hidden rounded-2xl border border-white/10">
              <table className="min-w-full text-left text-xs text-white/75">
                <thead className="bg-white/5 text-white/55">
                  <tr>
                    <th className="px-3 py-2">Article</th>
                    <th className="px-3 py-2">SKU</th>
                    <th className="px-3 py-2">Qte</th>
                    <th className="px-3 py-2">PU</th>
                    <th className="px-3 py-2">Total</th>
                  </tr>
                </thead>
                <tbody>
                  {order.items.map((item) => (
                    <tr key={item.id} className="border-t border-white/10">
                      <td className="px-3 py-2">{item.name} • {item.variantName}</td>
                      <td className="px-3 py-2">{item.sku}</td>
                      <td className="px-3 py-2">{item.quantity}</td>
                      <td className="px-3 py-2">{(item.unitPriceCents / 100).toFixed(2)} €</td>
                      <td className="px-3 py-2">{(item.totalPriceCents / 100).toFixed(2)} €</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
