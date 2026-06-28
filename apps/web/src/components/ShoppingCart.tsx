import Link from 'next/link';
import { useCart } from '@/hooks/useCart';

export default function ShoppingCart() {
  const { items, itemCount, subtotal } = useCart();

  return (
    <aside className="rounded-[32px] border border-white/10 bg-white/5 p-8">
      <h2 className="text-2xl font-black">Ton panier</h2>
      <p className="mt-4 text-white/70">Consulte la sélection en cours et passe au paiement.</p>
      <div className="mt-8 space-y-4">
        <div className="rounded-3xl border border-white/10 bg-[#0D0D0D] p-4">
          <div className="flex items-center justify-between text-sm text-white/70">
            <span>{itemCount > 0 ? `${itemCount} article${itemCount > 1 ? 's' : ''}` : 'Votre panier est vide'}</span>
            <span>{subtotal.toFixed(2)}€</span>
          </div>
          {items.length > 0 ? (
            <div className="mt-4 space-y-2">
              {items.slice(0, 3).map((item) => (
                <div key={item.id} className="flex items-center justify-between text-sm text-white/60">
                  <span>{item.name}</span>
                  <span>x{item.quantity}</span>
                </div>
              ))}
            </div>
          ) : null}
        </div>
        <Link href="/cart" className="block rounded-full bg-[#C8A45C] px-6 py-4 text-center font-bold text-black">
          Voir le panier
        </Link>
      </div>
    </aside>
  );
}
