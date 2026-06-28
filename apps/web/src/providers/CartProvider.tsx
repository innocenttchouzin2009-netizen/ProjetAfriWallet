"use client";

import { createContext, useMemo, useState, type ReactNode } from 'react';
import CartToast from '@/components/CartToast';
import type { CartContextValue, CartItem } from '@/types/cart.types';

export const CartContext = createContext<CartContextValue | undefined>(undefined);

export function CartProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<CartItem[]>([]);
  const [toastMessage, setToastMessage] = useState('');
  const [toastVisible, setToastVisible] = useState(false);

  const addItem = (item: Omit<CartItem, 'quantity'> & { quantity?: number }) => {
    setItems((current) => {
      const normalizedItem = { ...item, quantity: item.quantity ?? 1 };
      const existingItem = current.find((entry) => entry.id === normalizedItem.id);

      if (existingItem) {
        return current.map((entry) =>
          entry.id === normalizedItem.id
            ? { ...entry, quantity: entry.quantity + normalizedItem.quantity }
            : entry
        );
      }

      return [...current, normalizedItem];
    });
  };

  const removeItem = (id: string) => {
    setItems((current) => current.filter((item) => item.id !== id));
  };

  const updateQuantity = (id: string, quantity: number) => {
    setItems((current) =>
      current
        .map((item) => (item.id === id ? { ...item, quantity: Math.max(0, quantity) } : item))
        .filter((item) => item.quantity > 0)
    );
  };

  const clearCart = () => setItems([]);
  const showToast = (message: string) => {
    setToastMessage(message);
    setToastVisible(true);
  };
  const hideToast = () => setToastVisible(false);

  const value = useMemo<CartContextValue>(
    () => ({
      items,
      itemCount: items.reduce((sum, item) => sum + item.quantity, 0),
      subtotal: items.reduce((sum, item) => sum + item.price * item.quantity, 0),
      toastMessage,
      toastVisible,
      addItem,
      removeItem,
      updateQuantity,
      clearCart,
      showToast,
      hideToast,
    }),
    [items, toastMessage, toastVisible]
  );

  return (
    <CartContext.Provider value={value}>
      {children}
      <CartToast message={toastMessage} visible={toastVisible} onClose={hideToast} />
    </CartContext.Provider>
  );
}
