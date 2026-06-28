"use client";

import { useMemo } from 'react';
import POSProductGrid from '@/features/pos/components/POSProductGrid';
import POSTicket from '@/features/pos/components/POSTicket';
import POSPaymentPanel from '@/features/pos/components/POSPaymentPanel';
import { usePOS } from '@/features/pos/hooks/usePOS';

export default function POSPage() {
  const { items, subtotal, discount, total, selectedPayment, setDiscount, setSelectedPayment, addItem, removeItem, updateQuantity, resetTicket, paymentMethods } = usePOS();

  const paymentLabel = useMemo(() => {
    return selectedPayment === 'cash' ? 'Espèces' : 'Carte';
  }, [selectedPayment]);

  const handleCheckout = async () => {
    if (items.length === 0) return;

    const response = await fetch('/api/pos', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        paymentMethod: selectedPayment,
        discountCents: Math.round(discount * 100),
        items: items.map((item) => ({
          name: item.name,
          sku: item.sku,
          quantity: item.quantity,
          unitPrice: item.price,
        })),
      }),
    });

    if (!response.ok) {
      alert('Erreur lors de la validation de la vente POS.');
      return;
    }

    const order = await response.json();
    alert(`Reçu #${order.id} • ${paymentLabel} • Total ${total.toFixed(2)} €`);
    resetTicket();
  };

  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-8 text-white md:px-10 lg:px-16">
      <div className="rounded-[36px] border border-white/10 bg-gradient-to-br from-[#C8A45C]/20 via-black/80 to-black p-8">
        <p className="text-sm uppercase tracking-[0.35em] text-[#C8A45C]">POS / Caisse</p>
        <h1 className="mt-4 text-4xl font-black md:text-5xl">Vente en magasin</h1>
        <p className="mt-4 max-w-2xl text-white/70">
          Ajoute des produits au ticket, applique une remise, choisis un mode de paiement et finalise la vente.
        </p>
      </div>

      <div className="mt-8 grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
        <div className="space-y-6">
          <POSProductGrid onAdd={addItem} />
        </div>

        <div className="space-y-6">
          <POSTicket
            items={items}
            subtotal={subtotal}
            discount={discount}
            total={total}
            onQuantityChange={updateQuantity}
            onRemove={removeItem}
          />
          <POSPaymentPanel
            discount={discount}
            selectedPayment={selectedPayment}
            onDiscountChange={setDiscount}
            onPaymentChange={setSelectedPayment}
            onReset={resetTicket}
            onCheckout={handleCheckout}
            total={total}
            paymentMethods={paymentMethods}
          />
        </div>
      </div>
    </main>
  );
}
