import { Product } from '@/features/catalog/types/catalog.types';
import { ProductRepository } from '@/features/catalog/repositories/ProductRepository';

export const CatalogService = {
  getAllProducts(): Product[] {
    return ProductRepository.getAll();
  },

  getProductBySlug(slug: string): Product | undefined {
    return ProductRepository.getBySlug(slug);
  },

  getProductsByCategory(categorySlug: string): Product[] {
    return ProductRepository.getByCategory(categorySlug);
  },

  filterProducts(products: Product[], params: {
    query?: string;
    category?: string;
    color?: string;
    minPrice?: number;
    maxPrice?: number;
    inStock?: boolean;
  }) {
    return products.filter((product) => {
      const normalizedQuery = params.query?.trim().toLowerCase();
      const matchesQuery = normalizedQuery
        ? [
            product.name,
            product.shortDescription,
            product.description,
            product.brand,
            product.category.name,
            product.category.slug,
          ]
            .map((value) => value.toLowerCase())
            .some((value) => value.includes(normalizedQuery))
        : true;

      const matchesCategory = params.category ? product.category.slug === params.category : true;
      const matchesColor = params.color
        ? product.variants.some((variant) =>
            Object.values(variant.attributes).some((attribute) =>
              attribute.toLowerCase().includes(params.color!.toLowerCase()),
            ),
          )
        : true;
      const matchesMinPrice = params.minPrice !== undefined ? product.price >= params.minPrice : true;
      const matchesMaxPrice = params.maxPrice !== undefined ? product.price <= params.maxPrice : true;
      const matchesStock = params.inStock !== undefined
        ? params.inStock
          ? product.inventory.quantity > 0
          : product.inventory.quantity === 0
        : true;

      return matchesQuery && matchesCategory && matchesColor && matchesMinPrice && matchesMaxPrice && matchesStock;
    });
  },

  sortProducts(products: Product[], sort?: 'priceAsc' | 'priceDesc' | 'newest' | 'bestSelling') {
    if (!sort) return products;

    return [...products].sort((a, b) => {
      if (sort === 'priceAsc') return a.price - b.price;
      if (sort === 'priceDesc') return b.price - a.price;
      if (sort === 'newest') return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      if (sort === 'bestSelling') return (b.sales ?? 0) - (a.sales ?? 0);
      return 0;
    });
  },

  paginateProducts(products: Product[], page: number, pageSize: number) {
    const total = products.length;
    const start = (page - 1) * pageSize;
    const items = products.slice(start, start + pageSize);
    return { items, total };
  },

  getCategoryOptions(products: Product[]) {
    const categories = products.map((product) => product.category);
    const unique = new Map(categories.map((category) => [category.slug, category]));
    return Array.from(unique.values());
  },

  getColorOptions(products: Product[]) {
    const colors = products.flatMap((product) =>
      product.variants.flatMap((variant) => Object.values(variant.attributes)),
    );
    return Array.from(new Set(colors)).sort();
  },
};
