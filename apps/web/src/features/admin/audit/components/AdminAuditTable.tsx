"use client";

import type { AdminAuditFilters, AdminAuditLog, AdminAuditPeriod } from '../types/admin-audit.types';

type Props = {
  logs: AdminAuditLog[];
  actions: string[];
  entities: string[];
  filters: AdminAuditFilters;
  loading: boolean;
  error: string | null;
  onChangeFilters: (next: Partial<AdminAuditFilters>) => void;
};

const periods: { value: AdminAuditPeriod; label: string }[] = [
  { value: '24h', label: '24h' },
  { value: '7d', label: '7 jours' },
  { value: '30d', label: '30 jours' },
  { value: 'all', label: 'Tout' },
];

function formatPayload(payload: AdminAuditLog['payload']) {
  if (!payload) return '-';
  return JSON.stringify(payload);
}

function toCsvValue(value: string) {
  return `"${value.replace(/"/g, '""')}"`;
}

function buildExportFileName() {
  const now = new Date();
  const yyyy = now.getFullYear();
  const mm = String(now.getMonth() + 1).padStart(2, '0');
  const dd = String(now.getDate()).padStart(2, '0');
  const hh = String(now.getHours()).padStart(2, '0');
  const min = String(now.getMinutes()).padStart(2, '0');

  return `audit-logs-${yyyy}-${mm}-${dd}-${hh}${min}.csv`;
}

export default function AdminAuditTable({
  logs,
  actions,
  entities,
  filters,
  loading,
  error,
  onChangeFilters,
}: Props) {
  const exportCsv = () => {
    const header = ['createdAt', 'action', 'entity', 'entityId', 'userId', 'ipAddress', 'payload'];

    const rows = logs.map((log) => [
      log.createdAt,
      log.action,
      log.entity,
      log.entityId ?? '',
      log.userId ?? '',
      log.ipAddress ?? '',
      formatPayload(log.payload),
    ]);

    const csv = [header, ...rows]
      .map((row) => row.map((cell) => toCsvValue(String(cell))).join(','))
      .join('\n');

    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.setAttribute('download', buildExportFileName());
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);
    URL.revokeObjectURL(url);
  };

  const copyPayload = async (payload: AdminAuditLog['payload']) => {
    const value = formatPayload(payload);
    if (value === '-') return;

    if (navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(value);
      return;
    }

    const area = document.createElement('textarea');
    area.value = value;
    area.style.position = 'fixed';
    area.style.left = '-9999px';
    document.body.appendChild(area);
    area.select();
    document.execCommand('copy');
    document.body.removeChild(area);
  };

  return (
    <section className="rounded-[32px] border border-white/10 bg-white/5 p-6">
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <p className="text-sm uppercase tracking-[0.3em] text-[#C8A45C]">Audit</p>
          <h2 className="mt-2 text-2xl font-black text-white">Journal des actions</h2>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <select
            value={filters.action}
            onChange={(event) => onChangeFilters({ action: event.target.value })}
            className="rounded-full border border-white/10 bg-black/30 px-4 py-2 text-sm text-white"
          >
            <option value="ALL">Action: Toutes</option>
            {actions.map((action) => (
              <option key={action} value={action}>{action}</option>
            ))}
          </select>

          <select
            value={filters.entity}
            onChange={(event) => onChangeFilters({ entity: event.target.value })}
            className="rounded-full border border-white/10 bg-black/30 px-4 py-2 text-sm text-white"
          >
            <option value="ALL">Entite: Toutes</option>
            {entities.map((entity) => (
              <option key={entity} value={entity}>{entity}</option>
            ))}
          </select>

          <select
            value={filters.period}
            onChange={(event) => onChangeFilters({ period: event.target.value as AdminAuditPeriod })}
            className="rounded-full border border-white/10 bg-black/30 px-4 py-2 text-sm text-white"
          >
            {periods.map((period) => (
              <option key={period.value} value={period.value}>{period.label}</option>
            ))}
          </select>

          <button
            type="button"
            onClick={exportCsv}
            disabled={logs.length === 0}
            className="rounded-full border border-white/10 bg-black/30 px-4 py-2 text-sm text-white disabled:cursor-not-allowed disabled:opacity-40"
          >
            Export CSV
          </button>
        </div>
      </div>

      {loading ? <p className="mt-6 text-sm text-white/70">Chargement des logs...</p> : null}
      {error ? <p className="mt-6 text-sm text-[#F0B86E]">Erreur: {error}</p> : null}

      <div className="mt-6 overflow-hidden rounded-2xl border border-white/10">
        <table className="min-w-full text-left text-xs text-white/80">
          <thead className="bg-white/5 text-white/60">
            <tr>
              <th className="px-3 py-2">Date</th>
              <th className="px-3 py-2">Action</th>
              <th className="px-3 py-2">Entite</th>
              <th className="px-3 py-2">Entity ID</th>
              <th className="px-3 py-2">User ID</th>
              <th className="px-3 py-2">Payload</th>
              <th className="px-3 py-2">Action</th>
            </tr>
          </thead>
          <tbody>
            {!loading && logs.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-3 py-6 text-center text-white/60">
                  Aucun evenement pour ce filtre.
                </td>
              </tr>
            ) : (
              logs.map((log) => (
                <tr key={log.id} className="border-t border-white/10">
                  <td className="px-3 py-2">{new Date(log.createdAt).toLocaleString('fr-FR')}</td>
                  <td className="px-3 py-2">{log.action}</td>
                  <td className="px-3 py-2">{log.entity}</td>
                  <td className="px-3 py-2">{log.entityId ?? '-'}</td>
                  <td className="px-3 py-2">{log.userId ?? '-'}</td>
                  <td className="max-w-[340px] truncate px-3 py-2" title={formatPayload(log.payload)}>
                    {formatPayload(log.payload)}
                  </td>
                  <td className="px-3 py-2">
                    <button
                      type="button"
                      onClick={() => void copyPayload(log.payload)}
                      disabled={!log.payload}
                      className="rounded-full border border-white/10 bg-black/40 px-3 py-1 text-[11px] text-white disabled:cursor-not-allowed disabled:opacity-40"
                    >
                      Copier JSON
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}
