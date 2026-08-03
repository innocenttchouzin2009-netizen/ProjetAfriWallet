"use client";

import { useEffect, useMemo, useState } from 'react';
import type { AdminPromotion, AdminPromotionFormValues } from '../types/admin-promotion.types';

const emptyPromotionForm: AdminPromotionFormValues = {
  code: '',
  discountType: 'PERCENTAGE',
  discountValue: 10,
  minPurchase: 0,
  usageLimit: 0,
  startsAt: '',
  endsAt: '',
  active: true,
  appliesToAll: true,
  scope: 'all',
  category: '',
  collectionSlug: '',
};

function toDateTimeInputValue(iso: string) {
  const date = new Date(iso);
  const offsetMs = date.getTimezoneOffset() * 60000;
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16);
}

function payloadFromValues(values: AdminPromotionFormValues) {
  return {
    code: values.code.trim(),
    discountType: values.discountType,
    discountValue: Number(values.discountValue),
    minPurchase: values.minPurchase > 0 ? Number(values.minPurchase) : undefined,
    usageLimit: values.usageLimit > 0 ? Number(values.usageLimit) : undefined,
    startsAt: new Date(values.startsAt).toISOString(),
    endsAt: new Date(values.endsAt).toISOString(),
    active: values.active,
    appliesToAll: values.scope === 'all',
    scope: values.scope,
    category: values.scope === 'category' ? values.category.trim() : undefined,
    collectionSlug: values.scope === 'collection' ? values.collectionSlug.trim() : undefined,
  };
}

export function useAdminPromotions() {
  const [promotions, setPromotions] = useState<AdminPromotion[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formValues, setFormValues] = useState<AdminPromotionFormValues>(emptyPromotionForm);

  const loadPromotions = async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/admin/promotions');
      if (!response.ok) {
        throw new Error('Failed to load promotions');
      }

      const data = (await response.json()) as AdminPromotion[];
      setPromotions(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadPromotions();
  }, []);

  const resetForm = () => {
    setEditingId(null);
    setFormValues(emptyPromotionForm);
  };

  const createPromotion = async () => {
    if (!formValues.code.trim() || !formValues.startsAt || !formValues.endsAt) return;

    const response = await fetch('/api/admin/promotions', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payloadFromValues(formValues)),
    });

    if (response.ok) {
      await loadPromotions();
      resetForm();
      return;
    }

    const body = await response.json().catch(() => ({ message: 'Failed to create promotion' }));
    setError(body.message ?? 'Failed to create promotion');
  };

  const updatePromotion = async (id: string) => {
    if (!formValues.code.trim() || !formValues.startsAt || !formValues.endsAt) return;

    const response = await fetch(`/api/admin/promotions/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payloadFromValues(formValues)),
    });

    if (response.ok) {
      await loadPromotions();
      resetForm();
      return;
    }

    const body = await response.json().catch(() => ({ message: 'Failed to update promotion' }));
    setError(body.message ?? 'Failed to update promotion');
  };

  const removePromotion = async (id: string) => {
    const response = await fetch(`/api/admin/promotions/${id}`, { method: 'DELETE' });
    if (!response.ok) {
      const body = await response.json().catch(() => ({ message: 'Failed to delete promotion' }));
      setError(body.message ?? 'Failed to delete promotion');
      return;
    }

    await loadPromotions();
  };

  const startEditing = (promotion: AdminPromotion) => {
    setEditingId(promotion.id);
    setFormValues({
      code: promotion.code,
      discountType: promotion.discountType,
      discountValue: promotion.discountValue,
      minPurchase: promotion.minPurchase ?? 0,
      usageLimit: promotion.usageLimit ?? 0,
      startsAt: toDateTimeInputValue(promotion.startsAt),
      endsAt: toDateTimeInputValue(promotion.endsAt),
      active: promotion.active,
      appliesToAll: promotion.appliesToAll,
      scope: promotion.scope,
      category: promotion.category ?? '',
      collectionSlug: promotion.collectionSlug ?? '',
    });
  };

  const stats = useMemo(() => {
    const activeCount = promotions.filter((promotion) => promotion.active).length;
    const categoryScoped = promotions.filter((promotion) => promotion.scope === 'category').length;

    return {
      total: promotions.length,
      activeCount,
      categoryScoped,
    };
  }, [promotions]);

  return {
    promotions,
    loading,
    error,
    editingId,
    formValues,
    setFormValues,
    createPromotion,
    updatePromotion,
    removePromotion,
    startEditing,
    resetForm,
    stats,
  };
}
