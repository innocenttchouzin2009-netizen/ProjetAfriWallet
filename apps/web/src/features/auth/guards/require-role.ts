import { NextRequest, NextResponse } from 'next/server';

export type AppRole = 'super-admin' | 'manager' | 'production' | 'vendor' | 'support' | 'client';

const ADMIN_ROLES: AppRole[] = ['super-admin', 'manager', 'production', 'vendor', 'support'];

function normalizeRole(value: string | null | undefined): AppRole | null {
  if (!value) return null;

  const normalized = value.trim().toLowerCase().replace(/_/g, '-');
  if (
    normalized === 'super-admin' ||
    normalized === 'manager' ||
    normalized === 'production' ||
    normalized === 'vendor' ||
    normalized === 'support' ||
    normalized === 'client'
  ) {
    return normalized;
  }

  return null;
}

export type RequestAuth = {
  userId: string;
  role: AppRole;
};

export function readRequestAuth(request: NextRequest): RequestAuth | null {
  const cookieUserId = request.cookies.get('dcs-user-id')?.value;
  const cookieRole = normalizeRole(request.cookies.get('dcs-user-role')?.value);

  if (cookieUserId && cookieRole) {
    return {
      userId: cookieUserId,
      role: cookieRole,
    };
  }

  const headerUserId = request.headers.get('x-user-id');
  const headerRole = normalizeRole(request.headers.get('x-user-role'));

  if (headerUserId && headerRole) {
    return {
      userId: headerUserId,
      role: headerRole,
    };
  }

  return null;
}

function jsonError(status: 401 | 403, message: string) {
  return NextResponse.json({ message }, { status });
}

export function requireRole(request: NextRequest, allowedRoles: AppRole[]): RequestAuth | NextResponse {
  const auth = readRequestAuth(request);

  if (!auth) {
    return jsonError(401, 'Unauthorized: authentication required.');
  }

  if (auth.role === 'super-admin') {
    return auth;
  }

  if (!allowedRoles.includes(auth.role)) {
    return jsonError(403, 'Forbidden: insufficient role permission.');
  }

  return auth;
}

export function requireAnyAdminRole(request: NextRequest): RequestAuth | NextResponse {
  return requireRole(request, ADMIN_ROLES);
}

export function mapPrismaRoleToAppRole(value: string): AppRole {
  const normalized = normalizeRole(value);
  if (normalized) return normalized;

  if (value === 'SUPER_ADMIN') return 'super-admin';
  if (value === 'MANAGER') return 'manager';
  if (value === 'PRODUCTION') return 'production';
  if (value === 'VENDOR') return 'vendor';
  if (value === 'SUPPORT') return 'support';
  return 'client';
}
