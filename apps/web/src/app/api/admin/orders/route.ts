import { NextRequest, NextResponse } from 'next/server';
import { OrderStatus } from '@prisma/client';
import { z } from 'zod';
import { prisma } from '@/lib/prisma';
import { requireRole } from '@/features/auth/guards/require-role';

export const dynamic = 'force-dynamic';

const ALLOWED_STATUSES: OrderStatus[] = [
  OrderStatus.CONFIRMED,
  OrderStatus.IN_PRODUCTION,
  OrderStatus.READY,
  OrderStatus.SHIPPED,
  OrderStatus.DELIVERED,
];

const AdminOrdersQuerySchema = z.object({
  limit: z.coerce.number().int().positive().max(200).default(10),
  status: z.enum(['CONFIRMED', 'IN_PRODUCTION', 'READY', 'SHIPPED', 'DELIVERED']).optional(),
  channel: z.enum(['ONLINE', 'POS']).optional(),
});

function parseStatus(value: string | null): OrderStatus | undefined {
  if (!value) return undefined;
  const normalized = value.toUpperCase() as OrderStatus;
  return ALLOWED_STATUSES.includes(normalized) ? normalized : undefined;
}

function parseChannel(value: string | null): 'ONLINE' | 'POS' | undefined {
  if (!value) return undefined;
  const normalized = value.toUpperCase();
  if (normalized === 'ONLINE' || normalized === 'POS') {
    return normalized;
  }
  return undefined;
}

export async function GET(request: NextRequest) {
  const auth = requireRole(request, ['manager', 'production', 'support']);
  if (auth instanceof NextResponse) return auth;

  const { searchParams } = new URL(request.url);
  const parsed = AdminOrdersQuerySchema.safeParse({
    limit: searchParams.get('limit') ?? undefined,
    status: searchParams.get('status') ?? undefined,
    channel: searchParams.get('channel') ?? undefined,
  });

  if (!parsed.success) {
    return NextResponse.json({ message: parsed.error.issues.map((i) => i.message).join('; ') }, { status: 400 });
  }

  const limit = parsed.data.limit;
  const status = parseStatus(parsed.data.status ?? null);
  const channel = parseChannel(parsed.data.channel ?? null);

  const orders = await prisma.order.findMany({
    where: {
      ...(status ? { status } : {}),
      ...(channel === 'ONLINE'
        ? { paymentReference: { startsWith: 'ONLINE-' } }
        : {}),
      ...(channel === 'POS'
        ? { paymentReference: { startsWith: 'POS-' } }
        : {}),
    },
    include: {
      user: true,
      items: {
        include: {
          productVariant: {
            include: {
              product: true,
            },
          },
        },
      },
    },
    orderBy: {
      createdAt: 'desc',
    },
    take: Number.isFinite(limit) && limit > 0 ? limit : 25,
  });

  const orderIds = orders.map((order) => order.id);
  const shippingLogs = orderIds.length
    ? await prisma.auditLog.findMany({
        where: {
          action: 'SHIPPING_CREATED',
          entity: 'Shipping',
          entityId: { in: orderIds },
        },
        orderBy: {
          createdAt: 'desc',
        },
      })
    : [];

  const invoiceLogs = orderIds.length
    ? await prisma.auditLog.findMany({
        where: {
          action: 'INVOICE_CREATED',
          entity: 'Invoice',
          entityId: { in: orderIds },
        },
        orderBy: {
          createdAt: 'desc',
        },
      })
    : [];

  const shippingByOrder = new Map<string, { carrier: 'DHL' | 'DPD' | 'UPS'; trackingNumber: string; shippingStatus: 'CREATED' | 'IN_TRANSIT' | 'DELIVERED' }>();
  const invoiceByOrder = new Map<string, string>();

  for (const log of shippingLogs) {
    if (!log.entityId || shippingByOrder.has(log.entityId) || !log.payloadJson) {
      continue;
    }

    try {
      const payload = JSON.parse(log.payloadJson) as {
        carrier?: 'DHL' | 'DPD' | 'UPS';
        trackingNumber?: string;
        shippingStatus?: 'CREATED' | 'IN_TRANSIT' | 'DELIVERED';
      };

      if (payload.carrier && payload.trackingNumber && payload.shippingStatus) {
        shippingByOrder.set(log.entityId, {
          carrier: payload.carrier,
          trackingNumber: payload.trackingNumber,
          shippingStatus: payload.shippingStatus,
        });
      }
    } catch {
      continue;
    }
  }

  for (const log of invoiceLogs) {
    if (!log.entityId || invoiceByOrder.has(log.entityId) || !log.payloadJson) {
      continue;
    }

    try {
      const payload = JSON.parse(log.payloadJson) as { invoiceNumber?: string };
      if (payload.invoiceNumber) {
        invoiceByOrder.set(log.entityId, payload.invoiceNumber);
      }
    } catch {
      continue;
    }
  }

  return NextResponse.json(
    orders.map((order) => {
      const orderChannel = order.paymentReference?.startsWith('POS-') ? 'POS' : 'ONLINE';

      return {
        id: order.id,
        customer: `${order.user.firstName} ${order.user.lastName}`.trim() || order.user.email,
        channel: orderChannel,
        status: order.status,
        total: `${(order.totalCents / 100).toFixed(2)} €`,
        totalCents: order.totalCents,
        paymentMethod: order.paymentMethod,
        invoiceNumber: invoiceByOrder.get(order.id) ?? null,
        shipment: shippingByOrder.get(order.id) ?? null,
        createdAt: order.createdAt.toISOString(),
        items: order.items.map((item) => ({
          id: item.id,
          sku: item.productVariant.sku,
          name: item.productVariant.product.name,
          variantName: item.productVariant.name,
          quantity: item.quantity,
          unitPriceCents: item.unitPriceCents,
          totalPriceCents: item.totalPriceCents,
        })),
      };
    }),
  );
}
