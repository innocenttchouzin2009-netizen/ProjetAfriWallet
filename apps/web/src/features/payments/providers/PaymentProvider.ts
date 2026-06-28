import type {
  PaymentCheckoutInput,
  PaymentCheckoutSession,
  PaymentRefundInput,
  PaymentRefundResult,
  PaymentWebhookEvent,
} from '../types/payment.types';

export interface PaymentProvider {
  createCheckoutSession(input: PaymentCheckoutInput): Promise<PaymentCheckoutSession>;
  createRefund(input: PaymentRefundInput & { paymentReference: string }): Promise<PaymentRefundResult>;
  parseWebhookEvent(rawBody: string, headers: Headers): Promise<PaymentWebhookEvent>;
}
