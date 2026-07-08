import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { OrderValidationError, PrismaOrderRepository } from '@/features/orders/repositories/PrismaOrderRepository';
import { parseBody } from '@/lib/security/zod';

const repository = new PrismaOrderRepository();

const CheckoutSchema = z.object({
  customer: z.object({
    firstName: z.string().min(1),
    lastName: z.string().min(1),
    email: z.string().email(),
  }),
  address: z.object({
    address: z.string().min(1),
    city: z.string().min(1),
    postalCode: z.string().min(1),
    country: z.string().min(1),
  }),
  items: z.array(
    z.object({
      name: z.string().min(1),
      quantity: z.coerce.number().int().positive(),
      unitPrice: z.coerce.number().positive(),
      sku: z.string().optional(),
      customInitials: z.string().max(5).optional(),
      customLogoUrl: z.string().optional(),
    }),
  ).min(1),
  shippingCents: z.coerce.number().int().nonnegative().optional(),
  discountCents: z.coerce.number().int().nonnegative().optional(),
});

export async function POST(request: NextRequest) {
  try {
    const body = parseBody(CheckoutSchema, await request.json());

    const result = await repository.createOnlineOrder({
      customer: body.customer,
      address: body.address,
      items: body.items,
      shippingCents: Number(body.shippingCents ?? 0),
      discountCents: Number(body.discountCents ?? 0),
    });

    return NextResponse.json(result, { status: 201 });
  } catch (error) {
    if (error instanceof OrderValidationError) {
      return NextResponse.json({ message: error.message }, { status: 400 });
    }

    return NextResponse.json(
      { message: error instanceof Error ? error.message : 'Unable to create checkout order' },
      { status: 500 },
    );
  }
}
