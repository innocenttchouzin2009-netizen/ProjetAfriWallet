"use client";

import { useEffect, useState } from 'react';
import Link from 'next/link';
import AdminSidebar from '@/features/admin/components/AdminSidebar';

type SlaThresholdPayload = {
  thresholdPct: number;
};

export default function AdminSettingsPage() {
  const [thresholdPct, setThresholdPct] = useState(15);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;

    const load = async () => {
      setLoading(true);
      setMessage(null);

      try {
        const response = await fetch('/api/admin/settings/sla-threshold');
        if (!response.ok) {
          throw new Error('Impossible de charger les paramètres SLA.');
        }

        const payload = (await response.json()) as SlaThresholdPayload;
        if (mounted) {
          setThresholdPct(payload.thresholdPct);
        }
      } catch (error) {
        if (mounted) {
          setMessage(error instanceof Error ? error.message : 'Erreur paramètres.');
        }
      } finally {
        if (mounted) {
          setLoading(false);
        }
      }
    };

    void load();

    return () => {
      mounted = false;
    };
  }, []);

  const save = async () => {
    setSaving(true);
    setMessage(null);

    try {
      const response = await fetch('/api/admin/settings/sla-threshold', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ thresholdPct }),
      });

      if (!response.ok) {
        throw new Error('Impossible de sauvegarder le seuil SLA.');
      }

      const payload = (await response.json()) as SlaThresholdPayload;
      setThresholdPct(payload.thresholdPct);
      setMessage('Seuil SLA sauvegardé.');
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erreur sauvegarde.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-8 text-white md:px-10 lg:px-16">
      <div className="grid gap-6 lg:grid-cols-[280px_1fr]">
        <AdminSidebar />

        <div className="space-y-6">
          <section className="rounded-[36px] border border-white/10 bg-gradient-to-br from-[#C8A45C]/20 via-black/80 to-black p-8">
            <p className="text-sm uppercase tracking-[0.35em] text-[#C8A45C]">Paramètres</p>
            <h1 className="mt-4 text-4xl font-black md:text-5xl">Configuration opérationnelle</h1>
            <p className="mt-4 max-w-3xl text-white/70">
              Gère les seuils d’alerte du dashboard et les règles de monitoring business.
            </p>
          </section>

          <section className="rounded-[28px] border border-white/10 bg-white/5 p-6">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <p className="text-sm uppercase tracking-[0.25em] text-[#C8A45C]">SLA</p>
                <h2 className="mt-2 text-2xl font-black text-white">Seuil alerte retard</h2>
              </div>
              <Link href="/admin/health" className="rounded-full border border-white/10 px-4 py-2 text-sm text-white/80">
                Voir santé système
              </Link>
            </div>

            <p className="mt-4 text-sm text-white/60">
              Déclenche une alerte lorsque le taux de commandes en retard dépasse ce pourcentage.
            </p>

            <div className="mt-5 flex flex-wrap items-center gap-3">
              <label className="flex items-center gap-2 rounded-full border border-white/10 bg-black/30 px-3 py-2 text-sm text-white/70">
                Seuil SLA (%)
                <input
                  type="number"
                  min={1}
                  max={100}
                  value={thresholdPct}
                  onChange={(event) => setThresholdPct(Math.max(1, Math.min(100, Number(event.target.value) || 1)))}
                  className="w-16 rounded-full border border-white/10 bg-black/20 px-2 py-1 text-center text-white"
                />
              </label>

              <button
                onClick={() => void save()}
                disabled={loading || saving}
                className="rounded-full border border-[#C8A45C]/40 bg-[#C8A45C]/10 px-5 py-2 text-sm font-semibold text-[#F5E0AC] disabled:opacity-60"
              >
                {saving ? 'Sauvegarde...' : 'Sauvegarder'}
              </button>
            </div>

            {loading ? <p className="mt-4 text-sm text-white/60">Chargement des paramètres...</p> : null}
            {message ? <p className="mt-4 text-sm text-[#F0B86E]">{message}</p> : null}
          </section>

          <section className="rounded-[28px] border border-white/10 bg-white/5 p-6">
            <p className="text-sm uppercase tracking-[0.25em] text-[#C8A45C]">Webhook alerte</p>
            <h2 className="mt-2 text-2xl font-black text-white">Notification automatique</h2>
            <p className="mt-4 text-sm text-white/60">
              Configure la variable serveur ADMIN_ALERT_WEBHOOK_URL pour recevoir un POST JSON lors d’une nouvelle alerte SLA quotidienne.
            </p>
          </section>
        </div>
      </div>
    </main>
  );
}
