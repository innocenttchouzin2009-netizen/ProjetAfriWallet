import { NextRequest, NextResponse } from 'next/server';
import { PaymentService } from '@/features/payments/services/payment.service';

const paymentService = new PaymentService();

export async function POST(request: NextRequest) {
  try {
    const rawBody = await request.text();
    await paymentService.processWebhook('paypal', rawBody, request.headers);
    return NextResponse.json({ ok: true });
  } catch (error) {
    return NextResponse.json(
      { message: error instanceof Error ? error.message : 'PayPal webhook processing failed.' },
      { status: 400 },
    );
  }
}
