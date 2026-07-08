import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { prisma } from '@/lib/prisma';
import { AuditService } from '@/features/audit/services/audit.service';
import { requireRole } from '@/features/auth/guards/require-role';
import { parseBody } from '@/lib/security/zod';

export const dynamic = 'force-dynamic';

const AdminProductUpdateSchema = z.object({
  name: z.string().min(1).optional(),
  description: z.string().max(4000).optional(),
  sku: z.string().min(1).optional(),
  supplierUrl: z.string().url().optional().or(z.literal('')),
  supplierName: z.string().max(120).optional(),
  supplierSku: z.string().max(120).optional(),
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
  description: string | null;
  supplierUrl: string | null;
  supplierName: string | null;
  supplierSku: string | null;
  slug: string;
  isActive: boolean;
  variants: { sku: string; supplierUrl: string | null; supplierName: string | null; supplierSku: string | null; priceCents: number; stock: number }[];
  images: { id: string; url: string; publicId: string | null; isPrimary: boolean; sortOrder: number }[];
}) {
  const primaryVariant = product.variants[0];
  const stock = product.variants.reduce((sum, variant) => sum + variant.stock, 0);
  const orderedImages = [...product.images].sort((a, b) => {
    if (a.isPrimary !== b.isPrimary) return a.isPrimary ? -1 : 1;
    return a.sortOrder - b.sortOrder;
  });

  return {
    id: product.id,
    name: product.name,
    description: product.description ?? '',
    supplierUrl: primaryVariant?.supplierUrl ?? product.supplierUrl ?? '',
    supplierName: primaryVariant?.supplierName ?? product.supplierName ?? '',
    supplierSku: primaryVariant?.supplierSku ?? product.supplierSku ?? '',
    price: (primaryVariant?.priceCents ?? 0) / 100,
    stock,
    category: categoryFromSlug(product.slug),
    sku: primaryVariant?.sku ?? product.id,
    active: product.isActive,
    primaryImageUrl: orderedImages[0]?.url ?? null,
    images: orderedImages.map((image) => ({
      id: image.id,
      url: image.url,
      publicId: image.publicId,
      isPrimary: image.isPrimary,
    })),
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
    include: {
      variants: { orderBy: { createdAt: 'asc' } },
      images: {
        orderBy: [{ isPrimary: 'desc' }, { sortOrder: 'asc' }, { createdAt: 'asc' }],
      },
    },
  });

  if (!existing) {
    return NextResponse.json({ message: 'Product not found' }, { status: 404 });
  }

  const primaryVariant = existing.variants[0];

  const product = await prisma.product.update({
    where: { id: params.id },
    data: {
      name: String(body.name ?? existing.name).trim(),
      description: body.description === undefined ? existing.description : String(body.description).trim(),
      supplierUrl: body.supplierUrl === undefined ? existing.supplierUrl : String(body.supplierUrl).trim() || null,
      supplierName: body.supplierName === undefined ? existing.supplierName : String(body.supplierName).trim() || null,
      supplierSku: body.supplierSku === undefined ? existing.supplierSku : String(body.supplierSku).trim() || null,
      isActive: body.active === undefined ? existing.isActive : Boolean(body.active),
      variants: primaryVariant
        ? {
            update: {
              where: { id: primaryVariant.id },
              data: {
                sku: String(body.sku ?? primaryVariant.sku).trim(),
                supplierUrl: body.supplierUrl === undefined ? primaryVariant.supplierUrl : String(body.supplierUrl).trim() || null,
                supplierName: body.supplierName === undefined ? primaryVariant.supplierName : String(body.supplierName).trim() || null,
                supplierSku: body.supplierSku === undefined ? primaryVariant.supplierSku : String(body.supplierSku).trim() || null,
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
              supplierUrl: String(body.supplierUrl ?? '').trim() || null,
              supplierName: String(body.supplierName ?? '').trim() || null,
              supplierSku: String(body.supplierSku ?? '').trim() || null,
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
      images: {
        orderBy: [{ isPrimary: 'desc' }, { sortOrder: 'asc' }, { createdAt: 'asc' }],
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
