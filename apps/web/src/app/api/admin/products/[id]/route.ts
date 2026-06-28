import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { prisma } from '@/lib/prisma';
import { AuditService } from '@/features/audit/services/audit.service';
import { requireRole } from '@/features/auth/guards/require-role';
import { parseBody } from '@/lib/security/zod';

export const dynamic = 'force-dynamic';

const AdminProductUpdateSchema = z.object({
  name: z.string().min(1).optional(),
  sku: z.string().min(1).optional(),
  price: z.coerce.number().nonnegative().optional(),
  stock: z.coerce.number().int().nonnegative().optional(),
  active: z.boolean().optional(),
});

function categoryFromSlug(slug: string): string {
  const [head] = slug.split('-');
  if (!head) return 'general';
  return head;
}

function toAdminProduct(product: {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  variants: { sku: string; priceCents: number; stock: number }[];
}) {
  const primaryVariant = product.variants[0];
  const stock = product.variants.reduce((sum, variant) => sum + variant.stock, 0);

  return {
    id: product.id,
    name: product.name,
    price: (primaryVariant?.priceCents ?? 0) / 100,
    stock,
    category: categoryFromSlug(product.slug),
    sku: primaryVariant?.sku ?? product.id,
    active: product.isActive,
  };
}

type Params = {
  params: {
    id: string;
  };
};

export async function PUT(request: NextRequest, { params }: Params) {
  const auth = requireRole(request, ['manager']);
  if (auth instanceof NextResponse) return auth;

  const body = parseBody(AdminProductUpdateSchema, await request.json());

  const existing = await prisma.product.findUnique({
    where: { id: params.id },
    include: { variants: { orderBy: { createdAt: 'asc' } } },
  });

  if (!existing) {
    return NextResponse.json({ message: 'Product not found' }, { status: 404 });
  }

  const primaryVariant = existing.variants[0];

  const product = await prisma.product.update({
    where: { id: params.id },
    data: {
      name: String(body.name ?? existing.name).trim(),
      isActive: body.active === undefined ? existing.isActive : Boolean(body.active),
      variants: primaryVariant
        ? {
            update: {
              where: { id: primaryVariant.id },
              data: {
                sku: String(body.sku ?? primaryVariant.sku).trim(),
                priceCents: Math.round(Number(body.price ?? primaryVariant.priceCents / 100) * 100),
                stock: Number(body.stock ?? primaryVariant.stock),
                isActive: body.active === undefined ? primaryVariant.isActive : Boolean(body.active),
              },
            },
          }
        : {
            create: {
              name: 'Standard',
              sku: String(body.sku ?? `${params.id}-STD`).trim(),
              priceCents: Math.round(Number(body.price ?? 0) * 100),
              stock: Number(body.stock ?? 0),
              isActive: body.active === undefined ? true : Boolean(body.active),
            },
          },
    },
    include: {
      variants: {
        orderBy: { createdAt: 'asc' },
      },
    },
  });

  await AuditService.log({
    action: 'PRODUCT_UPDATED',
    entity: 'Product',
    entityId: product.id,
    payload: {
      name: product.name,
      sku: product.variants[0]?.sku,
      active: product.isActive,
    },
  });

  return NextResponse.json(toAdminProduct(product));
}

export async function DELETE(_request: NextRequest, { params }: Params) {
  const auth = requireRole(_request, ['manager']);
  if (auth instanceof NextResponse) return auth;

  const existing = await prisma.product.findUnique({ where: { id: params.id } });

  await prisma.product.delete({ where: { id: params.id } });

  await AuditService.log({
    action: 'PRODUCT_DELETED',
    entity: 'Product',
    entityId: params.id,
    payload: {
      name: existing?.name,
    },
  });

  return NextResponse.json({ ok: true });
}
