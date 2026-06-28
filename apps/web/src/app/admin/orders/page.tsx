"use client";

import AdminSidebar from '@/features/admin/components/AdminSidebar';
import AdminOrdersTable from '@/features/admin/orders/components/AdminOrdersTable';
import AdminInvoicePanel from '@/features/admin/orders/components/AdminInvoicePanel';
import AdminShipOrderPanel from '@/features/admin/orders/components/AdminShipOrderPanel';
import { useAdminOrders } from '@/features/admin/orders/hooks/useAdminOrders';

export default function AdminOrdersPage() {
  const { orders, filters, setFilters, loading, updatingId, error, updateStatus, refresh } = useAdminOrders();

  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-8 text-white md:px-10 lg:px-16">
      <div className="grid gap-6 lg:grid-cols-[280px_1fr]">
        <AdminSidebar />

        <div className="space-y-6">
          <section className="rounded-[36px] border border-white/10 bg-gradient-to-br from-[#C8A45C]/20 via-black/80 to-black p-8">
            <p className="text-sm uppercase tracking-[0.35em] text-[#C8A45C]">Admin Commandes</p>
            <h1 className="mt-4 text-4xl font-black md:text-5xl">Commandes unifiees</h1>
            <p className="mt-4 max-w-3xl text-white/70">
              Visualise les commandes ONLINE et POS, filtre par canal/statut, consulte les lignes et mets a jour le statut de traitement.
            </p>
          </section>

          <AdminOrdersTable
            orders={orders}
            filters={filters}
            loading={loading}
            error={error}
            updatingId={updatingId}
            onFilterChange={(next) => setFilters((current) => ({ ...current, ...next }))}
            onStatusChange={updateStatus}
          />

          <AdminInvoicePanel orders={orders} />

          <AdminShipOrderPanel orders={orders} onShipped={refresh} />
        </div>
      </div>
    </main>
  );
}
