import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import { PrismaProductRepository } from '@/features/catalog/repositories/PrismaProductRepository';
import { CatalogService } from '@/features/catalog/services/catalog.service';

const CatalogQuerySchema = z.object({
  page: z.coerce.number().int().positive().default(1),
  pageSize: z.coerce.number().int().positive().max(60).default(12),
  sort: z.enum(['priceAsc', 'priceDesc', 'newest', 'bestSelling']).optional(),
  inStock: z.enum(['true', 'false']).optional(),
  query: z.string().optional(),
  category: z.string().optional(),
  color: z.string().optional(),
  minPrice: z.coerce.number().nonnegative().optional(),
  maxPrice: z.coerce.number().nonnegative().optional(),
});

export async function GET(request: NextRequest) {
  const { searchParams } = new URL(request.url);

  const parsed = CatalogQuerySchema.safeParse({
    page: searchParams.get('page') ?? undefined,
    pageSize: searchParams.get('pageSize') ?? undefined,
    sort: searchParams.get('sort') ?? undefined,
    inStock: searchParams.get('inStock') ?? undefined,
    query: searchParams.get('query') ?? undefined,
    category: searchParams.get('category') ?? undefined,
    color: searchParams.get('color') ?? undefined,
    minPrice: searchParams.get('minPrice') ?? undefined,
    maxPrice: searchParams.get('maxPrice') ?? undefined,
  });

  if (!parsed.success) {
    return NextResponse.json({ message: parsed.error.issues.map((i) => i.message).join('; ') }, { status: 400 });
  }

  const { page, pageSize, sort, query, category, color, minPrice, maxPrice, inStock: inStockRaw } = parsed.data;
  const inStock = inStockRaw === undefined ? undefined : inStockRaw === 'true';

  const filters = {
    query,
    category,
    color,
    inStock,
    minPrice,
    maxPrice,
  };

  const [allProducts, filteredProducts] = await Promise.all([
    PrismaProductRepository.findAll(),
    PrismaProductRepository.filterProducts(filters),
  ]);

  const sortedProducts = CatalogService.sortProducts(filteredProducts, sort ?? undefined);
  const paginated = CatalogService.paginateProducts(sortedProducts, page, pageSize);

  const categories = CatalogService.getCategoryOptions(allProducts).map((category) => ({
    value: category.slug,
    label: category.name,
  }));
  const colors = CatalogService.getColorOptions(allProducts).map((color) => ({
    value: color,
    label: color,
  }));

  return NextResponse.json({
    products: paginated.items,
    total: paginated.total,
    categories,
    colors,
  });
}
