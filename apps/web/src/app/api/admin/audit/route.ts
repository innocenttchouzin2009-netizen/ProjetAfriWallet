import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { prisma } from '@/lib/prisma';
import { requireRole } from '@/features/auth/guards/require-role';

export const dynamic = 'force-dynamic';

const AdminAuditQuerySchema = z.object({
  action: z.string().optional(),
  entity: z.string().optional(),
  period: z.enum(['24h', '7d', '30d']).optional(),
  limit: z.coerce.number().int().positive().max(500).default(100),
});

function getPeriodDate(period: string | null): Date | null {
  const now = new Date();

  if (period === '24h') return new Date(now.getTime() - 24 * 60 * 60 * 1000);
  if (period === '7d') return new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
  if (period === '30d') return new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);

  return null;
}

export async function GET(request: NextRequest) {
  const auth = requireRole(request, ['manager', 'support']);
  if (auth instanceof NextResponse) return auth;

  const { searchParams } = new URL(request.url);
  const parsed = AdminAuditQuerySchema.safeParse({
    action: searchParams.get('action') ?? undefined,
    entity: searchParams.get('entity') ?? undefined,
    period: searchParams.get('period') ?? undefined,
    limit: searchParams.get('limit') ?? undefined,
  });

  if (!parsed.success) {
    return NextResponse.json({ message: parsed.error.issues.map((i) => i.message).join('; ') }, { status: 400 });
  }

  const { action, entity, period, limit } = parsed.data;

  const fromDate = getPeriodDate(period ?? null);

  const logs = await prisma.auditLog.findMany({
    where: {
      ...(action && action !== 'ALL' ? { action } : {}),
      ...(entity && entity !== 'ALL' ? { entity } : {}),
      ...(fromDate ? { createdAt: { gte: fromDate } } : {}),
    },
    orderBy: {
      createdAt: 'desc',
    },
    take: limit,
  });

  const actions = await prisma.auditLog.findMany({
    distinct: ['action'],
    select: { action: true },
    orderBy: { action: 'asc' },
  });

  const entities = await prisma.auditLog.findMany({
    distinct: ['entity'],
    select: { entity: true },
    orderBy: { entity: 'asc' },
  });

  return NextResponse.json({
    logs: logs.map((log) => ({
      id: log.id,
      action: log.action,
      entity: log.entity,
      entityId: log.entityId,
      userId: log.userId,
      ipAddress: log.ipAddress,
      payload: log.payloadJson ? JSON.parse(log.payloadJson) : null,
      createdAt: log.createdAt.toISOString(),
    })),
    actions: actions.map((entry) => entry.action),
    entities: entities.map((entry) => entry.entity),
  });
}
