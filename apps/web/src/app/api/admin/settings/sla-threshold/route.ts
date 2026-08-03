import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { prisma } from '@/lib/prisma';
import { requireRole } from '@/features/auth/guards/require-role';
import { AuditService } from '@/features/audit/services/audit.service';
import { parseBody } from '@/lib/security/zod';

export const dynamic = 'force-dynamic';

const SETTING_ENTITY_ID = 'sla-threshold';
const DEFAULT_THRESHOLD = 15;

const SlaThresholdSchema = z.object({
  thresholdPct: z.coerce.number().int().min(1).max(100),
});

async function getStoredThreshold() {
  const latest = await prisma.auditLog.findFirst({
    where: {
      action: 'ADMIN_SLA_THRESHOLD_UPDATED',
      entity: 'Settings',
      entityId: SETTING_ENTITY_ID,
    },
    orderBy: {
      createdAt: 'desc',
    },
  });

  if (!latest?.payloadJson) return DEFAULT_THRESHOLD;

  try {
    const payload = JSON.parse(latest.payloadJson) as { thresholdPct?: number };
    const value = Number(payload.thresholdPct);
    if (Number.isFinite(value) && value >= 1 && value <= 100) {
      return Math.round(value);
    }
  } catch {
    return DEFAULT_THRESHOLD;
  }

  return DEFAULT_THRESHOLD;
}

export async function GET(request: NextRequest) {
  const auth = requireRole(request, ['manager', 'production', 'support']);
  if (auth instanceof NextResponse) return auth;

  const thresholdPct = await getStoredThreshold();
  return NextResponse.json({ thresholdPct });
}

export async function PUT(request: NextRequest) {
  const auth = requireRole(request, ['manager']);
  if (auth instanceof NextResponse) return auth;

  const body = parseBody(SlaThresholdSchema, await request.json());
  const thresholdPct = Number(body.thresholdPct);

  await AuditService.log({
    action: 'ADMIN_SLA_THRESHOLD_UPDATED',
    entity: 'Settings',
    entityId: SETTING_ENTITY_ID,
    payload: {
      thresholdPct,
    },
  });

  return NextResponse.json({ thresholdPct });
}
