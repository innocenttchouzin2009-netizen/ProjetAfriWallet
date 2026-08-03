import { prisma } from '@/lib/prisma';
import type { Product as CatalogProduct, Variant } from '@/features/catalog/types/catalog.types';
import type { Product as PrismaProduct, ProductVariant, ProductImage } from '@prisma/client';

export interface ProductFilters {
  query?: string;
  category?: string;
  color?: string;
  minPrice?: number;
  maxPrice?: number;
  inStock?: boolean;
}

type PrismaProductWithVariants = PrismaProduct & {
  variants: ProductVariant[];
  images: ProductImage[];
};

function inferCategory(slug: string) {
  const [head] = slug.split('-');
  if (!head) {
    return { id: 'cat-uncategorized', name: 'Uncategorized', slug: 'uncategorized' };
  }

  if (head === 'premium') {
    return { id: 'cat-premium', name: 'Germany City Premium', slug: 'premium' };
  }

  if (head === 'regional') {
    return { id: 'cat-regional', name: 'Germany City Regional', slug: 'regional' };
  }

  if (head === 'nrw') {
    return { id: 'cat-nrw', name: 'Germany City NRW', slug: 'nrw' };
  }

  if (head === 'frpremium') {
    return { id: 'cat-frpremium', name: 'France City Premium', slug: 'frpremium' };
  }

  if (head === 'frheritage') {
    return { id: 'cat-frheritage', name: 'France City Heritage', slug: 'frheritage' };
  }

  if (head === 'frriviera') {
    return { id: 'cat-frriviera', name: 'France City Riviera & Alps', slug: 'frriviera' };
  }

  if (head === 'bepremium') {
    return { id: 'cat-bepremium', name: 'Belgium City Premium', slug: 'bepremium' };
  }

  if (head === 'beheritage') {
    return { id: 'cat-beheritage', name: 'Belgium City Heritage', slug: 'beheritage' };
  }

  if (head === 'beardennes') {
    return { id: 'cat-beardennes', name: 'Belgium City Ardennes & Coast', slug: 'beardennes' };
  }

  if (head === 'chpremium') {
    return { id: 'cat-chpremium', name: 'Swiss City Premium', slug: 'chpremium' };
  }

  if (head === 'chheritage') {
    return { id: 'cat-chheritage', name: 'Swiss City Heritage', slug: 'chheritage' };
  }

  if (head === 'chalps') {
    return { id: 'cat-chalps', name: 'Swiss City Alps & Lakes', slug: 'chalps' };
  }

  if (head === 'itpremium') {
    return { id: 'cat-itpremium', name: 'Italy City Premium', slug: 'itpremium' };
  }

  if (head === 'itheritage') {
    return { id: 'cat-itheritage', name: 'Italy City Heritage', slug: 'itheritage' };
  }

  if (head === 'italps') {
    return { id: 'cat-italps', name: 'Italy City Riviera & Alps', slug: 'italps' };
  }

  if (head === 'nlpremium') {
    return { id: 'cat-nlpremium', name: 'Netherlands City Premium', slug: 'nlpremium' };
  }

  if (head === 'nlheritage') {
    return { id: 'cat-nlheritage', name: 'Netherlands City Heritage', slug: 'nlheritage' };
  }

  if (head === 'nlcanals') {
    return { id: 'cat-nlcanals', name: 'Netherlands City Canals & Coast', slug: 'nlcanals' };
  }

  if (head === 'espremium') {
    return { id: 'cat-espremium', name: 'Spain City Premium', slug: 'espremium' };
  }

  if (head === 'esheritage') {
    return { id: 'cat-esheritage', name: 'Spain City Heritage', slug: 'esheritage' };
  }

  if (head === 'escoast') {
    return { id: 'cat-escoast', name: 'Spain City Coast & Islands', slug: 'escoast' };
  }

  return {
    id: `cat-${head}`,
    name: head.charAt(0).toUpperCase() + head.slice(1),
    slug: head,
  };
}

function mapVariant(variant: ProductVariant): Variant {
  return {
    id: variant.id,
    sku: variant.sku,
    name: variant.name,
    price: variant.priceCents / 100,
    inStock: variant.stock > 0 && variant.isActive,
    attributes: {
      option: variant.name,
    },
  };
}

function mapProduct(product: PrismaProductWithVariants): CatalogProduct {
  const variants = product.variants.map(mapVariant);
  const firstVariant = variants[0];
  const images = [...product.images].sort((a, b) => {
    if (a.isPrimary !== b.isPrimary) return a.isPrimary ? -1 : 1;
    return a.sortOrder - b.sortOrder;
  });
  const minPrice = variants.length > 0
    ? Math.min(...variants.map((variant) => variant.price))
    : 0;
  const inventoryQuantity = product.variants.reduce((total, variant) => total + variant.stock, 0);
  const category = inferCategory(product.slug);

  return {
    id: product.id,
    sku: firstVariant?.sku ?? product.id,
    slug: product.slug,
    name: product.name,
    shortDescription: product.description ?? 'Produit Dope&Cute Studio',
    description: product.description ?? 'Produit Dope&Cute Studio',
    category,
    brand: 'Dope&Cute Studio',
    price: minPrice,
    currency: 'EUR',
    images: images.map((image) => ({
      url: image.url,
      alt: product.name,
    })),
    variants,
    customizable: false,
    customizationZones: [],
    inventory: {
      quantity: inventoryQuantity,
      lowStockThreshold: 5,
    },
    shipping: {
      weight: 0.2,
      dimensions: { width: 15, height: 12, depth: 10 },
      originCountry: 'France',
    },
    seo: {
      title: `${product.name} | Dope&Cute Studio`,
      description: product.description ?? `Découvre ${product.name} sur Dope&Cute Studio.`,
      keywords: [product.slug, 'casquette', 'dope&cute'],
    },
    status: product.isActive ? 'published' : 'draft',
    createdAt: product.createdAt.toISOString(),
    sales: 0,
  };
}

export class PrismaProductRepository {
  static async findAll(): Promise<CatalogProduct[]> {
    const products = await prisma.product.findMany({
      include: {
        variants: true,
        images: true,
      },
      orderBy: {
        createdAt: 'desc',
      },
    });

    return products.map(mapProduct);
  }

  static async findBySlug(slug: string): Promise<CatalogProduct | null> {
    const product = await prisma.product.findUnique({
      where: { slug },
      include: {
        variants: true,
        images: true,
      },
    });

    if (!product) {
      return null;
    }

    return mapProduct(product);
  }

  static async filterProducts(filters: ProductFilters): Promise<CatalogProduct[]> {
    const query = filters.query?.trim();

    const products = await prisma.product.findMany({
      where: {
        isActive: true,
        AND: [
          query
            ? {
                OR: [
                  { name: { contains: query, mode: 'insensitive' } },
                  { description: { contains: query, mode: 'insensitive' } },
                  { slug: { contains: query, mode: 'insensitive' } },
                  {
                    variants: {
                      some: {
                        OR: [
                          { name: { contains: query, mode: 'insensitive' } },
                          { sku: { contains: query, mode: 'insensitive' } },
                        ],
                      },
                    },
                  },
                ],
              }
            : {},
          filters.category
            ? {
                slug: {
                  contains: filters.category,
                  mode: 'insensitive',
                },
              }
            : {},
          filters.color
            ? {
                variants: {
                  some: {
                    name: {
                      contains: filters.color,
                      mode: 'insensitive',
                    },
                  },
                },
              }
            : {},
          filters.inStock !== undefined
            ? {
                variants: {
                  some: {
                    stock: filters.inStock ? { gt: 0 } : { lte: 0 },
                  },
                },
              }
            : {},
          filters.minPrice !== undefined || filters.maxPrice !== undefined
            ? {
                variants: {
                  some: {
                    priceCents: {
                      gte: filters.minPrice !== undefined ? Math.round(filters.minPrice * 100) : undefined,
                      lte: filters.maxPrice !== undefined ? Math.round(filters.maxPrice * 100) : undefined,
                    },
                  },
                },
              }
            : {},
        ],
      },
      include: {
        variants: true,
        images: true,
      },
      orderBy: {
        createdAt: 'desc',
      },
    });

    return products.map(mapProduct);
  }
}
