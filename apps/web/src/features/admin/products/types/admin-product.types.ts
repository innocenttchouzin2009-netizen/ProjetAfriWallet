export interface AdminProduct {
  id: string;
  name: string;
  price: number;
  stock: number;
  category: string;
  sku: string;
  active: boolean;
}

export interface AdminProductFormValues {
  name: string;
  price: number;
  stock: number;
  category: string;
  sku: string;
  active: boolean;
}
