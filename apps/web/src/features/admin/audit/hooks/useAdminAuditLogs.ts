"use client";

import { useCallback, useEffect, useState } from 'react';
import type { AdminAuditFilters, AdminAuditLog } from '../types/admin-audit.types';

const defaultFilters: AdminAuditFilters = {
  action: 'ALL',
  entity: 'ALL',
  period: '7d',
};

export function useAdminAuditLogs() {
  const [logs, setLogs] = useState<AdminAuditLog[]>([]);
  const [actions, setActions] = useState<string[]>([]);
  const [entities, setEntities] = useState<string[]>([]);
  const [filters, setFilters] = useState<AdminAuditFilters>(defaultFilters);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const search = new URLSearchParams();
      search.set('action', filters.action);
      search.set('entity', filters.entity);
      search.set('period', filters.period);
      search.set('limit', '200');

      const response = await fetch(`/api/admin/audit?${search.toString()}`);
      if (!response.ok) {
        throw new Error('Failed to load audit logs');
      }

      const data = await response.json();
      setLogs(Array.isArray(data.logs) ? data.logs : []);
      setActions(Array.isArray(data.actions) ? data.actions : []);
      setEntities(Array.isArray(data.entities) ? data.entities : []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  }, [filters.action, filters.entity, filters.period]);

  useEffect(() => {
    void load();
  }, [load]);

  return {
    logs,
    actions,
    entities,
    filters,
    setFilters,
    loading,
    error,
    refresh: load,
  };
}
