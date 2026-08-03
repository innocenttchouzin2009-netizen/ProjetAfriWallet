"use client";

import { useEffect, useMemo, useState } from 'react';

type DashboardPayload = {
  kpis: {
    revenueTodayCents: number;
    revenueChangePct: number;
    revenueWeekCents: number;
    revenueMonthCents: number;
    revenueYearCents: number;
    pendingOrders: number;
    inProductionOrders: number;
    readyOrders: number;
    shippedOrders: number;
    deliveredOrders: number;
    lateOrders: number;
    newCustomersToday: number;
    lowStockCount: number;
  };
  sla: {
    pendingAvgHours: number;
    productionAvgHours: number;
    readyAvgHours: number;
    overdueCount: number;
    activePipelineCount: number;
    breachRatePct: number;
  };
  topProducts: Array<{
    variantId: string;
    name: string;
    variantName: string;
    sku: string;
    units: number;
    revenueCents: number;
  }>;
  topCollections: Array<{
    slug: string;
    label: string;
    revenueCents: number;
    units: number;
    deltaPct: number;
  }>;
  salesByCountry: Array<{
    country: string;
    revenueCents: number;
    orders: number;
    deltaPct: number;
  }>;
  settings: {
    slaThresholdPct: number;
    slaAlert: boolean;
  };
  range: 'day' | 'week' | 'month' | 'year';
  trend: Array<{
    key: string;
    label: string;
    revenueCents: number;
  }>;
};

function formatEuro(cents: number) {
  return new Intl.NumberFormat('fr-FR', {
    style: 'currency',
    currency: 'EUR',
    maximumFractionDigits: 0,
  }).format(cents / 100);
}

function formatDelta(value: number) {
  return `${value >= 0 ? '+' : ''}${value.toFixed(1)}%`;
}

function escapeCsvValue(value: string | number) {
  const raw = String(value);
  if (!raw.includes(',') && !raw.includes('"') && !raw.includes('\n')) {
    return raw;
  }
  return `"${raw.replace(/"/g, '""')}"`;
}

function exportCsv(fileName: string, headers: string[], rows: Array<Array<string | number>>) {
  const content = [headers, ...rows].map((row) => row.map((cell) => escapeCsvValue(cell)).join(',')).join('\n');
  const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

export default function AdminBusinessDashboard() {
  const [data, setData] = useState<DashboardPayload | null>(null);
  const [range, setRange] = useState<'day' | 'week' | 'month' | 'year'>('week');
  const [slaThresholdPct, setSlaThresholdPct] = useState(15);
  const [isThresholdDirty, setIsThresholdDirty] = useState(false);
  const [savingThreshold, setSavingThreshold] = useState(false);
  const [saveMessage, setSaveMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;

    const load = async () => {
      setLoading(true);
      setError(null);

      try {
        const response = await fetch(`/api/admin/dashboard?range=${range}`);
        if (!response.ok) {
          throw new Error('Impossible de charger le dashboard');
        }

        const payload = (await response.json()) as DashboardPayload;
        if (mounted) {
          setData(payload);
          if (!isThresholdDirty) {
            setSlaThresholdPct(payload.settings.slaThresholdPct);
          }
          setLoading(false);
        }
      } catch (err) {
        if (mounted) {
          setError(err instanceof Error ? err.message : 'Erreur inconnue');
          setLoading(false);
        }
      }
    };

    void load();

    return () => {
      mounted = false;
    };
  }, [range, isThresholdDirty]);

  const trendMax = useMemo(() => {
    if (!data?.trend.length) return 1;
    return Math.max(...data.trend.map((entry) => entry.revenueCents), 1);
  }, [data]);

  const slaAlert = data ? data.sla.breachRatePct >= slaThresholdPct : false;

  const saveThreshold = async () => {
    setSavingThreshold(true);
    setSaveMessage(null);

    try {
      const response = await fetch('/api/admin/settings/sla-threshold', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ thresholdPct: slaThresholdPct }),
      });

      if (!response.ok) {
        throw new Error('Echec sauvegarde seuil SLA');
      }

      setIsThresholdDirty(false);
      setSaveMessage('Seuil SLA sauvegarde.');

      const refreshed = await fetch(`/api/admin/dashboard?range=${range}`);
      if (refreshed.ok) {
        const payload = (await refreshed.json()) as DashboardPayload;
        setData(payload);
        setSlaThresholdPct(payload.settings.slaThresholdPct);
      }
    } catch (err) {
      setSaveMessage(err instanceof Error ? err.message : 'Echec sauvegarde');
    } finally {
      setSavingThreshold(false);
    }
  };

  if (loading) {
    return (
      <section className="rounded-[32px] border border-white/10 bg-white/5 p-6 text-sm text-white/70">
        Chargement des KPI business...
      </section>
    );
  }

  if (error || !data) {
    return (
      <section className="rounded-[32px] border border-[#F0B86E]/30 bg-[#F0B86E]/10 p-6 text-sm text-[#F0B86E]">
        {error ?? 'Impossible de charger le dashboard.'}
      </section>
    );
  }

  return (
    <section className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <select
            value={range}
            onChange={(event) => setRange(event.target.value as 'day' | 'week' | 'month' | 'year')}
            className="rounded-full border border-white/10 bg-black/30 px-4 py-2 text-sm text-white"
          >
            <option value="day">Jour</option>
            <option value="week">Semaine</option>
            <option value="month">Mois</option>
            <option value="year">Annee</option>
          </select>

          <label className="flex items-center gap-2 rounded-full border border-white/10 bg-black/30 px-3 py-2 text-xs text-white/70">
            Seuil SLA %
            <input
              type="number"
              min={1}
              max={100}
              value={slaThresholdPct}
              onChange={(event) => {
                setSlaThresholdPct(Math.max(1, Math.min(100, Number(event.target.value) || 1)));
                setIsThresholdDirty(true);
                setSaveMessage(null);
              }}
              className="w-14 rounded-full border border-white/10 bg-black/20 px-2 py-1 text-center text-white"
            />
          </label>
          <button
            onClick={() => void saveThreshold()}
            disabled={!isThresholdDirty || savingThreshold}
            className="rounded-full border border-[#C8A45C]/40 bg-[#C8A45C]/10 px-3 py-2 text-xs text-[#F5E0AC] disabled:opacity-50"
          >
            {savingThreshold ? 'Sauvegarde...' : 'Sauvegarder seuil'}
          </button>
          {saveMessage ? <span className="text-xs text-white/60">{saveMessage}</span> : null}
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <button
            onClick={() => exportCsv(
              `dashboard-top-produits-${range}.csv`,
              ['produit', 'variant', 'sku', 'unites', 'ca_eur'],
              data.topProducts.map((item) => [item.name, item.variantName, item.sku, item.units, (item.revenueCents / 100).toFixed(2)]),
            )}
            className="rounded-full border border-white/10 px-3 py-2 text-xs text-white/80"
          >
            Export produits CSV
          </button>
          <button
            onClick={() => exportCsv(
              `dashboard-collections-${range}.csv`,
              ['collection', 'ventes', 'ca_eur', 'delta_pct'],
              data.topCollections.map((item) => [item.label, item.units, (item.revenueCents / 100).toFixed(2), item.deltaPct.toFixed(1)]),
            )}
            className="rounded-full border border-white/10 px-3 py-2 text-xs text-white/80"
          >
            Export collections CSV
          </button>
          <button
            onClick={() => exportCsv(
              `dashboard-pays-${range}.csv`,
              ['pays', 'commandes', 'ca_eur', 'delta_pct'],
              data.salesByCountry.map((item) => [item.country, item.orders, (item.revenueCents / 100).toFixed(2), item.deltaPct.toFixed(1)]),
            )}
            className="rounded-full border border-white/10 px-3 py-2 text-xs text-white/80"
          >
            Export pays CSV
          </button>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-8">
        <article className="rounded-[24px] border border-white/10 bg-white/5 p-5">
          <p className="text-sm text-white/60">CA du jour</p>
          <p className="mt-2 text-3xl font-black text-white">{formatEuro(data.kpis.revenueTodayCents)}</p>
          <p className={`mt-2 text-sm ${data.kpis.revenueChangePct >= 0 ? 'text-[#8ED8A2]' : 'text-[#F0B86E]'}`}>
            {data.kpis.revenueChangePct >= 0 ? '+' : ''}{data.kpis.revenueChangePct}% vs hier
          </p>
        </article>

        <article className="rounded-[24px] border border-white/10 bg-white/5 p-5">
          <p className="text-sm text-white/60">Commandes en attente</p>
          <p className="mt-2 text-3xl font-black text-white">{data.kpis.pendingOrders}</p>
          <p className="mt-2 text-sm text-white/60">Statut CONFIRMED</p>
        </article>

        <article className="rounded-[24px] border border-white/10 bg-white/5 p-5">
          <p className="text-sm text-white/60">En production</p>
          <p className="mt-2 text-3xl font-black text-white">{data.kpis.inProductionOrders}</p>
          <p className="mt-2 text-sm text-white/60">Statut IN_PRODUCTION</p>
        </article>

        <article className="rounded-[24px] border border-white/10 bg-white/5 p-5">
          <p className="text-sm text-white/60">Expediees</p>
          <p className="mt-2 text-3xl font-black text-white">{data.kpis.shippedOrders}</p>
          <p className="mt-2 text-sm text-white/60">Statut SHIPPED</p>
        </article>

        <article className="rounded-[24px] border border-white/10 bg-white/5 p-5">
          <p className="text-sm text-white/60">Pretes</p>
          <p className="mt-2 text-3xl font-black text-white">{data.kpis.readyOrders}</p>
          <p className="mt-2 text-sm text-white/60">Statut READY</p>
        </article>

        <article className="rounded-[24px] border border-white/10 bg-white/5 p-5">
          <p className="text-sm text-white/60">Livrees</p>
          <p className="mt-2 text-3xl font-black text-white">{data.kpis.deliveredOrders}</p>
          <p className="mt-2 text-sm text-white/60">Statut DELIVERED</p>
        </article>

        <article className="rounded-[24px] border border-white/10 bg-white/5 p-5">
          <p className="text-sm text-white/60">Retard SLA</p>
          <p className="mt-2 text-3xl font-black text-white">{data.kpis.lateOrders}</p>
          <p className={`mt-2 text-sm ${slaAlert ? 'text-[#F06A6A]' : 'text-[#F0B86E]'}`}>&gt; 48h en attente/production</p>
        </article>

        <article className="rounded-[24px] border border-white/10 bg-white/5 p-5">
          <p className="text-sm text-white/60">Nouveaux clients</p>
          <p className="mt-2 text-3xl font-black text-white">{data.kpis.newCustomersToday}</p>
          <p className="mt-2 text-sm text-white/60">Aujourd&apos;hui</p>
        </article>
      </div>

      <div className="grid gap-4 xl:grid-cols-[1.1fr_0.9fr]">
        <article className="rounded-[28px] border border-white/10 bg-white/5 p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm uppercase tracking-[0.25em] text-[#C8A45C]">Performance</p>
              <h3 className="mt-2 text-2xl font-black text-white">Ventes et risque stock</h3>
            </div>
            <p className="rounded-full border border-[#F0B86E]/30 px-3 py-1 text-xs text-[#F0B86E]">
              {data.kpis.lowStockCount} SKU en tension
            </p>
          </div>

          <div className="mt-5 grid gap-3 md:grid-cols-3">
            <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
              <p className="text-xs uppercase tracking-[0.2em] text-white/50">Semaine</p>
              <p className="mt-2 text-xl font-black text-white">{formatEuro(data.kpis.revenueWeekCents)}</p>
            </div>
            <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
              <p className="text-xs uppercase tracking-[0.2em] text-white/50">Mois</p>
              <p className="mt-2 text-xl font-black text-white">{formatEuro(data.kpis.revenueMonthCents)}</p>
            </div>
            <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
              <p className="text-xs uppercase tracking-[0.2em] text-white/50">Annee</p>
              <p className="mt-2 text-xl font-black text-white">{formatEuro(data.kpis.revenueYearCents)}</p>
            </div>
          </div>

          <div className="mt-6">
            <p className="text-xs uppercase tracking-[0.2em] text-white/50">Tendance 7 jours</p>
            <div className="mt-3 grid grid-cols-7 gap-2">
              {data.trend.map((entry) => {
                const height = Math.max(12, Math.round((entry.revenueCents / trendMax) * 90));
                return (
                  <div key={entry.key} className="flex flex-col items-center gap-2">
                    <div className="w-full rounded-md bg-[#C8A45C]/20" style={{ height }} />
                    <span className="text-[10px] text-white/50">{entry.label}</span>
                  </div>
                );
              })}
            </div>
          </div>
        </article>

        <article className="rounded-[28px] border border-white/10 bg-white/5 p-6">
          <p className="text-sm uppercase tracking-[0.25em] text-[#C8A45C]">Top produits</p>
          <h3 className="mt-2 text-2xl font-black text-white">Best sellers</h3>
          <ul className="mt-4 space-y-3">
            {data.topProducts.map((item) => (
              <li key={item.variantId} className="rounded-2xl border border-white/10 bg-black/20 p-3">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <p className="font-semibold text-white">{item.name}</p>
                    <p className="text-xs text-white/50">{item.variantName} - {item.sku}</p>
                  </div>
                  <div className="text-right">
                    <p className="font-semibold text-white">{item.units} u.</p>
                    <p className="text-xs text-[#C8A45C]">{formatEuro(item.revenueCents)}</p>
                  </div>
                </div>
              </li>
            ))}
          </ul>
        </article>
      </div>

      <div className="grid gap-4 xl:grid-cols-[1fr_1fr]">
        <article className="rounded-[28px] border border-white/10 bg-white/5 p-6">
          <div className="flex items-center justify-between gap-3">
            <p className="text-sm uppercase tracking-[0.25em] text-[#C8A45C]">SLA logistique</p>
            <span className={`rounded-full px-3 py-1 text-xs ${slaAlert ? 'border border-[#F06A6A]/40 bg-[#F06A6A]/10 text-[#F06A6A]' : 'border border-[#8ED8A2]/40 bg-[#8ED8A2]/10 text-[#8ED8A2]'}`}>
              {slaAlert ? 'Alerte SLA' : 'SLA stable'}
            </span>
          </div>
          <h3 className="mt-2 text-2xl font-black text-white">Pipeline operations</h3>
          <div className="mt-4 grid gap-3 md:grid-cols-3">
            <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
              <p className="text-xs uppercase tracking-[0.2em] text-white/50">Attente</p>
              <p className="mt-2 text-xl font-black text-white">{data.sla.pendingAvgHours}h</p>
            </div>
            <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
              <p className="text-xs uppercase tracking-[0.2em] text-white/50">Production</p>
              <p className="mt-2 text-xl font-black text-white">{data.sla.productionAvgHours}h</p>
            </div>
            <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
              <p className="text-xs uppercase tracking-[0.2em] text-white/50">Ready</p>
              <p className="mt-2 text-xl font-black text-white">{data.sla.readyAvgHours}h</p>
            </div>
          </div>
          <div className={`mt-4 rounded-2xl p-4 text-sm ${slaAlert ? 'border border-[#F06A6A]/30 bg-[#F06A6A]/10 text-[#F06A6A]' : 'border border-[#8ED8A2]/30 bg-[#8ED8A2]/10 text-[#8ED8A2]'}`}>
            {data.sla.overdueCount} commandes en retard sur {data.sla.activePipelineCount} en pipeline ({data.sla.breachRatePct}%). Seuil: {slaThresholdPct}%.
          </div>
        </article>

        <article className="rounded-[28px] border border-white/10 bg-white/5 p-6">
          <p className="text-sm uppercase tracking-[0.25em] text-[#C8A45C]">Top collections</p>
          <h3 className="mt-2 text-2xl font-black text-white">Comparatif periode precedente</h3>
          <ul className="mt-4 space-y-3">
            {data.topCollections.map((entry) => (
              <li key={entry.slug} className="rounded-2xl border border-white/10 bg-black/20 p-3">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="font-semibold text-white">{entry.label}</p>
                    <p className="text-xs text-white/50">{entry.units} ventes</p>
                  </div>
                  <div className="text-right">
                    <p className="font-semibold text-white">{formatEuro(entry.revenueCents)}</p>
                    <p className={`text-xs ${entry.deltaPct >= 0 ? 'text-[#8ED8A2]' : 'text-[#F0B86E]'}`}>{formatDelta(entry.deltaPct)}</p>
                  </div>
                </div>
              </li>
            ))}
            {!data.topCollections.length ? <li className="text-sm text-white/60">Aucune vente collection sur la periode.</li> : null}
          </ul>
        </article>
      </div>

      <article className="rounded-[28px] border border-white/10 bg-white/5 p-6">
        <p className="text-sm uppercase tracking-[0.25em] text-[#C8A45C]">Ventes par pays</p>
        <h3 className="mt-2 text-2xl font-black text-white">Geo performance</h3>
        <div className="mt-4 grid gap-3 md:grid-cols-2">
          {data.salesByCountry.map((entry) => (
            <div key={entry.country} className="rounded-2xl border border-white/10 bg-black/20 p-4">
              <div className="flex items-center justify-between">
                <p className="font-semibold text-white">{entry.country}</p>
                <p className="text-sm text-white/60">{entry.orders} cmd</p>
              </div>
              <p className="mt-2 text-xl font-black text-white">{formatEuro(entry.revenueCents)}</p>
              <p className={`mt-1 text-xs ${entry.deltaPct >= 0 ? 'text-[#8ED8A2]' : 'text-[#F0B86E]'}`}>{formatDelta(entry.deltaPct)} vs periode precedente</p>
            </div>
          ))}
          {!data.salesByCountry.length ? <p className="text-sm text-white/60">Pas encore de donnees pays.</p> : null}
        </div>
      </article>
    </section>
  );
}
