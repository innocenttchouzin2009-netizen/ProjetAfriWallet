import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { prisma } from '@/lib/prisma';
import { requireRole } from '@/features/auth/guards/require-role';
import { parseBody } from '@/lib/security/zod';

export const dynamic = 'force-dynamic';

const PromotionSchema = z
  .object({
    code: z.string().min(3).max(64),
    discountType: z.enum(['PERCENTAGE', 'FIXED']),
    discountValue: z.coerce.number().positive(),
    startsAt: z.string().datetime(),
    endsAt: z.string().datetime(),
    minPurchase: z.coerce.number().nonnegative().optional(),
    usageLimit: z.coerce.number().int().positive().optional(),
    active: z.boolean(),
    appliesToAll: z.boolean(),
    scope: z.enum(['all', 'category', 'collection']).optional(),
    category: z.string().trim().optional(),
    collectionSlug: z.string().trim().optional(),
  })
  .refine((value) => new Date(value.startsAt).getTime() < new Date(value.endsAt).getTime(), {
    message: 'endsAt must be after startsAt',
    path: ['endsAt'],
  });

function toAdminPromotion(promotion: {
  id: string;
  code: string;
  discountType: 'PERCENTAGE' | 'FIXED';
  discountValue: number;
  minPurchaseCents: number | null;
  usageLimit: number | null;
  usageCount: number;
  startsAt: Date;
  endsAt: Date;
  isActive: boolean;
  appliesToAll: boolean;
  category: string | null;
}) {
  const rawCategory = promotion.category;
  const scope = promotion.appliesToAll ? 'all' : rawCategory?.startsWith('collection:') ? 'collection' : 'category';
  const category = scope === 'category' ? rawCategory : null;
  const collectionSlug = scope === 'collection' ? rawCategory?.replace('collection:', '') ?? null : null;

  return {
    id: promotion.id,
    code: promotion.code,
    discountType: promotion.discountType,
    discountValue:
      promotion.discountType === 'FIXED' ? promotion.discountValue / 100 : promotion.discountValue,
    minPurchase: promotion.minPurchaseCents === null ? null : promotion.minPurchaseCents / 100,
    usageLimit: promotion.usageLimit,
    usageCount: promotion.usageCount,
    startsAt: promotion.startsAt.toISOString(),
    endsAt: promotion.endsAt.toISOString(),
    active: promotion.isActive,
    appliesToAll: promotion.appliesToAll,
    scope,
    category,
    collectionSlug,
  };
}

export async function GET(request: NextRequest) {
  const auth = requireRole(request, ['manager']);
  if (auth instanceof NextResponse) return auth;

  const promotions = await prisma.promotion.findMany({
    orderBy: [{ createdAt: 'desc' }],
  });

  return NextResponse.json(promotions.map(toAdminPromotion));
}

export async function POST(request: NextRequest) {
  const auth = requireRole(request, ['manager']);
  if (auth instanceof NextResponse) return auth;

  const body = parseBody(PromotionSchema, await request.json());
  const scope = body.scope ?? (body.appliesToAll ? 'all' : 'category');
  const normalizedCategory =
    scope === 'all'
      ? null
      : scope === 'collection'
        ? body.collectionSlug
          ? `collection:${body.collectionSlug.trim()}`
          : null
        : body.category?.trim() || null;

  const promotion = await prisma.promotion.create({
    data: {
      code: body.code.trim().toUpperCase(),
      discountType: body.discountType,
      discountValue:
        body.discountType === 'FIXED' ? Math.round(Number(body.discountValue) * 100) : Math.round(Number(body.discountValue)),
      minPurchaseCents:
        body.minPurchase === undefined ? null : Math.round(Number(body.minPurchase) * 100),
      usageLimit: body.usageLimit === undefined ? null : Number(body.usageLimit),
      startsAt: new Date(body.startsAt),
      endsAt: new Date(body.endsAt),
      isActive: Boolean(body.active),
      appliesToAll: scope === 'all',
      category: normalizedCategory,
    },
  });

  return NextResponse.json(toAdminPromotion(promotion), { status: 201 });
}
