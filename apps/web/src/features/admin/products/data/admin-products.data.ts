import type { AdminProduct } from '../types/admin-product.types';

export const initialAdminProducts: AdminProduct[] = [
  { id: 'prod-001', name: 'D&C Signature Black', price: 49.9, stock: 24, category: 'Casquettes', sku: 'CAP-001', active: true },
  { id: 'prod-002', name: 'Urban Snapback', price: 44.9, stock: 18, category: 'Casquettes', sku: 'CAP-002', active: true },
  { id: 'prod-003', name: 'Patch Premium', price: 12.0, stock: 42, category: 'Accessoires', sku: 'PATCH-001', active: false },
];
