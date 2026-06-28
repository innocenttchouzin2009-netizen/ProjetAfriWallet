import AdminSidebar from '@/features/admin/components/AdminSidebar';
import AdminStatsCards from '@/features/admin/components/AdminStatsCards';
import AdminRecentOrders from '@/features/admin/components/AdminRecentOrders';
import AdminProductionQueue from '@/features/admin/components/AdminProductionQueue';

export default function AdminPage() {
  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-8 text-white md:px-10 lg:px-16">
      <div className="grid gap-6 lg:grid-cols-[280px_1fr]">
        <AdminSidebar />

        <div className="space-y-6">
          <section className="rounded-[36px] border border-white/10 bg-gradient-to-br from-[#C8A45C]/20 via-black/80 to-black p-8">
            <p className="text-sm uppercase tracking-[0.35em] text-[#C8A45C]">Admin v1</p>
            <h1 className="mt-4 text-4xl font-black md:text-5xl">Back-office unifié</h1>
            <p className="mt-4 max-w-2xl text-white/70">
              Vente en ligne, boutique physique et stock partagé dans une seule vue de pilotage.
            </p>
          </section>

          <AdminStatsCards />

          <div className="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
            <AdminRecentOrders />
            <AdminProductionQueue />
          </div>
        </div>
      </div>
    </main>
  );
}
