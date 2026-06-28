import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { AuthService } from '@/features/auth/services/auth.service';
import { parseBody } from '@/lib/security/zod';
import { enforceIpRateLimit } from '@/shared/security/rate-limit';

const authService = new AuthService();

const ForgotPasswordSchema = z.object({
  email: z.string().email(),
});

export async function POST(request: NextRequest) {
  try {
    const limited = enforceIpRateLimit(request, {
      scope: 'auth-forgot-password',
      max: 5,
      windowMs: 15 * 60 * 1000,
    });
    if (limited) return limited;

    const body = parseBody(ForgotPasswordSchema, await request.json());
    await authService.forgotPassword({ email: body.email });

    return NextResponse.json({ ok: true });
  } catch (error) {
    return NextResponse.json(
      { message: error instanceof Error ? error.message : 'Unable to process forgot password request.' },
      { status: 400 },
    );
  }
}
