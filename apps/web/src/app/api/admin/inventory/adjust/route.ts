import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { InventoryService } from '@/features/inventory/services/inventory.service';
import { parseBody } from '@/lib/security/zod';
import { requireRole } from '@/features/auth/guards/require-role';
import { logger } from '@/lib/monitoring/logger';

export const dynamic = 'force-dynamic';

const inventoryService = new InventoryService();

const AdjustSchema = z.object({
  variantId: z.string().min(1),
  quantityDelta: z.number().int(),
  reason: z.string().max(280).optional(),
});

export async function POST(request: NextRequest) {
  try {
    const auth = requireRole(request, ['manager']);
    if (auth instanceof NextResponse) return auth;

    const body = parseBody(AdjustSchema, await request.json());

    const updated = await inventoryService.adjustStock({
      variantId: body.variantId,
      quantityDelta: body.quantityDelta,
      reason: body.reason,
      source: 'ADMIN',
    });

    return NextResponse.json(updated);
  } catch (error) {
    logger.error('Inventory adjustment failed', error);
    const message = error instanceof Error ? error.message : 'Unable to adjust stock.';
    const status =
      message === 'Variant not found.'
        ? 404
        : 400;
    return NextResponse.json({ message }, { status });
  }
}
