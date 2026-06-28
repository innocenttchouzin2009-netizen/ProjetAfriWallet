import Stripe from 'stripe';
import type { PaymentProvider } from './PaymentProvider';
import type {
  PaymentCheckoutInput,
  PaymentCheckoutSession,
  PaymentRefundInput,
  PaymentRefundResult,
  PaymentWebhookEvent,
} from '../types/payment.types';

function getEnv(name: string): string {
  const value = process.env[name];
  if (!value) {
    throw new Error(`Missing required env variable: ${name}`);
  }
  return value;
}

export class StripePaymentProvider implements PaymentProvider {
  private readonly stripe: Stripe;

  constructor() {
    this.stripe = new Stripe(getEnv('STRIPE_SECRET_KEY'));
  }

  async createCheckoutSession(input: PaymentCheckoutInput): Promise<PaymentCheckoutSession> {
    const session = await this.stripe.checkout.sessions.create({
      mode: 'payment',
      customer_email: input.customerEmail,
      success_url: input.returnUrl ?? 'http://localhost:3000/checkout/success',
      cancel_url: input.cancelUrl ?? 'http://localhost:3000/checkout/cancel',
      line_items: [
        {
          quantity: 1,
          price_data: {
            currency: input.currency,
            unit_amount: input.amountCents,
            product_data: {
              name: `Commande ${input.orderId}`,
            },
          },
        },
      ],
      metadata: {
        orderId: input.orderId,
      },
      payment_intent_data: {
        metadata: {
          orderId: input.orderId,
        },
      },
    });

    const paymentIntentId = typeof session.payment_intent === 'string' ? session.payment_intent : session.id;

    return {
      provider: 'stripe',
      status: 'requires_action',
      reference: paymentIntentId,
      checkoutSessionId: session.id,
      approvalUrl: session.url ?? undefined,
    };
  }

  async parseWebhookEvent(rawBody: string, headers: Headers): Promise<PaymentWebhookEvent> {
    const signature = headers.get('stripe-signature');
    if (!signature) {
      throw new Error('Missing stripe-signature header');
    }

    const event = this.stripe.webhooks.constructEvent(rawBody, signature, getEnv('STRIPE_WEBHOOK_SECRET'));

    let orderId: string | undefined;
    let paymentReference: string | undefined;
    let status: 'succeeded' | 'failed' | 'refunded' | 'ignored' = 'ignored';

    if (event.type === 'checkout.session.completed') {
      const checkoutSession = event.data.object as { payment_intent?: string | null; metadata?: { orderId?: string } };
      orderId = checkoutSession.metadata?.orderId;
      paymentReference = checkoutSession.payment_intent ?? undefined;
      status = 'succeeded';
    }

    if (event.type === 'payment_intent.succeeded') {
      const paymentIntentObject = event.data.object as { id?: string; metadata?: { orderId?: string } };
      orderId = paymentIntentObject.metadata?.orderId;
      paymentReference = paymentIntentObject.id;
      status = 'succeeded';
    }

    if (event.type === 'payment_intent.payment_failed') {
      const paymentIntentObject = event.data.object as { id?: string; metadata?: { orderId?: string } };
      orderId = paymentIntentObject.metadata?.orderId;
      paymentReference = paymentIntentObject.id;
      status = 'failed';
    }

    if (event.type === 'charge.refunded') {
      const chargeObject = event.data.object as {
        payment_intent?: string;
        metadata?: { orderId?: string };
      };
      orderId = chargeObject.metadata?.orderId;
      paymentReference = chargeObject.payment_intent;
      status = 'refunded';
    }

    return {
      provider: 'stripe',
      eventId: event.id,
      orderId,
      paymentReference,
      status,
      rawType: event.type,
    };
  }

  async createRefund(input: PaymentRefundInput & { paymentReference: string }): Promise<PaymentRefundResult> {
    const refund = await this.stripe.refunds.create({
      payment_intent: input.paymentReference,
      amount: input.amountCents,
      reason: input.reason === 'fraudulent' || input.reason === 'requested_by_customer' || input.reason === 'duplicate'
        ? input.reason
        : undefined,
      metadata: {
        orderId: input.orderId,
      },
    });

    return {
      provider: 'stripe',
      reference: refund.id,
      status: refund.status === 'succeeded' ? 'succeeded' : 'pending',
    };
  }
}
