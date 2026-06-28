import { createHash } from 'crypto';
import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { prisma } from '@/lib/prisma';
import { requireRole } from '@/features/auth/guards/require-role';
import { parseBody } from '@/lib/security/zod';

export const dynamic = 'force-dynamic';

const UpdateImageSchema = z.object({
  isPrimary: z.boolean().optional(),
});

type Params = {
  params: {
    id: string;
    imageId: string;
  };
};

function cloudinaryConfig() {
  const cloudName = process.env.CLOUDINARY_CLOUD_NAME;
  const apiKey = process.env.CLOUDINARY_API_KEY;
  const apiSecret = process.env.CLOUDINARY_API_SECRET;
  if (!cloudName || !apiKey || !apiSecret) return null;
  return { cloudName, apiKey, apiSecret };
}

async function removeCloudinaryAsset(publicId: string) {
  const cfg = cloudinaryConfig();
  if (!cfg) return;

  const timestamp = Math.floor(Date.now() / 1000);
  const signature = createHash('sha1')
    .update(`public_id=${publicId}&timestamp=${timestamp}${cfg.apiSecret}`)
    .digest('hex');

  const formData = new FormData();
  formData.append('public_id', publicId);
  formData.append('api_key', cfg.apiKey);
  formData.append('timestamp', String(timestamp));
  formData.append('signature', signature);

  await fetch(`https://api.cloudinary.com/v1_1/${cfg.cloudName}/image/destroy`, {
    method: 'POST',
    body: formData,
  });
}

export async function PATCH(request: NextRequest, { params }: Params) {
  const auth = requireRole(request, ['manager']);
  if (auth instanceof NextResponse) return auth;

  const body = parseBody(UpdateImageSchema, await request.json());
  const image = await prisma.productImage.findFirst({
    where: {
      id: params.imageId,
      productId: params.id,
    },
  });

  if (!image) {
    return NextResponse.json({ message: 'Image not found' }, { status: 404 });
  }

  if (body.isPrimary) {
    await prisma.productImage.updateMany({
      where: { productId: params.id },
      data: { isPrimary: false },
    });
  }

  const updated = await prisma.productImage.update({
    where: { id: params.imageId },
    data: {
      isPrimary: body.isPrimary === undefined ? image.isPrimary : body.isPrimary,
    },
  });

  return NextResponse.json({
    id: updated.id,
    url: updated.url,
    publicId: updated.publicId,
    isPrimary: updated.isPrimary,
  });
}

export async function DELETE(request: NextRequest, { params }: Params) {
  const auth = requireRole(request, ['manager']);
  if (auth instanceof NextResponse) return auth;

  const image = await prisma.productImage.findFirst({
    where: {
      id: params.imageId,
      productId: params.id,
    },
  });

  if (!image) {
    return NextResponse.json({ message: 'Image not found' }, { status: 404 });
  }

  await prisma.productImage.delete({ where: { id: params.imageId } });

  if (image.publicId) {
    void removeCloudinaryAsset(image.publicId);
  }

  return NextResponse.json({ ok: true });
}
