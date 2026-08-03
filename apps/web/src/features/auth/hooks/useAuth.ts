"use client";

import { useCallback, useMemo, useState } from 'react';
import { ForgotPasswordUseCase } from '../use-cases/ForgotPasswordUseCase';
import { ResetPasswordUseCase } from '../use-cases/ResetPasswordUseCase';
import { LogoutUseCase } from '../use-cases/LogoutUseCase';
import type {
  AuthRole,
  ForgotPasswordPayload,
  LoginPayload,
  RegisterPayload,
  ResetPasswordPayload,
  SafeAuthUser,
} from '../types/auth.types';

export function useAuth() {
  const [user, setUser] = useState<SafeAuthUser | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const login = async (payload: LoginPayload) => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      const body = (await response.json()) as SafeAuthUser | { message?: string };
      if (!response.ok || 'message' in body) {
        setError('Identifiants invalides');
        return null;
      }

      const result = body as SafeAuthUser;
      setUser(result);
      return result;
    } finally {
      setLoading(false);
    }
  };

  const register = async (payload: RegisterPayload) => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      const body = (await response.json()) as SafeAuthUser | { message?: string };
      if (!response.ok || 'message' in body) {
        setError(('message' in body && body.message) || 'Registration failed');
        throw new Error(('message' in body && body.message) || 'Registration failed');
      }

      const result = body as SafeAuthUser;
      setUser(result);
      return result;
    } finally {
      setLoading(false);
    }
  };

  const forgotPassword = async (payload: ForgotPasswordPayload) => {
    setLoading(true);
    setError(null);
    const result = await new ForgotPasswordUseCase().execute(payload);
    return result;
  };

  const resetPassword = async (payload: ResetPasswordPayload) => {
    setLoading(true);
    setError(null);
    const result = await new ResetPasswordUseCase().execute(payload);
    return result;
  };

  const logout = async () => {
    setLoading(true);
    new LogoutUseCase().execute();
    setUser(null);
    setLoading(false);
  };

  const canAccess = useCallback((roles: AuthRole[]) => {
    if (!user) return false;
    return roles.includes(user.role);
  }, [user]);

  return useMemo(() => ({ user, error, loading, login, register, forgotPassword, resetPassword, logout, canAccess }), [user, error, loading, canAccess]);
}
