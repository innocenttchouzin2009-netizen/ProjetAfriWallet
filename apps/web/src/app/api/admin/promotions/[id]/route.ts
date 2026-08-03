import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { prisma } from '@/lib/prisma';
import { requireRole } from '@/features/auth/guards/require-role';
import { parseBody } from '@/lib/security/zod';

export const dynamic = 'force-dynamic';

const PromotionUpdateSchema = z
  .object({
    code: z.string().min(3).max(64).optional(),
    discountType: z.enum(['PERCENTAGE', 'FIXED']).optional(),
    discountValue: z.coerce.number().positive().optional(),
    startsAt: z.string().datetime().optional(),
    endsAt: z.string().datetime().optional(),
    minPurchase: z.coerce.number().nonnegative().nullable().optional(),
    usageLimit: z.coerce.number().int().positive().nullable().optional(),
    active: z.boolean().optional(),
    appliesToAll: z.boolean().optional(),
    scope: z.enum(['all', 'category', 'collection']).optional(),
    category: z.string().trim().nullable().optional(),
    collectionSlug: z.string().trim().nullable().optional(),
  })
  .superRefine((value, ctx) => {
    if (!value.startsAt || !value.endsAt) return;
    if (new Date(value.startsAt).getTime() >= new Date(value.endsAt).getTime()) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'endsAt must be after startsAt',
        path: ['endsAt'],
      });
    }
  });

type Params = {
  params: {
    id: string;
  };
};

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

export async function PUT(request: NextRequest, { params }: Params) {
  const auth = requireRole(request, ['manager']);
  if (auth instanceof NextResponse) return auth;

  const body = parseBody(PromotionUpdateSchema, await request.json());
  const existing = await prisma.promotion.findUnique({ where: { id: params.id } });

  if (!existing) {
    return NextResponse.json({ message: 'Promotion not found' }, { status: 404 });
  }

  const discountType = body.discountType ?? existing.discountType;
  const discountValueBase = body.discountValue ?? (existing.discountType === 'FIXED' ? existing.discountValue / 100 : existing.discountValue);
  const existingScope = existing.appliesToAll ? 'all' : existing.category?.startsWith('collection:') ? 'collection' : 'category';
  const scope = body.scope ?? existingScope;
  const normalizedCategory =
    scope === 'all'
      ? null
      : scope === 'collection'
        ? body.collectionSlug === undefined
          ? existingScope === 'collection'
            ? existing.category
            : null
          : body.collectionSlug === null
            ? null
            : body.collectionSlug.trim()
              ? `collection:${body.collectionSlug.trim()}`
              : null
        : body.category === undefined
          ? existingScope === 'category'
            ? existing.category
            : null
          : body.category === null
            ? null
            : body.category.trim() || null;

  const updated = await prisma.promotion.update({
    where: { id: params.id },
    data: {
      code: body.code === undefined ? existing.code : body.code.trim().toUpperCase(),
      discountType,
      discountValue: discountType === 'FIXED' ? Math.round(Number(discountValueBase) * 100) : Math.round(Number(discountValueBase)),
      startsAt: body.startsAt ? new Date(body.startsAt) : existing.startsAt,
      endsAt: body.endsAt ? new Date(body.endsAt) : existing.endsAt,
      minPurchaseCents:
        body.minPurchase === undefined
          ? existing.minPurchaseCents
          : body.minPurchase === null
            ? null
            : Math.round(Number(body.minPurchase) * 100),
      usageLimit:
        body.usageLimit === undefined
          ? existing.usageLimit
          : body.usageLimit === null
            ? null
            : Number(body.usageLimit),
      isActive: body.active === undefined ? existing.isActive : Boolean(body.active),
      appliesToAll: scope === 'all',
      category: normalizedCategory,
    },
  });

  return NextResponse.json(toAdminPromotion(updated));
}

export async function DELETE(request: NextRequest, { params }: Params) {
  const auth = requireRole(request, ['manager']);
  if (auth instanceof NextResponse) return auth;

  await prisma.promotion.delete({ where: { id: params.id } });

  return NextResponse.json({ ok: true });
}
