export type AdminOrderChannel = 'ONLINE' | 'POS';

export type AdminOrderStatus =
  | 'CONFIRMED'
  | 'IN_PRODUCTION'
  | 'READY'
  | 'SHIPPED'
  | 'DELIVERED';

export interface AdminOrderItem {
  id: string;
  sku: string;
  name: string;
  variantName: string;
  quantity: number;
  unitPriceCents: number;
  totalPriceCents: number;
}

export interface AdminOrder {
  id: string;
  customer: string;
  channel: AdminOrderChannel;
  status: AdminOrderStatus;
  total: string;
  totalCents: number;
  paymentMethod?: string | null;
  invoiceNumber?: string | null;
  shipment?: {
    carrier: 'DHL' | 'DPD' | 'UPS';
    trackingNumber: string;
    shippingStatus: 'CREATED' | 'IN_TRANSIT' | 'DELIVERED';
  } | null;
  createdAt: string;
  items: AdminOrderItem[];
}

export interface AdminOrdersFilters {
  channel: AdminOrderChannel | 'ALL';
  status: AdminOrderStatus | 'ALL';
}
