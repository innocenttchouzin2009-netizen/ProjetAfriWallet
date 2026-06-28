export type AuthRole = 'super-admin' | 'manager' | 'production' | 'vendor' | 'support' | 'client';

export interface AuthUserRecord {
  id: string;
  email: string;
  name: string;
  role: AuthRole;
  passwordHash: string;
}

export type SafeAuthUser = Omit<AuthUserRecord, 'passwordHash'>;

export interface LoginPayload {
  email: string;
  password: string;
}

export interface RegisterPayload {
  name: string;
  email: string;
  password: string;
}

export interface ForgotPasswordPayload {
  email: string;
}

export interface ResetPasswordPayload {
  password: string;
  confirmPassword: string;
}
