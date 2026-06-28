import { createHash } from 'crypto';
import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { requireRole } from '@/features/auth/guards/require-role';
import { parseBody } from '@/lib/security/zod';

export const dynamic = 'force-dynamic';

const UploadBodySchema = z.object({
  file: z.string().startsWith('data:image/'),
  fileName: z.string().min(1).max(255).optional(),
});

function cloudinaryConfig() {
  const cloudName = process.env.CLOUDINARY_CLOUD_NAME;
  const apiKey = process.env.CLOUDINARY_API_KEY;
  const apiSecret = process.env.CLOUDINARY_API_SECRET;

  if (!cloudName || !apiKey || !apiSecret) {
    return null;
  }

  return { cloudName, apiKey, apiSecret };
}

function signUpload(folder: string, timestamp: number, apiSecret: string) {
  const payload = `folder=${folder}&timestamp=${timestamp}${apiSecret}`;
  return createHash('sha1').update(payload).digest('hex');
}

export async function POST(request: NextRequest) {
  const auth = requireRole(request, ['manager']);
  if (auth instanceof NextResponse) return auth;

  const cfg = cloudinaryConfig();
  if (!cfg) {
    return NextResponse.json({ message: 'Cloudinary is not configured.' }, { status: 503 });
  }

  const body = parseBody(UploadBodySchema, await request.json());
  const timestamp = Math.floor(Date.now() / 1000);
  const folder = 'dopecute/products';

  const formData = new FormData();
  formData.append('file', body.file);
  formData.append('api_key', cfg.apiKey);
  formData.append('timestamp', String(timestamp));
  formData.append('folder', folder);
  formData.append('signature', signUpload(folder, timestamp, cfg.apiSecret));
  if (body.fileName) formData.append('filename_override', body.fileName);

  const response = await fetch(`https://api.cloudinary.com/v1_1/${cfg.cloudName}/image/upload`, {
    method: 'POST',
    body: formData,
  });

  if (!response.ok) {
    const payload = await response.json().catch(() => ({}));
    return NextResponse.json({ message: payload?.error?.message ?? 'Cloudinary upload failed.' }, { status: 502 });
  }

  const payload = (await response.json()) as {
    secure_url: string;
    public_id: string;
    width?: number;
    height?: number;
    format?: string;
  };

  return NextResponse.json({
    url: payload.secure_url,
    publicId: payload.public_id,
    width: payload.width,
    height: payload.height,
    format: payload.format,
  });
}
