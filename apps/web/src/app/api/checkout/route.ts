import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { OrderValidationError, PrismaOrderRepository } from '@/features/orders/repositories/PrismaOrderRepository';
import { AuditService } from '@/features/audit/services/audit.service';
import { NotificationService } from '@/features/notifications/services/notification.service';
import { PaymentService } from '@/features/payments/services/payment.service';
import type { PaymentProviderKey } from '@/features/payments/types/payment.types';
import { parseBody } from '@/lib/security/zod';
import { logger } from '@/lib/monitoring/logger';
import { enforceIpRateLimit } from '@/shared/security/rate-limit';

const repository = new PrismaOrderRepository();
const notificationService = new NotificationService();
const paymentService = new PaymentService();

const CheckoutSchema = z.object({
  customer: z.object({
    firstName: z.string().min(1),
    lastName: z.string().min(1),
    email: z.string().email(),
    phone: z.string().optional(),
  }),
  address: z.object({
    address: z.string().min(1),
    postalCode: z.string().min(1),
    city: z.string().min(1),
    country: z.string().min(1),
  }),
  items: z.array(z.object({
    name: z.string().min(1),
    quantity: z.number().int().positive(),
    unitPrice: z.number().positive(),
    sku: z.string().optional(),
    customInitials: z.string().max(5).optional(),
    customLogoUrl: z.string().optional(),
  })).min(1),
  shippingCents: z.number().int().nonnegative().optional(),
  discountCents: z.number().int().nonnegative().optional(),
  paymentProvider: z.enum(['stripe', 'paypal']).optional(),
});

function getAppUrl() {
  return process.env.NEXT_PUBLIC_APP_URL ?? 'http://localhost:3000';
}

function parseProvider(value: unknown): PaymentProviderKey | null {
  if (value === 'stripe' || value === 'paypal') return value;
  return null;
}

export async function POST(request: NextRequest) {
  try {
    const limited = enforceIpRateLimit(request, {
      scope: 'checkout-create',
      max: 30,
      windowMs: 60 * 1000,
    });
    if (limited) return limited;

    const body = parseBody(CheckoutSchema, await request.json());

    const result = await repository.createOnlineOrder({
      customer: body.customer,
      address: body.address,
      items: body.items,
      shippingCents: Number(body.shippingCents ?? 0),
      discountCents: Number(body.discountCents ?? 0),
    });

    await AuditService.log({
      action: 'ORDER_CREATED_ONLINE',
      entity: 'Order',
      entityId: result.id,
      payload: {
        itemCount: Array.isArray(body.items) ? body.items.length : 0,
        customerEmail: body.customer?.email,
      },
    });

    const provider = parseProvider(body.paymentProvider);
    if (provider) {
      try {
        const amountCents =
          Number(body.items?.reduce((sum: number, item: { quantity?: number; unitPrice?: number }) => {
            const quantity = Number(item.quantity ?? 0);
            const unitPrice = Number(item.unitPrice ?? 0);
            return sum + Math.round(unitPrice * 100) * quantity;
          }, 0) ?? 0) + Number(body.shippingCents ?? 0) - Number(body.discountCents ?? 0);

        const paymentSession = await paymentService.createCheckout(provider, {
          orderId: result.id,
          amountCents,
          currency: 'eur',
          customerEmail: body.customer?.email ? String(body.customer.email) : undefined,
          returnUrl:
            provider === 'paypal'
              ? `${getAppUrl()}/checkout/paypal/return?orderId=${result.id}`
              : `${getAppUrl()}/checkout/success?orderId=${result.id}`,
          cancelUrl: `${getAppUrl()}/checkout/cancel?orderId=${result.id}&provider=${provider}`,
        });

        return NextResponse.json({ ...result, payment: paymentSession }, { status: 201 });
      } catch (paymentError) {
        logger.error('Payment initialization failed', paymentError, {
          orderId: result.id,
          provider,
        });
        await AuditService.log({
          action: 'PAYMENT_INIT_FAILED',
          entity: 'Order',
          entityId: result.id,
          payload: {
            provider,
            message: paymentError instanceof Error ? paymentError.message : 'Unknown payment error',
          },
        });

        return NextResponse.json(
          {
            id: result.id,
            message: paymentError instanceof Error ? paymentError.message : 'Payment initialization failed',
          },
          { status: 402 },
        );
      }
    }

    if (body.customer?.email) {
      await notificationService.sendOrderConfirmed(
        {
          email: String(body.customer.email),
          firstName: String(body.customer.firstName ?? ''),
        },
        {
          orderId: result.id,
          customerName: `${String(body.customer.firstName ?? '')} ${String(body.customer.lastName ?? '')}`.trim(),
          totalCents:
            Number(body.items?.reduce((sum: number, item: { quantity?: number; unitPrice?: number }) => {
              const quantity = Number(item.quantity ?? 0);
              const unitPrice = Number(item.unitPrice ?? 0);
              return sum + Math.round(unitPrice * 100) * quantity;
            }, 0) ?? 0) + Number(body.shippingCents ?? 0) - Number(body.discountCents ?? 0),
        },
      );
    }

    return NextResponse.json(result, { status: 201 });
  } catch (error) {
    if (error instanceof OrderValidationError) {
      return NextResponse.json({ message: error.message }, { status: 400 });
    }

    logger.error('Checkout API failed', error);

    return NextResponse.json(
      { message: error instanceof Error ? error.message : 'Unable to create checkout order' },
      { status: 500 },
    );
  }
}
