import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { PaymentService } from '@/features/payments/services/payment.service';
import { parseBody } from '@/lib/security/zod';

const paymentService = new PaymentService();

const RefundSchema = z.object({
  orderId: z.string().min(1),
  amountCents: z.number().int().positive().optional(),
  reason: z.string().max(280).optional(),
});

export async function POST(request: NextRequest) {
  try {
    const body = parseBody(RefundSchema, await request.json());

    const result = await paymentService.createRefund({
      orderId: body.orderId,
      amountCents: body.amountCents,
      reason: body.reason,
    });

    return NextResponse.json(result);
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Unable to create refund.';
    const status = message.includes('PayPal refund failed') ? 502 : 400;
    return NextResponse.json({ message }, { status });
  }
}
