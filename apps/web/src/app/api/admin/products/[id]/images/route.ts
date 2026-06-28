import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { prisma } from '@/lib/prisma';
import { requireRole } from '@/features/auth/guards/require-role';
import { parseBody } from '@/lib/security/zod';

export const dynamic = 'force-dynamic';

const ImageCreateSchema = z.object({
  url: z.string().url(),
  publicId: z.string().min(1).optional(),
  isPrimary: z.boolean().optional(),
});

type Params = {
  params: {
    id: string;
  };
};

export async function GET(request: NextRequest, { params }: Params) {
  const auth = requireRole(request, ['manager']);
  if (auth instanceof NextResponse) return auth;

  const images = await prisma.productImage.findMany({
    where: { productId: params.id },
    orderBy: [{ isPrimary: 'desc' }, { sortOrder: 'asc' }, { createdAt: 'asc' }],
  });

  return NextResponse.json(
    images.map((image) => ({
      id: image.id,
      url: image.url,
      publicId: image.publicId,
      isPrimary: image.isPrimary,
    })),
  );
}

export async function POST(request: NextRequest, { params }: Params) {
  const auth = requireRole(request, ['manager']);
  if (auth instanceof NextResponse) return auth;

  const body = parseBody(ImageCreateSchema, await request.json());

  const product = await prisma.product.findUnique({ where: { id: params.id }, select: { id: true } });
  if (!product) {
    return NextResponse.json({ message: 'Product not found' }, { status: 404 });
  }

  if (body.isPrimary) {
    await prisma.productImage.updateMany({
      where: { productId: params.id },
      data: { isPrimary: false },
    });
  }

  const latest = await prisma.productImage.findFirst({
    where: { productId: params.id },
    orderBy: { sortOrder: 'desc' },
    select: { sortOrder: true },
  });

  const image = await prisma.productImage.create({
    data: {
      productId: params.id,
      url: body.url,
      publicId: body.publicId,
      isPrimary: body.isPrimary ?? false,
      sortOrder: (latest?.sortOrder ?? -1) + 1,
    },
  });

  return NextResponse.json(
    {
      id: image.id,
      url: image.url,
      publicId: image.publicId,
      isPrimary: image.isPrimary,
    },
    { status: 201 },
  );
}
