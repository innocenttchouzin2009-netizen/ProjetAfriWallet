export type AdminAuditPeriod = '24h' | '7d' | '30d' | 'all';

export interface AdminAuditLog {
  id: string;
  action: string;
  entity: string;
  entityId?: string | null;
  userId?: string | null;
  ipAddress?: string | null;
  payload?: Record<string, unknown> | null;
  createdAt: string;
}

export interface AdminAuditFilters {
  action: string;
  entity: string;
  period: AdminAuditPeriod;
}
