"use client";

import type { ReactNode } from 'react';
import { useAuth } from '../hooks/useAuth';
import type { AuthRole } from '../types/auth.types';

interface AuthGuardProps {
  allowedRoles: AuthRole[];
  children: ReactNode;
}

export default function AuthGuard({ allowedRoles, children }: AuthGuardProps) {
  const { user, canAccess } = useAuth();

  if (!user || !canAccess(allowedRoles)) {
    return (
      <div className="rounded-[32px] border border-white/10 bg-white/5 p-8 text-white/70">
        <p className="text-sm uppercase tracking-[0.3em] text-[#C8A45C]">Accès restreint</p>
        <h2 className="mt-2 text-2xl font-black text-white">Connexion requise</h2>
        <p className="mt-3">Cette zone est réservée aux profils autorisés.</p>
      </div>
    );
  }

  return <>{children}</>;
}
