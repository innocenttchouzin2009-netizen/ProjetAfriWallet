import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { prisma } from '@/lib/prisma';
import { PaymentService } from '@/features/payments/services/payment.service';
import type { PaymentProviderKey } from '@/features/payments/types/payment.types';
import { parseBody } from '@/lib/security/zod';

const paymentService = new PaymentService();

const PaymentCheckoutSchema = z.object({
  provider: z.enum(['stripe', 'paypal']),
  orderId: z.string().min(1),
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
    const body = parseBody(PaymentCheckoutSchema, await request.json());
    const provider = parseProvider(body.provider);
    const orderId = body.orderId;

    if (!provider || !orderId) {
      return NextResponse.json({ message: 'provider and orderId are required.' }, { status: 400 });
    }

    const order = await prisma.order.findUnique({ where: { id: orderId }, include: { user: true } });
    if (!order) {
      return NextResponse.json({ message: 'Order not found.' }, { status: 404 });
    }

    const session = await paymentService.createCheckout(provider, {
      orderId: order.id,
      amountCents: order.totalCents,
      currency: 'eur',
      customerEmail: order.user?.email,
      returnUrl:
        provider === 'paypal'
          ? `${getAppUrl()}/checkout/paypal/return?orderId=${order.id}`
          : `${getAppUrl()}/checkout/success?orderId=${order.id}`,
      cancelUrl: `${getAppUrl()}/checkout/cancel?orderId=${order.id}&provider=${provider}`,
    });

    return NextResponse.json(session, { status: 201 });
  } catch (error) {
    return NextResponse.json(
      { message: error instanceof Error ? error.message : 'Unable to initialize payment checkout.' },
      { status: 500 },
    );
  }
}
