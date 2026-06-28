"use client";

import { loadStripe } from '@stripe/stripe-js';
import { useCheckout } from '../hooks/useCheckout';
import { shippingMethods } from '../data/shipping.data';
import ShippingMethodSelector from './ShippingMethodSelector';
import OrderSummary from './OrderSummary';
import { useCart } from '@/hooks/useCart';

const stripePromise = loadStripe(process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY ?? '');

export default function CheckoutForm() {
  const { state, updateCustomer, updateAddress, updateField, submitCheckout, selectedShippingMethod } = useCheckout();
  const { items, subtotal, itemCount, clearCart } = useCart();

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    const result = await submitCheckout({
      items,
      shippingCents: Math.round((selectedShippingMethod?.price ?? 0) * 100),
    });

    if (result.redirectUrl) {
      window.location.href = result.redirectUrl;
      return;
    }

    if (result.stripeSessionId) {
      const stripe = await stripePromise;
      if (!stripe) {
        window.location.href = '/checkout/cancel?provider=stripe&reason=stripe_redirect_failed';
        return;
      }

      const redirectResult = await stripe.redirectToCheckout({
        sessionId: result.stripeSessionId,
      });

      if (redirectResult.error) {
        window.location.href = '/checkout/cancel?provider=stripe&reason=stripe_redirect_failed';
      }
      return;
    }

    if (result.ok && !result.requiresAction) {
      clearCart();
    }
  };

  const isValid =
    state.values.customer.firstName.trim() &&
    state.values.customer.lastName.trim() &&
    state.values.customer.email.trim() &&
    state.values.address.address.trim() &&
    state.values.address.postalCode.trim() &&
    state.values.address.city.trim();

  return (
    <form onSubmit={handleSubmit} className="grid gap-8 lg:grid-cols-[1.2fr_0.8fr]">
      <div className="space-y-8 rounded-[32px] border border-white/10 bg-white/5 p-8">
        <section>
          <h2 className="text-2xl font-black">Informations client</h2>
          <div className="mt-6 grid gap-4 md:grid-cols-2">
            <input
              required
              value={state.values.customer.firstName}
              onChange={(event) => updateCustomer('firstName', event.target.value)}
              placeholder="Prénom"
              className="rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white"
            />
            <input
              required
              value={state.values.customer.lastName}
              onChange={(event) => updateCustomer('lastName', event.target.value)}
              placeholder="Nom"
              className="rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white"
            />
            <input
              required
              type="email"
              value={state.values.customer.email}
              onChange={(event) => updateCustomer('email', event.target.value)}
              placeholder="Email"
              className="rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white"
            />
            <input
              value={state.values.customer.phone}
              onChange={(event) => updateCustomer('phone', event.target.value)}
              placeholder="Téléphone"
              className="rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white"
            />
          </div>
        </section>

        <section>
          <h2 className="text-2xl font-black">Adresse de livraison</h2>
          <div className="mt-6 grid gap-4">
            <input
              required
              value={state.values.address.address}
              onChange={(event) => updateAddress('address', event.target.value)}
              placeholder="Adresse"
              className="rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white"
            />
            <div className="grid gap-4 md:grid-cols-2">
              <input
                required
                value={state.values.address.postalCode}
                onChange={(event) => updateAddress('postalCode', event.target.value)}
                placeholder="Code postal"
                className="rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white"
              />
              <input
                required
                value={state.values.address.city}
                onChange={(event) => updateAddress('city', event.target.value)}
                placeholder="Ville"
                className="rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white"
              />
            </div>
            <input
              value={state.values.address.country}
              onChange={(event) => updateAddress('country', event.target.value)}
              placeholder="Pays"
              className="rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white"
            />
          </div>
        </section>

        <section>
          <h2 className="text-2xl font-black">Mode de livraison</h2>
          <ShippingMethodSelector
            methods={shippingMethods}
            selectedId={state.values.shippingMethodId}
            onSelect={(id) => updateField('shippingMethodId', id)}
          />
        </section>

        <section>
          <h2 className="text-2xl font-black">Paiement</h2>
          <div className="mt-4 grid gap-3 md:grid-cols-2">
            <label className="flex cursor-pointer items-center gap-3 rounded-2xl border border-white/10 bg-black/30 px-4 py-3">
              <input
                type="radio"
                name="paymentProvider"
                value="stripe"
                checked={state.values.paymentProvider === 'stripe'}
                onChange={() => updateField('paymentProvider', 'stripe')}
              />
              <span>Stripe (carte)</span>
            </label>
            <label className="flex cursor-pointer items-center gap-3 rounded-2xl border border-white/10 bg-black/30 px-4 py-3">
              <input
                type="radio"
                name="paymentProvider"
                value="paypal"
                checked={state.values.paymentProvider === 'paypal'}
                onChange={() => updateField('paymentProvider', 'paypal')}
              />
              <span>PayPal</span>
            </label>
          </div>
        </section>

        {state.errorMessage ? (
          <p className="rounded-2xl border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-200">{state.errorMessage}</p>
        ) : null}

        <button
          type="submit"
          disabled={!isValid || state.isSubmitting || itemCount === 0}
          className="w-full rounded-full bg-[#C8A45C] px-6 py-4 font-bold text-black disabled:cursor-not-allowed disabled:opacity-50"
        >
          {state.isSubmitting ? 'Paiement en cours…' : 'Payer maintenant'}
        </button>
      </div>

      <OrderSummary
        items={items}
        subtotal={subtotal}
        shippingMethod={selectedShippingMethod}
        itemCount={itemCount}
      />
    </form>
  );
}
