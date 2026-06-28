"use client";

import { useMemo, useState } from 'react';
import type { POSLineItem, POSPaymentMethod } from '../types/pos.types';
import { paymentMethods, posProducts } from '../data/pos.data';

export function usePOS() {
  const [items, setItems] = useState<POSLineItem[]>([]);
  const [discount, setDiscount] = useState(0);
  const [selectedPayment, setSelectedPayment] = useState<POSPaymentMethod['id']>('cash');

  const addItem = (productId: string) => {
    const product = posProducts.find((entry) => entry.id === productId);
    if (!product) return;

    setItems((current) => {
      const existing = current.find((entry) => entry.productId === productId);
      if (existing) {
        return current.map((entry) =>
          entry.productId === productId ? { ...entry, quantity: entry.quantity + 1 } : entry,
        );
      }

      return [
        ...current,
        { productId, sku: product.sku, name: product.name, price: product.price, quantity: 1 },
      ];
    });
  };

  const removeItem = (productId: string) => {
    setItems((current) => current.filter((entry) => entry.productId !== productId));
  };

  const updateQuantity = (productId: string, quantity: number) => {
    if (quantity <= 0) {
      removeItem(productId);
      return;
    }

    setItems((current) =>
      current.map((entry) => (entry.productId === productId ? { ...entry, quantity } : entry)),
    );
  };

  const subtotal = useMemo(() => items.reduce((sum, item) => sum + item.price * item.quantity, 0), [items]);
  const total = useMemo(() => Math.max(0, subtotal - discount), [subtotal, discount]);

  const resetTicket = () => {
    setItems([]);
    setDiscount(0);
    setSelectedPayment('cash');
  };

  return {
    items,
    subtotal,
    discount,
    total,
    selectedPayment,
    setDiscount,
    setSelectedPayment,
    addItem,
    removeItem,
    updateQuantity,
    resetTicket,
    paymentMethods,
  };
}
