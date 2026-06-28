import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { OrderValidationError, PrismaOrderRepository } from '@/features/orders/repositories/PrismaOrderRepository';
import { parseBody } from '@/lib/security/zod';

const repository = new PrismaOrderRepository();

const PosCheckoutSchema = z.object({
  cashierEmail: z.string().email().optional(),
  discountCents: z.coerce.number().int().nonnegative().optional(),
  paymentMethod: z.enum(['cash', 'card']),
  items: z.array(
    z.object({
      name: z.string().min(1),
      quantity: z.coerce.number().int().positive(),
      unitPrice: z.coerce.number().positive(),
      sku: z.string().optional(),
    }),
  ).min(1),
});

export async function POST(request: NextRequest) {
  try {
    const body = parseBody(PosCheckoutSchema, await request.json());

    const result = await repository.createPOSOrder({
      cashierEmail: body.cashierEmail,
      items: body.items,
      discountCents: Number(body.discountCents ?? 0),
      paymentMethod: body.paymentMethod,
    });

    return NextResponse.json(result, { status: 201 });
  } catch (error) {
    if (error instanceof OrderValidationError) {
      return NextResponse.json({ message: error.message }, { status: 400 });
    }

    return NextResponse.json(
      { message: error instanceof Error ? error.message : 'Unable to create POS order' },
      { status: 500 },
    );
  }
}
