import type { POSPaymentMethod, POSProduct } from '../types/pos.types';

export const posProducts: POSProduct[] = [
  { id: 'cap-001', name: 'D&C Signature Black', price: 49.9, sku: 'CAP-001', stock: 24, category: 'Casquettes' },
  { id: 'cap-002', name: 'Urban Snapback', price: 44.9, sku: 'CAP-002', stock: 18, category: 'Casquettes' },
  { id: 'cap-003', name: 'Camo Edition', price: 54.9, sku: 'CAP-003', stock: 9, category: 'Casquettes' },
  { id: 'patch-001', name: 'Patch Premium', price: 12.0, sku: 'PATCH-001', stock: 42, category: 'Accessoires' },
  { id: 'brod-001', name: 'Broderie 3D', price: 18.0, sku: 'BROD-001', stock: 15, category: 'Services' },
];

export const paymentMethods: POSPaymentMethod[] = [
  { id: 'cash', label: 'Espèces' },
  { id: 'card', label: 'Carte' },
];
