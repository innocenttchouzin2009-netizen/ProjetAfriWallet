import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { PaymentService } from '@/features/payments/services/payment.service';
import { parseBody } from '@/lib/security/zod';

const paymentService = new PaymentService();

const PaypalCaptureSchema = z.object({
  orderId: z.string().min(1),
  paypalOrderId: z.string().min(1),
});

export async function POST(request: NextRequest) {
  try {
    const body = parseBody(PaypalCaptureSchema, await request.json());

    const result = await paymentService.confirmPaypalCapture(body.orderId, body.paypalOrderId);
    return NextResponse.json(result);
  } catch (error) {
    return NextResponse.json(
      { message: error instanceof Error ? error.message : 'Unable to capture PayPal payment.' },
      { status: 400 },
    );
  }
}
