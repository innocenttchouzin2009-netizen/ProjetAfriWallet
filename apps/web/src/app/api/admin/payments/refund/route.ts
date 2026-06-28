import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { PaymentService } from '@/features/payments/services/payment.service';
import { parseBody } from '@/lib/security/zod';
import { requireRole } from '@/features/auth/guards/require-role';
import { logger } from '@/lib/monitoring/logger';
import { enforceIpRateLimit } from '@/shared/security/rate-limit';

export const dynamic = 'force-dynamic';

const paymentService = new PaymentService();

const RefundSchema = z.object({
  orderId: z.string().min(1),
  amountCents: z.number().int().positive().optional(),
  reason: z.string().max(280).optional(),
});

export async function POST(request: NextRequest) {
  try {
    const auth = requireRole(request, ['manager']);
    if (auth instanceof NextResponse) return auth;

    const limited = enforceIpRateLimit(request, {
      scope: 'admin-refund',
      max: 10,
      windowMs: 10 * 60 * 1000,
    });
    if (limited) return limited;

    const body = parseBody(RefundSchema, await request.json());

    const result = await paymentService.createRefund({
      orderId: body.orderId,
      amountCents: body.amountCents,
      reason: body.reason,
    });

    return NextResponse.json(result);
  } catch (error) {
    logger.error('Admin refund API failed', error);
    const message = error instanceof Error ? error.message : 'Unable to create refund.';
    const status =
      message.includes('PayPal refund failed')
        ? 502
        : message.includes('exceeds paid amount')
          ? 422
          : 400;

    return NextResponse.json({ message }, { status });
  }
}
