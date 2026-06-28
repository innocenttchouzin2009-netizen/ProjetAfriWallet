"use client";

import type { POSLineItem } from '../types/pos.types';

interface POSTicketProps {
  items: POSLineItem[];
  subtotal: number;
  discount: number;
  total: number;
  onQuantityChange: (productId: string, quantity: number) => void;
  onRemove: (productId: string) => void;
}

export default function POSTicket({
  items,
  subtotal,
  discount,
  total,
  onQuantityChange,
  onRemove,
}: POSTicketProps) {
  return (
    <div className="rounded-[32px] border border-white/10 bg-black/40 p-6">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm uppercase tracking-[0.3em] text-[#C8A45C]">Ticket</p>
          <h2 className="mt-2 text-2xl font-black text-white">Caisse boutique</h2>
        </div>
        <div className="rounded-full border border-white/10 bg-white/5 px-3 py-1 text-sm text-white/70">
          {items.length} article{items.length > 1 ? 's' : ''}
        </div>
      </div>

      <div className="mt-6 space-y-3">
        {items.length === 0 ? (
          <div className="rounded-[20px] border border-dashed border-white/10 p-5 text-center text-sm text-white/60">
            Ajoute un produit pour commencer la vente.
          </div>
        ) : (
          items.map((item) => (
            <div key={item.productId} className="rounded-[20px] border border-white/10 bg-white/5 p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-semibold text-white">{item.name}</p>
                  <p className="text-sm text-white/60">{item.price.toFixed(2)} €</p>
                </div>
                <button onClick={() => onRemove(item.productId)} className="text-sm text-[#F0B86E]">
                  Retirer
                </button>
              </div>

              <div className="mt-4 flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => onQuantityChange(item.productId, item.quantity - 1)}
                    className="h-8 w-8 rounded-full border border-white/10 text-white"
                  >
                    −
                  </button>
                  <span className="w-8 text-center font-semibold text-white">{item.quantity}</span>
                  <button
                    onClick={() => onQuantityChange(item.productId, item.quantity + 1)}
                    className="h-8 w-8 rounded-full border border-white/10 text-white"
                  >
                    +
                  </button>
                </div>
                <p className="font-semibold text-white">{(item.price * item.quantity).toFixed(2)} €</p>
              </div>
            </div>
          ))
        )}
      </div>

      <div className="mt-6 space-y-2 border-t border-white/10 pt-4 text-sm text-white/70">
        <div className="flex items-center justify-between">
          <span>Sous-total</span>
          <span>{subtotal.toFixed(2)} €</span>
        </div>
        <div className="flex items-center justify-between">
          <span>Remise</span>
          <span>-{discount.toFixed(2)} €</span>
        </div>
        <div className="mt-3 flex items-center justify-between border-t border-white/10 pt-3 text-base font-semibold text-white">
          <span>Total</span>
          <span>{total.toFixed(2)} €</span>
        </div>
      </div>
    </div>
  );
}
