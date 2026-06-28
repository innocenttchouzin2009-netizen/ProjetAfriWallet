import { CheckoutPaymentIntent, Client, Environment, OrdersController, PaymentsController } from '@paypal/paypal-server-sdk';
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

function getPaypalClient() {
  return new Client({
    clientCredentialsAuthCredentials: {
      oAuthClientId: getEnv('PAYPAL_CLIENT_ID'),
      oAuthClientSecret: getEnv('PAYPAL_CLIENT_SECRET'),
    },
    environment: (process.env.PAYPAL_MODE ?? 'sandbox').toLowerCase() === 'live' ? Environment.Production : Environment.Sandbox,
  });
}

function getPaypalBaseUrl() {
  return (process.env.PAYPAL_MODE ?? 'sandbox').toLowerCase() === 'live'
    ? 'https://api-m.paypal.com'
    : 'https://api-m.sandbox.paypal.com';
}

export class PaypalPaymentProvider implements PaymentProvider {
  private readonly client = getPaypalClient();
  private readonly ordersController = new OrdersController(this.client);
  private readonly paymentsController = new PaymentsController(this.client);

  private async getAccessToken(): Promise<string> {
    const auth = Buffer.from(`${getEnv('PAYPAL_CLIENT_ID')}:${getEnv('PAYPAL_CLIENT_SECRET')}`).toString('base64');

    const response = await fetch(`${getPaypalBaseUrl()}/v1/oauth2/token`, {
      method: 'POST',
      headers: {
        Authorization: `Basic ${auth}`,
        'Content-Type': 'application/x-www-form-urlencoded',
      },
      body: 'grant_type=client_credentials',
    });

    if (!response.ok) {
      throw new Error('Unable to authenticate against PayPal API.');
    }

    const payload = (await response.json()) as { access_token?: string };
    if (!payload.access_token) {
      throw new Error('PayPal access token missing.');
    }

    return payload.access_token;
  }

  private async verifyWebhookSignature(rawBody: string, headers: Headers): Promise<void> {
    const webhookId = process.env.PAYPAL_WEBHOOK_ID;
    if (!webhookId) {
      return;
    }

    const transmissionId = headers.get('paypal-transmission-id');
    const transmissionTime = headers.get('paypal-transmission-time');
    const certUrl = headers.get('paypal-cert-url');
    const authAlgo = headers.get('paypal-auth-algo');
    const transmissionSig = headers.get('paypal-transmission-sig');

    if (!transmissionId || !transmissionTime || !certUrl || !authAlgo || !transmissionSig) {
      throw new Error('Missing PayPal webhook verification headers.');
    }

    const token = await this.getAccessToken();
    const eventPayload = JSON.parse(rawBody);

    const verifyResponse = await fetch(`${getPaypalBaseUrl()}/v1/notifications/verify-webhook-signature`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        transmission_id: transmissionId,
        transmission_time: transmissionTime,
        cert_url: certUrl,
        auth_algo: authAlgo,
        transmission_sig: transmissionSig,
        webhook_id: webhookId,
        webhook_event: eventPayload,
      }),
    });

    if (!verifyResponse.ok) {
      throw new Error('PayPal webhook verification call failed.');
    }

    const verifyPayload = (await verifyResponse.json()) as { verification_status?: string };
    if (verifyPayload.verification_status !== 'SUCCESS') {
      throw new Error('Invalid PayPal webhook signature.');
    }
  }

  async createCheckoutSession(input: PaymentCheckoutInput): Promise<PaymentCheckoutSession> {
    const response = await this.ordersController.createOrder({
      body: {
        intent: CheckoutPaymentIntent.Capture,
        purchaseUnits: [
          {
            referenceId: input.orderId,
            customId: input.orderId,
            amount: {
              currencyCode: input.currency.toUpperCase(),
              value: (input.amountCents / 100).toFixed(2),
            },
          },
        ],
        applicationContext: {
          returnUrl: input.returnUrl,
          cancelUrl: input.cancelUrl,
        },
      },
      prefer: 'return=representation',
    });

    if (!response.result.id) {
      throw new Error('PayPal order reference missing.');
    }

    const approvalUrl = response.result.links?.find((link: { rel?: string }) => link.rel === 'approve')?.href;

    return {
      provider: 'paypal',
      status: 'requires_action',
      reference: response.result.id,
      approvalUrl,
    };
  }

  async parseWebhookEvent(rawBody: string, headers: Headers): Promise<PaymentWebhookEvent> {
    await this.verifyWebhookSignature(rawBody, headers);

    const payload = JSON.parse(rawBody) as {
      id?: string;
      event_type?: string;
      resource?: {
        id?: string;
        custom_id?: string;
        supplementary_data?: {
          related_ids?: {
            order_id?: string;
          };
        };
      };
    };

    const eventType = payload.event_type ?? 'unknown';
    const status =
      eventType === 'CHECKOUT.ORDER.APPROVED' || eventType === 'PAYMENT.CAPTURE.COMPLETED'
        ? 'succeeded'
        : eventType === 'PAYMENT.CAPTURE.DENIED' || eventType === 'PAYMENT.CAPTURE.DECLINED'
          ? 'failed'
          : eventType === 'PAYMENT.CAPTURE.REFUNDED'
            ? 'refunded'
            : 'ignored';

    return {
      provider: 'paypal',
      eventId: payload.id ?? `paypal-${Date.now()}`,
      orderId: payload.resource?.custom_id,
      paymentReference: payload.resource?.id ?? payload.resource?.supplementary_data?.related_ids?.order_id,
      status,
      rawType: eventType,
    };
  }

  async captureOrder(paypalOrderId: string): Promise<{ reference: string; status: 'succeeded' | 'failed' }> {
    const response = await this.ordersController.captureOrder({
      id: paypalOrderId,
      prefer: 'return=representation',
    });
    const capture = response.result.purchaseUnits?.[0]?.payments?.captures?.[0];

    if (!capture?.id) {
      throw new Error('PayPal capture reference missing.');
    }

    return {
      reference: capture.id,
      status: capture.status === 'COMPLETED' ? 'succeeded' : 'failed',
    };
  }

  async createRefund(input: PaymentRefundInput & { paymentReference: string }): Promise<PaymentRefundResult> {
    const response = await this.paymentsController.refundCapturedPayment({
      captureId: input.paymentReference,
      prefer: 'return=representation',
      body: input.amountCents
        ? {
            amount: {
              currencyCode: 'EUR',
              value: (input.amountCents / 100).toFixed(2),
            },
            noteToPayer: input.reason,
          }
        : {
            noteToPayer: input.reason,
          },
    });

    if (!response.result.id) {
      throw new Error('PayPal refund reference missing.');
    }

    return {
      provider: 'paypal',
      reference: response.result.id,
      status: response.result.status === 'COMPLETED' ? 'succeeded' : 'pending',
    };
  }
}
