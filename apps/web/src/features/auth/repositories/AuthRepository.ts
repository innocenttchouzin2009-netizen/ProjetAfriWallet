import { hashPassword, verifyPassword } from '../utils/password';
import type {
  AuthUserRecord,
  ForgotPasswordPayload,
  LoginPayload,
  RegisterPayload,
  ResetPasswordPayload,
  SafeAuthUser,
} from '../types/auth.types';

export class AuthRepository {
  private users: AuthUserRecord[] | null = null;

  private async ensureSeedUsers() {
    if (this.users) {
      return;
    }

    const demoPasswordHash = await hashPassword('demo1234');

    this.users = [
      {
        id: 'user-1',
        email: 'admin@dopecute.studio',
        name: 'Super Admin',
        role: 'super-admin',
        passwordHash: demoPasswordHash,
      },
      {
        id: 'user-2',
        email: 'manager@dopecute.studio',
        name: 'Manager',
        role: 'manager',
        passwordHash: demoPasswordHash,
      },
      {
        id: 'user-3',
        email: 'client@dopecute.studio',
        name: 'Client',
        role: 'client',
        passwordHash: demoPasswordHash,
      },
    ];
  }

  private toSafeAuthUser(user: AuthUserRecord): SafeAuthUser {
    const { passwordHash, ...safeUser } = user;
    return safeUser;
  }

  async login(payload: LoginPayload): Promise<SafeAuthUser | null> {
    await this.ensureSeedUsers();

    const user = this.users?.find(
      (entry) => entry.email.toLowerCase() === payload.email.trim().toLowerCase(),
    );

    if (!user) {
      return null;
    }

    const isPasswordValid = await verifyPassword(payload.password, user.passwordHash);
    if (!isPasswordValid) {
      return null;
    }

    return this.toSafeAuthUser(user);
  }

  async register(payload: RegisterPayload): Promise<SafeAuthUser> {
    await this.ensureSeedUsers();

    const email = payload.email.trim().toLowerCase();
    const alreadyExists = this.users?.some((entry) => entry.email.toLowerCase() === email);

    if (alreadyExists) {
      throw new Error('An account already exists for this email.');
    }

    const newUser: AuthUserRecord = {
      id: `user-${Date.now()}`,
      email,
      name: payload.name.trim(),
      role: 'client',
      passwordHash: await hashPassword(payload.password),
    };

    this.users = [newUser, ...(this.users ?? [])];

    return this.toSafeAuthUser(newUser);
  }

  async forgotPassword(payload: ForgotPasswordPayload): Promise<boolean> {
    return payload.email.includes('@');
  }

  async resetPassword(payload: ResetPasswordPayload): Promise<boolean> {
    return payload.password.length >= 8 && payload.password === payload.confirmPassword;
  }
}
