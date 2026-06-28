"use client";

import type { POSPaymentMethod } from '../types/pos.types';

interface POSPaymentPanelProps {
  discount: number;
  selectedPayment: POSPaymentMethod['id'];
  onDiscountChange: (value: number) => void;
  onPaymentChange: (value: POSPaymentMethod['id']) => void;
  onReset: () => void;
  onCheckout: () => void;
  total: number;
  paymentMethods: POSPaymentMethod[];
}

export default function POSPaymentPanel({
  discount,
  selectedPayment,
  onDiscountChange,
  onPaymentChange,
  onReset,
  onCheckout,
  total,
  paymentMethods,
}: POSPaymentPanelProps) {
  return (
    <div className="rounded-[32px] border border-white/10 bg-white/5 p-6">
      <p className="text-sm uppercase tracking-[0.3em] text-[#C8A45C]">Paiement</p>
      <h2 className="mt-2 text-2xl font-black text-white">Finaliser la vente</h2>

      <label className="mt-6 block text-sm text-white/70">
        Remise (€)
        <input
          type="number"
          min="0"
          value={discount}
          onChange={(event) => onDiscountChange(Number(event.target.value))}
          className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white"
        />
      </label>

      <div className="mt-6">
        <p className="text-sm text-white/70">Mode de paiement</p>
        <div className="mt-3 grid gap-3 sm:grid-cols-2">
          {paymentMethods.map((method) => (
            <button
              key={method.id}
              onClick={() => onPaymentChange(method.id)}
              className={`rounded-[20px] border px-4 py-3 text-left ${
                selectedPayment === method.id
                  ? 'border-[#C8A45C] bg-[#C8A45C]/15 text-[#F5E0AC]'
                  : 'border-white/10 bg-black/20 text-white/70'
              }`}
            >
              {method.label}
            </button>
          ))}
        </div>
      </div>

      <div className="mt-6 rounded-[24px] border border-white/10 bg-black/20 p-4">
        <p className="text-sm text-white/60">À régler</p>
        <p className="mt-2 text-4xl font-black text-white">{total.toFixed(2)} €</p>
      </div>

      <div className="mt-6 flex flex-col gap-3">
        <button
          onClick={onCheckout}
          className="rounded-full bg-[#C8A45C] px-6 py-3 font-semibold text-black"
        >
          Valider la vente
        </button>
        <button
          onClick={onReset}
          className="rounded-full border border-white/10 px-6 py-3 font-semibold text-white/70"
        >
          Réinitialiser
        </button>
      </div>
    </div>
  );
}
