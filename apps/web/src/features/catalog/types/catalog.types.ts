export interface ProductImage {
  url: string;
  alt: string;
}

export interface Variant {
  id: string;
  sku: string;
  name: string;
  price: number;
  inStock: boolean;
  attributes: Record<string, string>;
}

export interface CustomizationZone {
  id: string;
  name: string;
  description: string;
  allowedOptions: string[];
}

export interface Inventory {
  quantity: number;
  lowStockThreshold?: number;
}

export interface Shipping {
  weight: number;
  dimensions: {
    width: number;
    height: number;
    depth: number;
  };
  originCountry: string;
}

export interface SEO {
  title: string;
  description: string;
  keywords: string[];
}

export interface Category {
  id: string;
  name: string;
  slug: string;
}

export interface Product {
  id: string;
  sku: string;
  slug: string;
  name: string;
  shortDescription: string;
  description: string;
  category: Category;
  brand: string;
  price: number;
  compareAtPrice?: number;
  currency: 'EUR';
  images: ProductImage[];
  variants: Variant[];
  customizable: boolean;
  customizationZones: CustomizationZone[];
  inventory: Inventory;
  shipping: Shipping;
  seo: SEO;
  status: 'draft' | 'published';
  createdAt: string;
  sales: number;
}
