import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { hashPassword } from '@/features/auth/utils/password';
import { prisma } from '@/lib/prisma';
import { AuditService } from '@/features/audit/services/audit.service';
import { NotificationService } from '@/features/notifications/services/notification.service';
import { mapPrismaRoleToAppRole } from '@/features/auth/guards/require-role';
import { parseBody } from '@/lib/security/zod';
import { logger } from '@/lib/monitoring/logger';
import { enforceIpRateLimit } from '@/shared/security/rate-limit';

const notificationService = new NotificationService();

const RegisterSchema = z.object({
  name: z.string().min(2),
  email: z.string().email(),
  password: z.string().min(8),
});

export async function POST(request: NextRequest) {
  try {
    const limited = enforceIpRateLimit(request, {
      scope: 'auth-register',
      max: 5,
      windowMs: 15 * 60 * 1000,
    });
    if (limited) return limited;

    const body = parseBody(RegisterSchema, await request.json());

    const name = body.name.trim();
    const email = body.email.trim().toLowerCase();
    const password = body.password;

    if (!name || !email || !password) {
      return NextResponse.json({ message: 'Missing required fields.' }, { status: 400 });
    }

    const existing = await prisma.user.findUnique({ where: { email } });
    if (existing) {
      return NextResponse.json({ message: 'An account already exists for this email.' }, { status: 409 });
    }

    const [firstName, ...rest] = name.split(' ');
    const lastName = rest.join(' ');

    const user = await prisma.user.create({
      data: {
        email,
        passwordHash: await hashPassword(password),
        firstName,
        lastName,
        role: 'CLIENT',
      },
    });

    await AuditService.log({
      action: 'USER_REGISTERED',
      entity: 'User',
      entityId: user.id,
      userId: user.id,
      payload: {
        email: user.email,
      },
    });

    await notificationService.sendWelcome({
      email: user.email,
      firstName: user.firstName,
    });

    logger.info('User registered', {
      userId: user.id,
      email: user.email,
    });

    const role = mapPrismaRoleToAppRole(user.role);

    const response = NextResponse.json(
      {
        id: user.id,
        email: user.email,
        name: `${user.firstName} ${user.lastName}`.trim(),
        role,
      },
      { status: 201 },
    );

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
    logger.error('Register API failed', error);
    return NextResponse.json(
      { message: error instanceof Error ? error.message : 'Unable to register user' },
      { status: 500 },
    );
  }
}
