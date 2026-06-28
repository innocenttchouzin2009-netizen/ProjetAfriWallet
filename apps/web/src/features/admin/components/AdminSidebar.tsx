import Link from 'next/link';
import type { AdminNavItem } from '../types/admin.types';

const navigation: AdminNavItem[] = [
  { label: 'Dashboard', key: 'dashboard', description: 'Vue générale' },
  { label: 'Commandes', key: 'orders', description: 'Ventes en ligne' },
  { label: 'Caisse', key: 'pos', description: 'POS boutique' },
  { label: 'Produits', key: 'products', description: 'Catalogue' },
  { label: 'Stocks', key: 'stock', description: 'Inventaire partagé' },
  { label: 'Clients', key: 'clients', description: 'Base clients' },
  { label: 'Devis', key: 'quotes', description: 'Professionnels' },
  { label: 'Production', key: 'production', description: 'Broderie' },
  { label: 'Paiements', key: 'payments', description: 'Remboursements' },
  { label: 'Promotions', key: 'promotions', description: 'Offres' },
  { label: 'Audit', key: 'audit', description: 'Journal des actions' },
  { label: 'Paramètres', key: 'settings', description: 'Réglages' },
];

const navigationLinks: Partial<Record<AdminNavItem['key'], string>> = {
  dashboard: '/admin',
  orders: '/admin/orders',
  pos: '/admin/pos',
  products: '/admin/products',
  stock: '/admin/inventory',
  payments: '/admin/payments',
  audit: '/admin/audit',
  settings: '/admin/health',
};

export default function AdminSidebar() {
  return (
    <aside className="rounded-[32px] border border-white/10 bg-black/40 p-6">
      <div>
        <p className="text-sm uppercase tracking-[0.35em] text-[#C8A45C]">Back-office</p>
        <h2 className="mt-3 text-2xl font-black text-white">Dope&Cute</h2>
      </div>

      <nav className="mt-8 space-y-2">
        {navigation.map((item) => {
          const href = navigationLinks[item.key];

          if (!href) {
            return (
              <div
                key={item.key}
                className="flex w-full cursor-not-allowed flex-col rounded-[18px] border border-white/10 bg-white/5 px-4 py-3 text-left opacity-60"
              >
                <span className="font-semibold text-white">{item.label}</span>
                <span className="mt-1 text-sm text-white/50">{item.description}</span>
              </div>
            );
          }

          return (
            <Link
              key={item.key}
              href={href}
              className="flex w-full flex-col rounded-[18px] border border-white/10 bg-white/5 px-4 py-3 text-left transition hover:bg-white/10"
            >
              <span className="font-semibold text-white">{item.label}</span>
              <span className="mt-1 text-sm text-white/50">{item.description}</span>
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
