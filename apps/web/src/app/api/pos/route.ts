import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { OrderValidationError, PrismaOrderRepository } from '@/features/orders/repositories/PrismaOrderRepository';
import { AuditService } from '@/features/audit/services/audit.service';
import { NotificationService } from '@/features/notifications/services/notification.service';
import { parseBody } from '@/lib/security/zod';
import { enforceIpRateLimit } from '@/shared/security/rate-limit';

const repository = new PrismaOrderRepository();
const notificationService = new NotificationService();

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
    const limited = enforceIpRateLimit(request, {
      scope: 'pos-create',
      max: 30,
      windowMs: 60 * 1000,
    });
    if (limited) return limited;

    const body = parseBody(PosCheckoutSchema, await request.json());

    const result = await repository.createPOSOrder({
      cashierEmail: body.cashierEmail,
      items: body.items,
      discountCents: Number(body.discountCents ?? 0),
      paymentMethod: body.paymentMethod,
    });

    await AuditService.log({
      action: 'ORDER_CREATED_POS',
      entity: 'Order',
      entityId: result.id,
      payload: {
        itemCount: Array.isArray(body.items) ? body.items.length : 0,
        paymentMethod: body.paymentMethod,
      },
    });

    if (body.cashierEmail) {
      await notificationService.sendOrderConfirmed(
        {
          email: String(body.cashierEmail),
        },
        {
          orderId: result.id,
          totalCents:
            Number(body.items?.reduce((sum: number, item: { quantity?: number; unitPrice?: number }) => {
              const quantity = Number(item.quantity ?? 0);
              const unitPrice = Number(item.unitPrice ?? 0);
              return sum + Math.round(unitPrice * 100) * quantity;
            }, 0) ?? 0) - Number(body.discountCents ?? 0),
        },
      );
    }

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
