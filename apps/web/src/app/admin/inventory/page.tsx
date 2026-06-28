"use client";

import AdminSidebar from '@/features/admin/components/AdminSidebar';
import AdminInventoryTable from '@/features/admin/inventory/components/AdminInventoryTable';
import { useAdminInventory } from '@/features/admin/inventory/hooks/useAdminInventory';

export default function AdminInventoryPage() {
  const { items, loading, error, adjustingVariantId, adjust, stats } = useAdminInventory();

  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-8 text-white md:px-10 lg:px-16">
      <div className="grid gap-6 lg:grid-cols-[280px_1fr]">
        <AdminSidebar />

        <div className="space-y-6">
          <section className="rounded-[36px] border border-white/10 bg-gradient-to-br from-[#C8A45C]/20 via-black/80 to-black p-8">
            <p className="text-sm uppercase tracking-[0.35em] text-[#C8A45C]">Admin Inventaire</p>
            <h1 className="mt-4 text-4xl font-black md:text-5xl">Stock avance v1</h1>
            <p className="mt-4 max-w-3xl text-white/70">
              Historique des mouvements, ajustements manuels, alertes de stock faible et stock unifie boutique physique + online.
            </p>
          </section>

          <div className="grid gap-4 md:grid-cols-3">
            <div className="rounded-[24px] border border-white/10 bg-white/5 p-5">
              <p className="text-sm text-white/60">Variants suivis</p>
              <p className="mt-2 text-3xl font-black text-white">{stats.totalVariants}</p>
            </div>
            <div className="rounded-[24px] border border-white/10 bg-white/5 p-5">
              <p className="text-sm text-white/60">Unites en stock</p>
              <p className="mt-2 text-3xl font-black text-white">{stats.totalStockUnits}</p>
            </div>
            <div className="rounded-[24px] border border-white/10 bg-white/5 p-5">
              <p className="text-sm text-white/60">Alertes stock faible</p>
              <p className="mt-2 text-3xl font-black text-[#F0B86E]">{stats.lowStockCount}</p>
            </div>
          </div>

          {loading ? <p className="text-sm text-white/70">Chargement inventaire...</p> : null}
          {error ? <p className="text-sm text-[#F0B86E]">Erreur: {error}</p> : null}

          <AdminInventoryTable items={items} adjustingVariantId={adjustingVariantId} onAdjust={adjust} />
        </div>
      </div>
    </main>
  );
}
