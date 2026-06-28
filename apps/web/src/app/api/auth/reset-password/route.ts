import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { AuthService } from '@/features/auth/services/auth.service';
import { parseBody } from '@/lib/security/zod';
import { enforceIpRateLimit } from '@/shared/security/rate-limit';

const authService = new AuthService();

const ResetPasswordSchema = z.object({
  password: z.string().min(8),
  confirmPassword: z.string().min(8),
});

export async function POST(request: NextRequest) {
  try {
    const limited = enforceIpRateLimit(request, {
      scope: 'auth-reset-password',
      max: 5,
      windowMs: 15 * 60 * 1000,
    });
    if (limited) return limited;

    const body = parseBody(ResetPasswordSchema, await request.json());
    const ok = await authService.resetPassword({
      password: body.password,
      confirmPassword: body.confirmPassword,
    });

    if (!ok) {
      return NextResponse.json({ message: 'Invalid password reset payload.' }, { status: 400 });
    }

    return NextResponse.json({ ok: true });
  } catch (error) {
    return NextResponse.json(
      { message: error instanceof Error ? error.message : 'Unable to reset password.' },
      { status: 400 },
    );
  }
}
