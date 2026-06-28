import { Product } from '@/features/catalog/types/catalog.types';
import { catalogProducts } from '@/features/catalog/data/catalog.data';

export class ProductRepository {
  static getAll(): Product[] {
    return catalogProducts;
  }

  static getBySlug(slug: string): Product | undefined {
    return catalogProducts.find((product) => product.slug === slug);
  }

  static getByCategory(categorySlug: string): Product[] {
    return catalogProducts.filter((product) => product.category.slug === categorySlug);
  }
}
