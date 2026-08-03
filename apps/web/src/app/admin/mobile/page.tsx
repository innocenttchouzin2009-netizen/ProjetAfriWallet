import { cookies } from 'next/headers';
import { redirect } from 'next/navigation';
import MobileAdminHome from '@/features/admin/mobile/components/MobileAdminHome';

type MobileRole = 'super-admin' | 'manager' | 'production' | 'vendor' | 'support' | 'client';

function normalizeRole(value: string | undefined): MobileRole | null {
  if (!value) return null;
  const normalized = value.trim().toLowerCase().replace(/_/g, '-');
  if (normalized === 'super-admin' || normalized === 'manager' || normalized === 'production' || normalized === 'vendor' || normalized === 'support' || normalized === 'client') {
    return normalized;
  }
  return null;
}

function canAccessMobileAdmin(role: MobileRole | null) {
  return role === 'super-admin' || role === 'manager' || role === 'production' || role === 'vendor' || role === 'support';
}

export default function AdminMobilePage() {
  const cookieStore = cookies();
  const userId = cookieStore.get('dcs-user-id')?.value;
  const role = normalizeRole(cookieStore.get('dcs-user-role')?.value);

  if (!userId || !canAccessMobileAdmin(role)) {
    redirect('/login?next=/admin/mobile');
  }

  return <MobileAdminHome role={role} />;
}
