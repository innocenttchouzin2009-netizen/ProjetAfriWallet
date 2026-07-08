"use client";
import Link from "next/link";
import { useCart } from '@/hooks/useCart';

export default function CartPage() {
  const { items, updateQuantity, removeItem, subtotal } = useCart();
  const shipping = subtotal >= 100 ? 0 : 7.9;
  const total = subtotal + shipping;

  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-28 text-white md:px-16">
      <h1 className="text-5xl font-black">Mon panier</h1>
      <div className="mt-12 grid gap-10 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          {items.length === 0 ? (
            <div className="rounded-3xl border border-white/10 bg-white/5 p-6 text-white/70">
              Ton panier est vide pour le moment.
            </div>
          ) : items.map((item) => (
            <div
              key={item.id}
              className="rounded-3xl border border-white/10 bg-white/5 p-6"
            >
              <div className="flex flex-col justify-between gap-6 md:flex-row">
                <div>
                  <h2 className="text-2xl font-bold">{item.name}</h2>
                  <p className="mt-2 text-white/60">{item.kind === 'studio-design' ? 'Design Studio' : 'Produit'}</p>
                  {item.description ? <p className="mt-1 text-white/60">{item.description}</p> : null}
                  {typeof item.metadata?.customInitials === 'string' && item.metadata.customInitials ? (
                    <p className="mt-1 text-xs text-[#C8A45C]">Initiales: {item.metadata.customInitials}</p>
                  ) : null}
                  {typeof item.metadata?.customLogoUrl === 'string' && item.metadata.customLogoUrl ? (
                    <p className="mt-1 text-xs text-[#C8A45C]">Logo perso ajoute</p>
                  ) : null}
                </div>
                <div className="text-right">
                  <p className="text-2xl font-bold text-[#C8A45C]">
                    {(item.price * item.quantity).toFixed(2)} €
                  </p>
                  <div className="mt-3 flex items-center justify-end gap-3">
                    <button
                      type="button"
                      onClick={() => updateQuantity(item.id, item.quantity - 1)}
                      className="rounded-full border border-white/20 px-3 py-1"
                    >
                      -
                    </button>
                    <span>{item.quantity}</span>
                    <button
                      type="button"
                      onClick={() => updateQuantity(item.id, item.quantity + 1)}
                      className="rounded-full border border-white/20 px-3 py-1"
                    >
                      +
                    </button>
                    <button
                      type="button"
                      onClick={() => removeItem(item.id)}
                      className="ml-2 text-sm text-white/50"
                    >
                      Supprimer
                    </button>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
        <aside className="rounded-3xl border border-white/10 bg-white/5 p-8">
          <h2 className="text-2xl font-bold">Résumé</h2>
          <div className="mt-8 space-y-4">
            <div className="flex justify-between">
              <span>Sous-total</span>
              <span>{subtotal.toFixed(2)} €</span>
            </div>
            <div className="flex justify-between">
              <span>Livraison</span>
              <span>
                {shipping === 0 ? "Offerte" : `${shipping.toFixed(2)} €`}
              </span>
            </div>
            <hr className="border-white/10" />
            <div className="flex justify-between text-2xl font-bold">
              <span>Total</span>
              <span className="text-[#C8A45C]">{total.toFixed(2)} €</span>
            </div>
          </div>
          <Link
            href="/checkout"
            className="mt-8 block rounded-full bg-[#C8A45C] px-6 py-4 text-center font-bold text-black"
          >
            Passer au paiement
          </Link>
          <Link
            href="/shop"
            className="mt-4 block rounded-full border border-white/20 px-6 py-4 text-center"
          >
            Continuer les achats
          </Link>
        </aside>
      </div>
    </main>
  );
}
