export interface AdminProductImage {
  id: string;
  url: string;
  publicId: string | null;
  isPrimary: boolean;
}

export interface AdminProduct {
  id: string;
  name: string;
  description: string;
  price: number;
  stock: number;
  category: string;
  sku: string;
  active: boolean;
  primaryImageUrl: string | null;
  images: AdminProductImage[];
}

export interface AdminProductFormValues {
  name: string;
  description: string;
  price: number;
  stock: number;
  category: string;
  sku: string;
  active: boolean;
}
