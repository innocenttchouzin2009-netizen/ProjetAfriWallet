import { NextRequest, NextResponse } from 'next/server';
import { requireRole } from '@/features/auth/guards/require-role';
import { COLLECTION_DEFINITIONS } from '@/features/admin/catalog/data/catalog-taxonomy';

export const dynamic = 'force-dynamic';

export async function GET(request: NextRequest) {
  const auth = requireRole(request, ['manager', 'production', 'support']);
  if (auth instanceof NextResponse) return auth;

  return NextResponse.json(COLLECTION_DEFINITIONS);
}
