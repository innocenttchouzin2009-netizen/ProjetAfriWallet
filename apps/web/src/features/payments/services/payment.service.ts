import { OrderStatus } from '@prisma/client';
import { prisma } from '@/lib/prisma';
import { AuditService } from '@/features/audit/services/audit.service';
import { NotificationService } from '@/features/notifications/services/notification.service';
import { InvoicePdfService } from '@/features/invoices/services/invoice-pdf.service';
import { StripePaymentProvider } from '../providers/stripe.provider';
import { PaypalPaymentProvider } from '../providers/paypal.provider';
import type { PaymentProvider } from '../providers/PaymentProvider';
import type {
  PaymentCheckoutInput,
  PaymentCheckoutSession,
  PaymentProviderKey,
  PaymentRefundInput,
  PaymentRefundResult,
} from '../types/payment.types';

type PrismaLike = typeof prisma;

type PaymentServiceDeps = {
  prismaClient?: PrismaLike;
  notificationService?: NotificationService;
  invoicePdfService?: InvoicePdfService;
  providerFactory?: (provider: PaymentProviderKey) => PaymentProvider;
  paypalProvider?: PaypalPaymentProvider;
};

function resolveWebhookOrderStatus(current: OrderStatus, eventStatus: 'succeeded' | 'failed' | 'refunded' | 'ignored') {
  if (eventStatus === 'succeeded') {
    return current === OrderStatus.DELIVERED ? current : OrderStatus.CONFIRMED;
  }

  if (eventStatus === 'failed') {
    return current === OrderStatus.DRAFT || current === OrderStatus.CONFIRMED ? OrderStatus.CANCELED : current;
  }

  return current;
}

async function notifyOrderConfirmed(
  notificationService: NotificationService,
  order: { id: string; totalCents: number; user: { email: string; firstName: string; lastName: string } | null },
) {
  if (!order.user?.email) return;

  await notificationService.sendOrderConfirmed(
    {
      email: order.user.email,
      firstName: order.user.firstName,
    },
    {
      orderId: order.id,
      customerName: `${order.user.firstName} ${order.user.lastName}`.trim(),
      totalCents: order.totalCents,
    },
  );
}

function extractRefundAmountFromPayload(payloadJson: string | null): number {
  if (!payloadJson) return 0;

  try {
    const payload = JSON.parse(payloadJson) as { amountCents?: unknown };
    const value = payload.amountCents;
    return typeof value === 'number' && Number.isFinite(value) && value > 0 ? value : 0;
  } catch {
    return 0;
  }
}

export class PaymentService {
  private readonly prismaClient: PrismaLike;
  private readonly notificationService: NotificationService;
  private readonly invoicePdfService: InvoicePdfService;
  private readonly providerFactory: (provider: PaymentProviderKey) => PaymentProvider;
  private readonly paypalProvider?: PaypalPaymentProvider;

  constructor(deps: PaymentServiceDeps = {}) {
    this.prismaClient = deps.prismaClient ?? prisma;
    this.notificationService = deps.notificationService ?? new NotificationService();
    this.invoicePdfService = deps.invoicePdfService ?? new InvoicePdfService();
    this.providerFactory = deps.providerFactory ?? ((provider) => (provider === 'stripe' ? new StripePaymentProvider() : new PaypalPaymentProvider()));
    this.paypalProvider = deps.paypalProvider;
  }

  private getProvider(provider: PaymentProviderKey): PaymentProvider {
    return this.providerFactory(provider);
  }

  async createCheckout(providerKey: PaymentProviderKey, input: PaymentCheckoutInput): Promise<PaymentCheckoutSession> {
    const provider = this.getProvider(providerKey);
    const session = await provider.createCheckoutSession(input);

    await this.prismaClient.order.update({
      where: { id: input.orderId },
      data: {
        // Payment-backed orders stay draft until asynchronous provider confirmation.
        status: OrderStatus.DRAFT,
        paymentMethod: providerKey,
        paymentReference: session.reference,
      },
    });

    await AuditService.log({
      action: 'PAYMENT_CHECKOUT_CREATED',
      entity: 'Order',
      entityId: input.orderId,
      payload: {
        provider: providerKey,
        reference: session.reference,
      },
    });

    return session;
  }

  async createRefund(input: PaymentRefundInput): Promise<PaymentRefundResult> {
    const order = await this.prismaClient.order.findUnique({ where: { id: input.orderId } });

    if (!order) {
      throw new Error('Order not found.');
    }

    if (!order.paymentReference) {
      throw new Error('Order has no payment reference to refund.');
    }

    const providerKey = order.paymentMethod === 'stripe' || order.paymentMethod === 'paypal' ? order.paymentMethod : null;

    if (!providerKey) {
      throw new Error('Automatic provider detection failed for this order.');
    }

    const refundAmountCents = input.amountCents ?? order.totalCents;

    if (!Number.isFinite(refundAmountCents) || refundAmountCents <= 0) {
      throw new Error('Refund amount must be greater than zero.');
    }

    if (refundAmountCents > order.totalCents) {
      throw new Error('Refund amount exceeds paid amount for this order.');
    }

    const previousRefundLogs = await this.prismaClient.auditLog.findMany({
      where: {
        action: 'PAYMENT_REFUNDED',
        entity: 'Payment',
        entityId: order.id,
      },
      select: {
        payloadJson: true,
      },
    });

    const alreadyRefundedCents = previousRefundLogs.reduce(
      (sum, log) => sum + extractRefundAmountFromPayload(log.payloadJson),
      0,
    );

    if (alreadyRefundedCents + refundAmountCents > order.totalCents) {
      throw new Error('Refund amount exceeds remaining refundable amount for this order.');
    }

    const provider = this.getProvider(providerKey);
    await AuditService.log({
      action: 'PAYMENT_REFUND_REQUESTED',
      entity: 'Payment',
      entityId: order.id,
      payload: {
        provider: providerKey,
        amountCents: refundAmountCents,
        reason: input.reason,
      },
    });

    try {
      const refund = await provider.createRefund({
        ...input,
        amountCents: refundAmountCents,
        paymentReference: order.paymentReference,
      });

      await AuditService.log({
        action: 'PAYMENT_REFUNDED',
        entity: 'Payment',
        entityId: order.id,
        payload: {
          provider: providerKey,
          refundReference: refund.reference,
          amountCents: refundAmountCents,
          status: refund.status,
        },
      });

      return refund;
    } catch (error) {
      await AuditService.log({
        action: 'PAYMENT_REFUND_FAILED',
        entity: 'Payment',
        entityId: order.id,
        payload: {
          provider: providerKey,
          amountCents: refundAmountCents,
          reason: input.reason,
          message: error instanceof Error ? error.message : 'Unknown refund error',
        },
      });

      const baseMessage = error instanceof Error ? error.message : 'Refund failed';
      if (providerKey === 'paypal') {
        throw new Error(`PayPal refund failed: ${baseMessage}`);
      }

      throw error;
    }
  }

  async confirmPaypalCapture(orderId: string, paypalOrderId: string): Promise<{ orderId: string; status: OrderStatus }> {
    const order = await this.prismaClient.order.findUnique({
      where: { id: orderId },
      include: { user: true },
    });

    if (!order) {
      throw new Error('Order not found.');
    }

    const paypalProvider = this.paypalProvider ?? new PaypalPaymentProvider();
    const capture = await paypalProvider.captureOrder(paypalOrderId);
    const nextStatus = capture.status === 'succeeded' ? OrderStatus.CONFIRMED : resolveWebhookOrderStatus(order.status, 'failed');

    const updated = await this.prismaClient.order.update({
      where: { id: order.id },
      data: {
        status: nextStatus,
        paymentMethod: 'paypal',
        paymentReference: capture.reference,
      },
    });

    await AuditService.log({
      action: 'PAYMENT_CAPTURE_CONFIRMED',
      entity: 'Payment',
      entityId: order.id,
      payload: {
        provider: 'paypal',
        paypalOrderId,
        captureReference: capture.reference,
        status: capture.status,
      },
    });

    if (capture.status === 'succeeded') {
      await notifyOrderConfirmed(this.notificationService, order);
      await this.invoicePdfService.emailInvoice(order.id);
    }

    return { orderId: updated.id, status: updated.status };
  }

  async processWebhook(providerKey: PaymentProviderKey, rawBody: string, headers: Headers): Promise<void> {
    const provider = this.getProvider(providerKey);
    const event = await provider.parseWebhookEvent(rawBody, headers);

    const existing = await this.prismaClient.auditLog.findFirst({
      where: {
        action: 'PAYMENT_WEBHOOK_PROCESSED',
        entity: 'Payment',
        entityId: event.eventId,
      },
      select: { id: true },
    });

    if (existing) {
      return;
    }

    if (event.orderId && (event.status === 'succeeded' || event.status === 'failed' || event.status === 'refunded')) {
      const order = await this.prismaClient.order.findUnique({
        where: { id: event.orderId },
        include: { user: true },
      });

      if (order) {
        const nextStatus = resolveWebhookOrderStatus(order.status, event.status);

        await this.prismaClient.order.update({
          where: { id: order.id },
          data: {
            status: nextStatus,
            paymentMethod: providerKey,
            paymentReference: event.paymentReference ?? order.paymentReference,
          },
        });

        if (event.status === 'succeeded') {
          await notifyOrderConfirmed(this.notificationService, order);
          await this.invoicePdfService.emailInvoice(order.id);
        }

        if (event.status === 'refunded') {
          await AuditService.log({
            action: 'PAYMENT_REFUNDED',
            entity: 'Payment',
            entityId: order.id,
            payload: {
              provider: providerKey,
              paymentReference: event.paymentReference,
            },
          });
        }
      }
    }

    await AuditService.log({
      action: 'PAYMENT_WEBHOOK_PROCESSED',
      entity: 'Payment',
      entityId: event.eventId,
      payload: {
        provider: event.provider,
        rawType: event.rawType,
        orderId: event.orderId,
        paymentReference: event.paymentReference,
        status: event.status,
      },
    });
  }
}
