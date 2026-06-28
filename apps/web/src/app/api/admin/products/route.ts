import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { prisma } from '@/lib/prisma';
import { AuditService } from '@/features/audit/services/audit.service';
import { requireRole } from '@/features/auth/guards/require-role';
import { parseBody } from '@/lib/security/zod';

export const dynamic = 'force-dynamic';

const AdminProductSchema = z.object({
  name: z.string().min(1),
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

export async function GET(request: NextRequest) {
  const auth = requireRole(request, ['manager']);
  if (auth instanceof NextResponse) return auth;

  const products = await prisma.product.findMany({
    include: {
      variants: {
        orderBy: { createdAt: 'asc' },
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
      description: `${String(body.name ?? '').trim()} - fiche produit admin`,
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
