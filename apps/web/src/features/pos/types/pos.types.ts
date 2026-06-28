export interface POSProduct {
  id: string;
  name: string;
  price: number;
  sku: string;
  stock: number;
  category: string;
}

export interface POSLineItem {
  productId: string;
  sku: string;
  name: string;
  price: number;
  quantity: number;
}

export interface POSPaymentMethod {
  id: 'cash' | 'card';
  label: string;
}
