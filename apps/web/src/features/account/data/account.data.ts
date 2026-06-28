export interface CustomerProfile {
  name: string;
  email: string;
  phone: string;
  membership: string;
  address: string;
}

export interface AccountOrder {
  id: string;
  date: string;
  status: 'Livrée' | 'En préparation' | 'Confirmée';
  total: string;
  items: string[];
}

export interface SavedDesign {
  id: string;
  title: string;
  description: string;
  updatedAt: string;
  accent: string;
}

export const customerProfile: CustomerProfile = {
  name: 'Mina Laurent',
  email: 'mina@dopecute.studio',
  phone: '+33 6 12 34 56 78',
  membership: 'Premium',
  address: '12 Rue de la Mode, 75002 Paris',
};

export const recentOrders: AccountOrder[] = [
  {
    id: 'CMD-1042',
    date: '12 juin 2026',
    status: 'Livrée',
    total: '89,90 €',
    items: ['D&C Signature Black', 'Broderie prénom'],
  },
  {
    id: 'CMD-1018',
    date: '03 juin 2026',
    status: 'En préparation',
    total: '124,00 €',
    items: ['Studio Custom Cap', 'Patch premium'],
  },
  {
    id: 'CMD-0997',
    date: '24 mai 2026',
    status: 'Confirmée',
    total: '54,90 €',
    items: ['D&C Camo Edition'],
  },
];

export const savedDesigns: SavedDesign[] = [
  {
    id: 'DES-01',
    title: 'Cap Studio Noir',
    description: 'Broderie 3D + prénom en écriture cursive.',
    updatedAt: 'Il y a 2 jours',
    accent: 'from-[#C8A45C]/60 to-[#0D0D0D]',
  },
  {
    id: 'DES-02',
    title: 'Edition Event',
    description: 'Patch premium et couleurs contrastées pour un événement.',
    updatedAt: 'Il y a 1 semaine',
    accent: 'from-[#F5E7C8]/40 to-[#0D0D0D]',
  },
  {
    id: 'DES-03',
    title: 'Signature Crew',
    description: 'Version club avec logo personnalisé et bordure métallisée.',
    updatedAt: 'Il y a 2 semaines',
    accent: 'from-[#7B5E3A]/50 to-[#0D0D0D]',
  },
];
