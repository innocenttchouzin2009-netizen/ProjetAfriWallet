export type PaymentProviderKey = 'stripe' | 'paypal';

export type PaymentStatus = 'requires_action' | 'succeeded' | 'failed';

export type WebhookStatus = 'succeeded' | 'failed' | 'refunded' | 'ignored';

export interface PaymentCheckoutInput {
  orderId: string;
  amountCents: number;
  currency: 'eur';
  customerEmail?: string;
  returnUrl?: string;
  cancelUrl?: string;
}

export interface PaymentCheckoutSession {
  provider: PaymentProviderKey;
  status: PaymentStatus;
  reference: string;
  checkoutSessionId?: string;
  clientSecret?: string;
  approvalUrl?: string;
}

export interface PaymentWebhookEvent {
  provider: PaymentProviderKey;
  eventId: string;
  orderId?: string;
  paymentReference?: string;
  status: WebhookStatus;
  rawType: string;
}

export interface PaymentRefundInput {
  orderId: string;
  amountCents?: number;
  reason?: string;
}

export interface PaymentRefundResult {
  provider: PaymentProviderKey;
  reference: string;
  status: 'succeeded' | 'pending';
}

export type StripeWebhookHeaders = {
  stripeSignature?: string;
};
