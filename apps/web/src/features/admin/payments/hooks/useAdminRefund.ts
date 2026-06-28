"use client";

import { useState } from 'react';

type RefundResult = {
  provider: 'stripe' | 'paypal';
  reference: string;
  status: 'succeeded' | 'pending';
};

export function useAdminRefund() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<RefundResult | null>(null);

  const submitRefund = async (input: { orderId: string; amountCents?: number; reason?: string }) => {
    setLoading(true);
    setError(null);
    setResult(null);

    try {
      const response = await fetch('/api/admin/payments/refund', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(input),
      });

      const payload = (await response.json()) as RefundResult | { message?: string };

      if (!response.ok) {
        throw new Error('message' in payload && payload.message ? payload.message : 'Refund request failed');
      }

      setResult(payload as RefundResult);
      return true;
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown refund error');
      return false;
    } finally {
      setLoading(false);
    }
  };

  return {
    loading,
    error,
    result,
    submitRefund,
  };
}
