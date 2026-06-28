export interface AdminStat {
  label: string;
  value: string;
  change: string;
  tone: 'positive' | 'neutral' | 'warning';
}

export interface AdminOrder {
  id: string;
  customer: string;
  channel: 'Online' | 'Boutique' | 'Professionnel';
  total: string;
  status: 'En préparation' | 'Prête' | 'À expédier';
}

export interface ProductionTask {
  id: string;
  design: string;
  quantity: number;
  dueDate: string;
  priority: 'Haute' | 'Moyenne' | 'Faible';
}

export const adminStats: AdminStat[] = [
  { label: 'Ventes du jour', value: '12 840 €', change: '+8,4%', tone: 'positive' },
  { label: 'Commandes en ligne', value: '48', change: '+12%', tone: 'positive' },
  { label: 'Stock critique', value: '6 SKU', change: 'À surveiller', tone: 'warning' },
  { label: 'Devis en attente', value: '14', change: '2 nouveaux', tone: 'neutral' },
];

export const recentOrders: AdminOrder[] = [
  { id: 'ORD-1042', customer: 'Mina Laurent', channel: 'Online', total: '89,90 €', status: 'À expédier' },
  { id: 'ORD-1038', customer: 'Studio Nori', channel: 'Professionnel', total: '312,00 €', status: 'En préparation' },
  { id: 'ORD-1035', customer: 'Léa', channel: 'Boutique', total: '54,90 €', status: 'Prête' },
];

export const productionQueue: ProductionTask[] = [
  { id: 'PRD-201', design: 'Cap Signature Noir', quantity: 120, dueDate: 'Aujourd’hui', priority: 'Haute' },
  { id: 'PRD-202', design: 'Edition Crew', quantity: 60, dueDate: 'Demain', priority: 'Moyenne' },
  { id: 'PRD-203', design: 'Event Premium', quantity: 24, dueDate: 'Jeudi', priority: 'Faible' },
];
