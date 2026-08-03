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
  supplierUrl: string;
  supplierName: string;
  supplierSku: string;
  price: number;
  stock: number;
  category: string;
  collectionSlug: string;
  hatType: string;
  sku: string;
  active: boolean;
  primaryImageUrl: string | null;
  images: AdminProductImage[];
}

export interface AdminProductFormValues {
  name: string;
  description: string;
  supplierUrl: string;
  supplierName: string;
  supplierSku: string;
  price: number;
  stock: number;
  category: string;
  hatType: string;
  sku: string;
  active: boolean;
}
