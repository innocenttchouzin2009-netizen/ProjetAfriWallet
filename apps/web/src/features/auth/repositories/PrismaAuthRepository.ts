import { UserRole, type User } from '@prisma/client';
import { prisma } from '@/lib/prisma';
import type { AuthRole, AuthUserRecord, SafeAuthUser } from '../types/auth.types';

interface CreateUserInput {
  email: string;
  name: string;
  passwordHash: string;
  role?: AuthRole;
}

export class PrismaAuthRepository {
  private toAuthRole(role: UserRole): AuthRole {
    switch (role) {
      case UserRole.SUPER_ADMIN:
        return 'super-admin';
      case UserRole.MANAGER:
        return 'manager';
      case UserRole.PRODUCTION:
        return 'production';
      case UserRole.VENDOR:
        return 'vendor';
      case UserRole.SUPPORT:
        return 'support';
      case UserRole.CLIENT:
      default:
        return 'client';
    }
  }

  private toPrismaRole(role: AuthRole): UserRole {
    switch (role) {
      case 'super-admin':
        return UserRole.SUPER_ADMIN;
      case 'manager':
        return UserRole.MANAGER;
      case 'production':
        return UserRole.PRODUCTION;
      case 'vendor':
        return UserRole.VENDOR;
      case 'support':
        return UserRole.SUPPORT;
      case 'client':
      default:
        return UserRole.CLIENT;
    }
  }

  private splitName(name: string): { firstName: string; lastName: string } {
    const value = name.trim();
    if (!value) {
      return { firstName: 'Client', lastName: '' };
    }

    const [firstName, ...rest] = value.split(' ');
    return { firstName, lastName: rest.join(' ') };
  }

  private toAuthUserRecord(user: User): AuthUserRecord {
    return {
      id: user.id,
      email: user.email,
      name: `${user.firstName} ${user.lastName}`.trim(),
      role: this.toAuthRole(user.role),
      passwordHash: user.passwordHash,
    };
  }

  toSafeAuthUser(user: User): SafeAuthUser {
    const mapped = this.toAuthUserRecord(user);
    const { passwordHash, ...safeUser } = mapped;
    return safeUser;
  }

  async createUser(input: CreateUserInput): Promise<SafeAuthUser> {
    const { firstName, lastName } = this.splitName(input.name);

    const user = await prisma.user.create({
      data: {
        email: input.email.trim().toLowerCase(),
        passwordHash: input.passwordHash,
        firstName,
        lastName,
        role: this.toPrismaRole(input.role ?? 'client'),
      },
    });

    return this.toSafeAuthUser(user);
  }

  async findByEmail(email: string): Promise<AuthUserRecord | null> {
    const user = await prisma.user.findUnique({
      where: { email: email.trim().toLowerCase() },
    });

    if (!user) {
      return null;
    }

    return this.toAuthUserRecord(user);
  }

  async findById(id: string): Promise<SafeAuthUser | null> {
    const user = await prisma.user.findUnique({
      where: { id },
    });

    if (!user) {
      return null;
    }

    return this.toSafeAuthUser(user);
  }
}
