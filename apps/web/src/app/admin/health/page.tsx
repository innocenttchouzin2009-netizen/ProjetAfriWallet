"use client";

import { useEffect, useState } from 'react';
import AdminSidebar from '@/features/admin/components/AdminSidebar';

type HealthPayload = {
  status: string;
  service: string;
  uptimeSeconds: number;
  db: string;
  latencyMs: number;
  timestamp: string;
  error?: string;
};

export default function AdminHealthPage() {
  const [data, setData] = useState<HealthPayload | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const run = async () => {
      setLoading(true);
      setError(null);

      try {
        const response = await fetch('/api/health');
        const payload = (await response.json()) as HealthPayload;

        if (!response.ok) {
          throw new Error(payload.error ?? 'Health check failed');
        }

        setData(payload);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unknown error');
      } finally {
        setLoading(false);
      }
    };

    run();
    const id = window.setInterval(run, 15000);
    return () => window.clearInterval(id);
  }, []);

  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-8 text-white md:px-10 lg:px-16">
      <div className="grid gap-6 lg:grid-cols-[280px_1fr]">
        <AdminSidebar />

        <div className="space-y-6">
          <section className="rounded-[36px] border border-white/10 bg-gradient-to-br from-[#C8A45C]/20 via-black/80 to-black p-8">
            <p className="text-sm uppercase tracking-[0.35em] text-[#C8A45C]">Observabilite</p>
            <h1 className="mt-4 text-4xl font-black md:text-5xl">Health Dashboard</h1>
            <p className="mt-4 max-w-3xl text-white/70">Surveillance en temps reel de l'etat applicatif et de la connectivite PostgreSQL.</p>
          </section>

          {loading ? <p className="text-sm text-white/70">Verification en cours...</p> : null}
          {error ? <p className="text-sm text-[#F0B86E]">Erreur: {error}</p> : null}

          {data ? (
            <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
              <div className="rounded-[24px] border border-white/10 bg-white/5 p-5">
                <p className="text-sm text-white/60">Etat</p>
                <p className="mt-2 text-3xl font-black text-white">{data.status}</p>
              </div>
              <div className="rounded-[24px] border border-white/10 bg-white/5 p-5">
                <p className="text-sm text-white/60">Database</p>
                <p className="mt-2 text-3xl font-black text-white">{data.db}</p>
              </div>
              <div className="rounded-[24px] border border-white/10 bg-white/5 p-5">
                <p className="text-sm text-white/60">Latence</p>
                <p className="mt-2 text-3xl font-black text-white">{data.latencyMs} ms</p>
              </div>
              <div className="rounded-[24px] border border-white/10 bg-white/5 p-5">
                <p className="text-sm text-white/60">Uptime</p>
                <p className="mt-2 text-3xl font-black text-white">{Math.floor(data.uptimeSeconds / 60)} min</p>
              </div>
            </section>
          ) : null}
        </div>
      </div>
    </main>
  );
}
