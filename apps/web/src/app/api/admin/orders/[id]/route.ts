import { NextRequest, NextResponse } from 'next/server';
import { OrderStatus } from '@prisma/client';
import { z } from 'zod';
import { prisma } from '@/lib/prisma';
import { AuditService } from '@/features/audit/services/audit.service';
import { NotificationService } from '@/features/notifications/services/notification.service';
import type { NotificationOrderStatus } from '@/features/notifications/types/notification.types';
import { requireRole } from '@/features/auth/guards/require-role';
import { parseBody } from '@/lib/security/zod';

export const dynamic = 'force-dynamic';

const ALLOWED_STATUSES: OrderStatus[] = [
  OrderStatus.CONFIRMED,
  OrderStatus.IN_PRODUCTION,
  OrderStatus.READY,
  OrderStatus.SHIPPED,
  OrderStatus.DELIVERED,
];

const notificationService = new NotificationService();

const AdminOrderStatusSchema = z.object({
  status: z.enum(['CONFIRMED', 'IN_PRODUCTION', 'READY', 'SHIPPED', 'DELIVERED']),
});

type Params = {
  params: {
    id: string;
  };
};

function parseStatus(value: unknown): OrderStatus | null {
  if (typeof value !== 'string') return null;
  const normalized = value.toUpperCase() as OrderStatus;
  return ALLOWED_STATUSES.includes(normalized) ? normalized : null;
}

export async function PUT(request: NextRequest, { params }: Params) {
  const auth = requireRole(request, ['manager', 'production', 'support']);
  if (auth instanceof NextResponse) return auth;

  const body = parseBody(AdminOrderStatusSchema, await request.json());
  const status = parseStatus(body.status);

  if (!status) {
    return NextResponse.json(
      { message: 'Invalid status. Allowed: CONFIRMED, IN_PRODUCTION, READY, SHIPPED, DELIVERED.' },
      { status: 400 },
    );
  }

  const order = await prisma.order.findUnique({
    where: { id: params.id },
    include: {
      user: true,
    },
  });
  if (!order) {
    return NextResponse.json({ message: 'Order not found.' }, { status: 404 });
  }

  const updated = await prisma.order.update({
    where: { id: params.id },
    data: { status },
  });

  await AuditService.log({
    action: 'ORDER_STATUS_UPDATED',
    entity: 'Order',
    entityId: updated.id,
    payload: {
      previousStatus: order.status,
      nextStatus: updated.status,
    },
  });

  const notificationStatusMap: Partial<Record<OrderStatus, NotificationOrderStatus>> = {
    CONFIRMED: 'CONFIRMED',
    IN_PRODUCTION: 'IN_PRODUCTION',
    SHIPPED: 'SHIPPED',
    DELIVERED: 'DELIVERED',
  };

  const notificationStatus = notificationStatusMap[updated.status];

  if (notificationStatus && order.user?.email) {
    await notificationService.sendOrderStatusUpdate(
      notificationStatus,
      {
        email: order.user.email,
        firstName: order.user.firstName,
      },
      {
        orderId: updated.id,
        customerName: `${order.user.firstName} ${order.user.lastName}`.trim(),
        totalCents: updated.totalCents,
      },
    );
  }

  return NextResponse.json({ id: updated.id, status: updated.status });
}
