import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { prisma } from '@/lib/prisma';
import { verifyPassword } from '@/features/auth/utils/password';
import { mapPrismaRoleToAppRole } from '@/features/auth/guards/require-role';
import { parseBody } from '@/lib/security/zod';
import { logger } from '@/lib/monitoring/logger';
import { enforceIpRateLimit } from '@/shared/security/rate-limit';

const LoginSchema = z.object({
  email: z.string().email(),
  password: z.string().min(8),
});

export async function POST(request: NextRequest) {
  try {
    const limited = enforceIpRateLimit(request, {
      scope: 'auth-login',
      max: 5,
      windowMs: 15 * 60 * 1000,
    });
    if (limited) return limited;

    const body = parseBody(LoginSchema, await request.json());
    const email = body.email.trim().toLowerCase();

    const user = await prisma.user.findUnique({ where: { email } });
    if (!user) {
      return NextResponse.json({ message: 'Invalid credentials.' }, { status: 401 });
    }

    const valid = await verifyPassword(body.password, user.passwordHash);
    if (!valid) {
      return NextResponse.json({ message: 'Invalid credentials.' }, { status: 401 });
    }

    logger.info('User login success', { userId: user.id, email: user.email });

    const role = mapPrismaRoleToAppRole(user.role);

    const response = NextResponse.json({
      id: user.id,
      email: user.email,
      name: `${user.firstName} ${user.lastName}`.trim(),
      role,
    });

    response.cookies.set('dcs-user-id', user.id, {
      httpOnly: true,
      sameSite: 'lax',
      secure: process.env.NODE_ENV === 'production',
      path: '/',
      maxAge: 60 * 60 * 24 * 7,
    });

    response.cookies.set('dcs-user-role', role, {
      httpOnly: true,
      sameSite: 'lax',
      secure: process.env.NODE_ENV === 'production',
      path: '/',
      maxAge: 60 * 60 * 24 * 7,
    });

    return response;
  } catch (error) {
    logger.error('Login API failed', error);
    return NextResponse.json(
      { message: error instanceof Error ? error.message : 'Unable to login' },
      { status: 500 },
    );
  }
}
