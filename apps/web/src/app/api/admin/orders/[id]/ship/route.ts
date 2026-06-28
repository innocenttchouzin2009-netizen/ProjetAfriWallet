import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { ShippingService } from '@/features/shipping/services/shipping.service';
import type { ShippingCarrier } from '@/features/shipping/types/shipping.types';
import { requireRole } from '@/features/auth/guards/require-role';
import { parseBody } from '@/lib/security/zod';
export const dynamic = 'force-dynamic';

const shippingService = new ShippingService();

const ShipOrderSchema = z.object({
  carrier: z.enum(['DHL', 'DPD', 'UPS']),
});

type Params = {
  params: {
    id: string;
  };
};

function parseCarrier(value: unknown): ShippingCarrier | null {
  if (value === 'DHL' || value === 'DPD' || value === 'UPS') {
    return value;
  }
  return null;
}

export async function POST(request: NextRequest, { params }: Params) {
  try {
    const auth = requireRole(request, ['manager', 'production']);
    if (auth instanceof NextResponse) return auth;

    const body = parseBody(ShipOrderSchema, await request.json());
    const carrier = parseCarrier(body.carrier);

    if (!carrier) {
      return NextResponse.json({ message: 'Invalid carrier. Allowed: DHL, DPD, UPS.' }, { status: 400 });
    }

    const shipment = await shippingService.shipOrder(params.id, carrier);
    return NextResponse.json({ orderId: params.id, shipment }, { status: 201 });
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Unable to create shipment.';
    const status = message === 'Order not found.' ? 404 : 400;
    return NextResponse.json({ message }, { status });
  }
}
