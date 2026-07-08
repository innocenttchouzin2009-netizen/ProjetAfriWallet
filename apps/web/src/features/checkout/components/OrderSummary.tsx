import type { CartItem } from '@/types/cart.types';
import type { ShippingMethod } from '../types/checkout.types';

type Props = {
  items: CartItem[];
  subtotal: number;
  shippingMethod?: ShippingMethod;
  itemCount: number;
};

export default function OrderSummary({ items, subtotal, shippingMethod, itemCount }: Props) {
  const shipping = shippingMethod?.price ?? 0;
  const total = subtotal + shipping;

  return (
    <aside className="rounded-[32px] border border-white/10 bg-black/70 p-8 text-white">
      <h2 className="text-2xl font-black">Résumé de commande</h2>
      <p className="mt-2 text-sm text-white/60">{itemCount} article{itemCount > 1 ? 's' : ''}</p>

      <div className="mt-8 space-y-4">
        {items.map((item) => (
          <div key={item.id} className="flex items-start justify-between text-sm text-white/70">
            <div>
              <div>{item.name}</div>
              {typeof item.metadata?.customInitials === 'string' && item.metadata.customInitials ? (
                <div className="text-xs text-[#C8A45C]">Initiales: {item.metadata.customInitials}</div>
              ) : null}
              {typeof item.metadata?.customLogoUrl === 'string' && item.metadata.customLogoUrl ? (
                <div className="text-xs text-[#C8A45C]">Logo perso ajoute</div>
              ) : null}
            </div>
            <span>x{item.quantity}</span>
          </div>
        ))}
      </div>

      <div className="mt-8 space-y-3 border-t border-white/10 pt-6 text-sm text-white/70">
        <div className="flex justify-between">
          <span>Sous-total</span>
          <span>{subtotal.toFixed(2)} €</span>
        </div>
        <div className="flex justify-between">
          <span>Livraison</span>
          <span>{shipping.toFixed(2)} €</span>
        </div>
        <div className="flex justify-between text-lg font-semibold text-white">
          <span>Total</span>
          <span className="text-[#C8A45C]">{total.toFixed(2)} €</span>
        </div>
      </div>
    </aside>
  );
}
