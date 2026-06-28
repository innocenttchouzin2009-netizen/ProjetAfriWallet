export type InventoryMovementType = 'IN' | 'OUT' | 'ADJUSTMENT';
export type InventoryMovementSource = 'ADMIN' | 'ONLINE' | 'POS' | 'SYSTEM';

export interface InventoryMovement {
  id: string;
  variantId: string;
  sku: string;
  quantityDelta: number;
  type: InventoryMovementType;
  source: InventoryMovementSource;
  reason?: string;
  createdAt: string;
}

export interface InventoryItem {
  variantId: string;
  productId: string;
  productName: string;
  variantName: string;
  sku: string;
  stock: number;
  lowStockThreshold: number;
  lowStockAlert: boolean;
  recentMovements: InventoryMovement[];
}

export interface InventoryOverview {
  items: InventoryItem[];
  lowStockCount: number;
  totalStockUnits: number;
}

export interface AdjustInventoryInput {
  variantId: string;
  quantityDelta: number;
  reason?: string;
  source?: InventoryMovementSource;
}
