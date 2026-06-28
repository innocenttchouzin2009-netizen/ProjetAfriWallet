"use client";

import AdminSidebar from '@/features/admin/components/AdminSidebar';
import AdminAuditTable from '@/features/admin/audit/components/AdminAuditTable';
import { useAdminAuditLogs } from '@/features/admin/audit/hooks/useAdminAuditLogs';

export default function AdminAuditPage() {
  const { logs, actions, entities, filters, setFilters, loading, error } = useAdminAuditLogs();

  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-8 text-white md:px-10 lg:px-16">
      <div className="grid gap-6 lg:grid-cols-[280px_1fr]">
        <AdminSidebar />

        <div className="space-y-6">
          <section className="rounded-[36px] border border-white/10 bg-gradient-to-br from-[#C8A45C]/20 via-black/80 to-black p-8">
            <p className="text-sm uppercase tracking-[0.35em] text-[#C8A45C]">Admin Audit</p>
            <h1 className="mt-4 text-4xl font-black md:text-5xl">Audit Logs</h1>
            <p className="mt-4 max-w-3xl text-white/70">
              Suivi complet des actions sensibles: commandes, produits, statuts et inscriptions.
            </p>
          </section>

          <AdminAuditTable
            logs={logs}
            actions={actions}
            entities={entities}
            filters={filters}
            loading={loading}
            error={error}
            onChangeFilters={(next) => setFilters((current) => ({ ...current, ...next }))}
          />
        </div>
      </div>
    </main>
  );
}
