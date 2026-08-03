"use client";

import { useCallback, useEffect, useState } from 'react';
import type { AdminOrder, AdminOrdersFilters, AdminOrderStatus } from '../types/admin-order.types';

const defaultFilters: AdminOrdersFilters = {
  channel: 'ALL',
  status: 'ALL',
};

export function useAdminOrders() {
  const [orders, setOrders] = useState<AdminOrder[]>([]);
  const [filters, setFilters] = useState<AdminOrdersFilters>(defaultFilters);
  const [loading, setLoading] = useState(true);
  const [updatingId, setUpdatingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const loadOrders = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const search = new URLSearchParams();
      search.set('limit', '100');
      if (filters.channel !== 'ALL') search.set('channel', filters.channel);
      if (filters.status !== 'ALL') search.set('status', filters.status);

      const response = await fetch(`/api/admin/orders?${search.toString()}`);
      if (!response.ok) {
        throw new Error('Failed to load orders');
      }

      const data = (await response.json()) as AdminOrder[];
      setOrders(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  }, [filters.channel, filters.status]);

  useEffect(() => {
    void loadOrders();
  }, [loadOrders]);

  const updateStatus = async (orderId: string, status: AdminOrderStatus) => {
    setUpdatingId(orderId);
    setError(null);

    try {
      const response = await fetch(`/api/admin/orders/${orderId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ status }),
      });

      if (!response.ok) {
        const body = await response.json().catch(() => ({ message: 'Failed to update order status' }));
        throw new Error(body.message ?? 'Failed to update order status');
      }

      setOrders((current) => current.map((order) => (order.id === orderId ? { ...order, status } : order)));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setUpdatingId(null);
    }
  };

  return {
    orders,
    filters,
    setFilters,
    loading,
    updatingId,
    error,
    refresh: loadOrders,
    updateStatus,
  };
}
