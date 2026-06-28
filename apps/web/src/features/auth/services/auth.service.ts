import { PrismaAuthRepository } from '../repositories/PrismaAuthRepository';
import { hashPassword, verifyPassword } from '../utils/password';
import type {
  ForgotPasswordPayload,
  LoginPayload,
  RegisterPayload,
  ResetPasswordPayload,
  SafeAuthUser,
} from '../types/auth.types';

export class AuthService {
  constructor(private readonly repository = new PrismaAuthRepository()) {}

  async login(payload: LoginPayload): Promise<SafeAuthUser | null> {
    const user = await this.repository.findByEmail(payload.email);
    if (!user) {
      return null;
    }

    const isValid = await verifyPassword(payload.password, user.passwordHash);
    if (!isValid) {
      return null;
    }

    const { passwordHash, ...safeUser } = user;
    return safeUser;
  }

  async register(payload: RegisterPayload): Promise<SafeAuthUser> {
    const existingUser = await this.repository.findByEmail(payload.email);
    if (existingUser) {
      throw new Error('An account already exists for this email.');
    }

    const passwordHash = await hashPassword(payload.password);

    return this.repository.createUser({
      email: payload.email,
      name: payload.name,
      passwordHash,
      role: 'client',
    });
  }

  async forgotPassword(payload: ForgotPasswordPayload): Promise<boolean> {
    const user = await this.repository.findByEmail(payload.email);
    return Boolean(user);
  }

  async resetPassword(payload: ResetPasswordPayload): Promise<boolean> {
    return payload.password.length >= 8 && payload.password === payload.confirmPassword;
  }
}
