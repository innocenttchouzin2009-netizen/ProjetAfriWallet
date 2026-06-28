export interface AdminNavItem {
  label: string;
  key: string;
  description: string;
}

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
