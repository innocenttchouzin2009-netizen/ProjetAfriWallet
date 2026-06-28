import { NextRequest } from 'next/server';

export type Role = 'SUPER_ADMIN' | 'MANAGER' | 'PRODUCTION' | 'VENDOR' | 'SUPPORT' | 'CLIENT';

export function assertRole(request: NextRequest, allowed: Role[]) {
  if (process.env.RC_ENFORCE_RBAC !== 'true') {
    return true;
  }

  const role = request.headers.get('x-user-role')?.toUpperCase() as Role | undefined;

  if (!role || !allowed.includes(role)) {
    return false;
  }

  return true;
}
