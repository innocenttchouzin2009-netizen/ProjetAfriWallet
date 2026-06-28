import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { prisma } from '@/lib/prisma';
import { AuditService } from '@/features/audit/services/audit.service';
import { requireRole } from '@/features/auth/guards/require-role';
import { parseBody } from '@/lib/security/zod';

export const dynamic = 'force-dynamic';

const AdminProductSchema = z.object({
  name: z.string().min(1),
  description: z.string().max(4000).optional(),
  category: z.string().min(1).optional(),
  sku: z.string().min(1),
  price: z.coerce.number().nonnegative(),
  stock: z.coerce.number().int().nonnegative(),
  active: z.boolean(),
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
  slug: string;
  isActive: boolean;
  variants: { sku: string; priceCents: number; stock: number }[];
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

export async function GET(request: NextRequest) {
  const auth = requireRole(request, ['manager']);
  if (auth instanceof NextResponse) return auth;

  const products = await prisma.product.findMany({
    include: {
      variants: {
        orderBy: { createdAt: 'asc' },
      },
      images: {
        orderBy: [{ isPrimary: 'desc' }, { sortOrder: 'asc' }, { createdAt: 'asc' }],
      },
    },
    orderBy: {
      createdAt: 'desc',
    },
  });

  return NextResponse.json(products.map(toAdminProduct));
}

export async function POST(request: NextRequest) {
  const auth = requireRole(request, ['manager']);
  if (auth instanceof NextResponse) return auth;

  const body = parseBody(AdminProductSchema, await request.json());

  const product = await prisma.product.create({
    data: {
      name: String(body.name ?? '').trim(),
      slug: `${String(body.category ?? 'product').trim().toLowerCase()}-${Date.now()}`,
      description: String(body.description ?? '').trim() || `${String(body.name ?? '').trim()} - fiche produit admin`,
      isActive: Boolean(body.active),
      variants: {
        create: [
          {
            name: 'Standard',
            sku: String(body.sku ?? '').trim(),
            priceCents: Math.round(Number(body.price ?? 0) * 100),
            stock: Number(body.stock ?? 0),
            isActive: Boolean(body.active),
          },
        ],
      },
    },
    include: {
      variants: true,
      images: {
        orderBy: [{ isPrimary: 'desc' }, { sortOrder: 'asc' }, { createdAt: 'asc' }],
      },
    },
  });

  await AuditService.log({
    action: 'PRODUCT_CREATED',
    entity: 'Product',
    entityId: product.id,
    payload: {
      name: product.name,
      sku: product.variants[0]?.sku,
      active: product.isActive,
    },
  });

  return NextResponse.json(toAdminProduct(product), { status: 201 });
}
