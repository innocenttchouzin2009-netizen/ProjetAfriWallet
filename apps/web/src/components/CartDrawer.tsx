"use client";

import Link from 'next/link';
import { useCart } from '@/hooks/useCart';

type CartDrawerProps = {
  open: boolean;
  onClose: () => void;
};

export default function CartDrawer({ open, onClose }: CartDrawerProps) {
  const { items, subtotal, updateQuantity, removeItem } = useCart();

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-[120] flex justify-end bg-black/60 backdrop-blur-sm transition-opacity duration-300">
      <div className="flex h-full w-full max-w-md translate-x-0 flex-col border-l border-white/10 bg-[#0D0D0D] p-6 text-white shadow-[0_0_60px_rgba(0,0,0,0.45)] transition-transform duration-300 ease-out">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm uppercase tracking-[0.3em] text-[#C8A45C]">Panier</p>
            <h2 className="mt-2 text-2xl font-black">Votre sélection</h2>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-full border border-white/15 px-3 py-2 text-sm"
          >
            Fermer
          </button>
        </div>

        <div className="mt-8 flex-1 space-y-4 overflow-y-auto">
          {items.length === 0 ? (
            <div className="flex h-full flex-col items-center justify-center rounded-[28px] border border-dashed border-white/15 bg-gradient-to-br from-white/8 to-white/3 p-8 text-center text-white/70">
              <div className="mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-[#C8A45C]/15 text-2xl text-[#C8A45C]">
                ✦
              </div>
              <p className="text-lg font-semibold text-white">Votre panier est encore vide</p>
              <p className="mt-2 max-w-xs text-sm text-white/60">
                Ajoutez un produit ou un design Studio pour commencer votre sélection premium.
              </p>
            </div>
          ) : (
            items.map((item) => (
              <div key={item.id} className="rounded-3xl border border-white/10 bg-white/5 p-4">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <p className="font-semibold">{item.name}</p>
                    <p className="mt-1 text-sm text-white/60">{item.description ?? 'Produit'}</p>
                  </div>
                  <p className="text-sm font-semibold text-[#C8A45C]">{(item.price * item.quantity).toFixed(2)} €</p>
                </div>

                <div className="mt-4 flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={() => updateQuantity(item.id, item.quantity - 1)}
                      className="h-8 w-8 rounded-full border border-white/15"
                    >
                      -
                    </button>
                    <span className="w-6 text-center">{item.quantity}</span>
                    <button
                      type="button"
                      onClick={() => updateQuantity(item.id, item.quantity + 1)}
                      className="h-8 w-8 rounded-full border border-white/15"
                    >
                      +
                    </button>
                  </div>
                  <button
                    type="button"
                    onClick={() => removeItem(item.id)}
                    className="text-sm text-white/50"
                  >
                    Supprimer
                  </button>
                </div>
              </div>
            ))
          )}
        </div>

        <div className="mt-6 border-t border-white/10 pt-6">
          <div className="flex items-center justify-between text-lg font-semibold">
            <span>Sous-total</span>
            <span className="text-[#C8A45C]">{subtotal.toFixed(2)} €</span>
          </div>
          <Link
            href="/cart"
            className="mt-6 block rounded-full bg-[#C8A45C] px-6 py-4 text-center font-bold text-black shadow-[0_0_24px_rgba(200,164,92,0.25)] transition hover:-translate-y-0.5"
          >
            Passer au paiement
          </Link>
        </div>
      </div>
    </div>
  );
}
