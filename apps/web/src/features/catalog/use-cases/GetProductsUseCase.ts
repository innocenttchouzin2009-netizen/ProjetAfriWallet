import { Product } from '@/features/catalog/types/catalog.types';
import { CatalogService } from '@/features/catalog/services/catalog.service';

export type GetProductsParams = {
  query?: string;
  category?: string;
  color?: string;
  minPrice?: number;
  maxPrice?: number;
  inStock?: boolean;
  sort?: 'priceAsc' | 'priceDesc' | 'newest' | 'bestSelling';
  page?: number;
  pageSize?: number;
};

export type GetProductsResult = {
  products: Product[];
  total: number;
};

export class GetProductsUseCase {
  static execute(params: GetProductsParams = {}): GetProductsResult {
    const products = CatalogService.getAllProducts();
    const filtered = CatalogService.filterProducts(products, params);
    const sorted = CatalogService.sortProducts(filtered, params.sort);
    const paginated = CatalogService.paginateProducts(sorted, params.page ?? 1, params.pageSize ?? 12);

    return {
      products: paginated.items,
      total: paginated.total,
    };
  }
}
