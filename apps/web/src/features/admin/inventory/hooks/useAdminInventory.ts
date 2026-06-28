"use client";

import { useEffect, useMemo, useState } from 'react';
import type { InventoryItem } from '@/features/inventory/types/inventory.types';

type InventoryApiResponse = {
  items: InventoryItem[];
  lowStockCount: number;
  totalStockUnits: number;
};

export function useAdminInventory() {
  const [items, setItems] = useState<InventoryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [adjustingVariantId, setAdjustingVariantId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/admin/inventory?movementLimit=5');
      if (!response.ok) {
        throw new Error('Failed to load inventory');
      }

      const payload = (await response.json()) as InventoryApiResponse;
      setItems(payload.items);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown inventory error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const adjust = async (variantId: string, quantityDelta: number, reason?: string) => {
    setAdjustingVariantId(variantId);
    setError(null);

    try {
      const response = await fetch('/api/admin/inventory/adjust', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ variantId, quantityDelta, reason }),
      });

      if (!response.ok) {
        const body = await response.json().catch(() => ({ message: 'Adjustment failed' }));
        throw new Error(body.message ?? 'Adjustment failed');
      }

      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown inventory error');
    } finally {
      setAdjustingVariantId(null);
    }
  };

  const stats = useMemo(() => {
    const lowStockCount = items.filter((item) => item.lowStockAlert).length;
    const totalStockUnits = items.reduce((sum, item) => sum + item.stock, 0);

    return {
      totalVariants: items.length,
      lowStockCount,
      totalStockUnits,
    };
  }, [items]);

  return {
    items,
    loading,
    error,
    adjustingVariantId,
    adjust,
    refresh: load,
    stats,
  };
}
