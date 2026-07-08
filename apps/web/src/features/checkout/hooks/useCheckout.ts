"use client";

import { useMemo, useState } from 'react';
import { shippingMethods } from '../data/shipping.data';
import type { CheckoutFormValues, CheckoutState, CheckoutSubmitResult, ShippingMethod } from '../types/checkout.types';
import type { CartItem } from '@/types/cart.types';

type SubmitCheckoutInput = {
  items: CartItem[];
  shippingCents: number;
  discountCents?: number;
};

const initialValues: CheckoutFormValues = {
  customer: {
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
  },
  address: {
    address: '',
    postalCode: '',
    city: '',
    country: 'France',
  },
  shippingMethodId: 'standard',
  paymentProvider: 'stripe',
};

export function useCheckout() {
  const [state, setState] = useState<CheckoutState>({
    values: initialValues,
    isSubmitting: false,
    isComplete: false,
  });

  const selectedShippingMethod = useMemo<ShippingMethod | undefined>(() => {
    return shippingMethods.find((method) => method.id === state.values.shippingMethodId);
  }, [state.values.shippingMethodId]);

  const updateField = <T extends keyof CheckoutFormValues>(field: T, value: CheckoutFormValues[T]) => {
    setState((current) => ({
      ...current,
      values: {
        ...current.values,
        [field]: value,
      },
    }));
  };

  const updateCustomer = (field: keyof CheckoutFormValues['customer'], value: string) => {
    setState((current) => ({
      ...current,
      values: {
        ...current.values,
        customer: {
          ...current.values.customer,
          [field]: value,
        },
      },
    }));
  };

  const updateAddress = (field: keyof CheckoutFormValues['address'], value: string) => {
    setState((current) => ({
      ...current,
      values: {
        ...current.values,
        address: {
          ...current.values.address,
          [field]: value,
        },
      },
    }));
  };

  const submitCheckout = async (input: SubmitCheckoutInput): Promise<CheckoutSubmitResult> => {
    setState((current) => ({ ...current, isSubmitting: true, errorMessage: undefined }));

    try {
      const response = await fetch('/api/checkout', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          customer: state.values.customer,
          address: state.values.address,
          paymentProvider: state.values.paymentProvider,
          items: input.items.map((item) => ({
            name: item.name,
            quantity: item.quantity,
            unitPrice: item.price,
            sku: typeof item.metadata?.sku === 'string' ? item.metadata.sku : undefined,
            customInitials:
              typeof item.metadata?.customInitials === 'string' ? item.metadata.customInitials : undefined,
            customLogoUrl:
              typeof item.metadata?.customLogoUrl === 'string' ? item.metadata.customLogoUrl : undefined,
          })),
          shippingCents: input.shippingCents,
          discountCents: input.discountCents ?? 0,
        }),
      });

      const payload = (await response.json()) as {
        id?: string;
        payment?: {
          provider?: 'stripe' | 'paypal';
          status?: 'requires_action' | 'succeeded' | 'failed';
          checkoutSessionId?: string;
          approvalUrl?: string;
          clientSecret?: string;
        };
        message?: string;
      };

      if (!response.ok) {
        throw new Error(payload.message ?? 'Checkout failed');
      }

      if (payload.payment?.provider === 'paypal' && payload.payment.approvalUrl) {
        setState((current) => ({
          ...current,
          isSubmitting: false,
        }));

        return {
          ok: true,
          redirectUrl: payload.payment.approvalUrl,
        };
      }

      if (payload.payment?.provider === 'stripe' && payload.payment.status === 'requires_action') {
        setState((current) => ({
          ...current,
          isSubmitting: false,
        }));

        return {
          ok: true,
          stripeSessionId: payload.payment.checkoutSessionId,
          requiresAction: true,
          message: 'Redirection Stripe en cours.',
        };
      }

      setState((current) => ({
        ...current,
        isSubmitting: false,
        isComplete: true,
      }));

      return { ok: true };
    } catch (error) {
      setState((current) => ({
        ...current,
        isSubmitting: false,
        errorMessage: error instanceof Error ? error.message : 'Checkout failed',
      }));

      return {
        ok: false,
        message: error instanceof Error ? error.message : 'Checkout failed',
      };
    }
  };

  return {
    state,
    selectedShippingMethod,
    updateField,
    updateCustomer,
    updateAddress,
    submitCheckout,
  };
}
