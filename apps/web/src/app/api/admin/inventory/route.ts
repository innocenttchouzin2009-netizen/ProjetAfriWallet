import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { InventoryService } from '@/features/inventory/services/inventory.service';
import { requireRole } from '@/features/auth/guards/require-role';
import { logger } from '@/lib/monitoring/logger';

export const dynamic = 'force-dynamic';

const inventoryService = new InventoryService();

const AdminInventoryQuerySchema = z.object({
  movementLimit: z.coerce.number().int().positive().max(50).default(6),
});

export async function GET(request: NextRequest) {
  try {
    const auth = requireRole(request, ['manager']);
    if (auth instanceof NextResponse) return auth;

    const { searchParams } = new URL(request.url);
    const parsed = AdminInventoryQuerySchema.safeParse({
      movementLimit: searchParams.get('movementLimit') ?? undefined,
    });

    if (!parsed.success) {
      return NextResponse.json({ message: parsed.error.issues.map((i) => i.message).join('; ') }, { status: 400 });
    }

    const movementLimit = parsed.data.movementLimit;

    const overview = await inventoryService.getOverview(movementLimit);
    return NextResponse.json(overview);
  } catch (error) {
    logger.error('Admin inventory fetch failed', error);
    return NextResponse.json({ message: error instanceof Error ? error.message : 'Unable to load inventory.' }, { status: 400 });
  }
}
