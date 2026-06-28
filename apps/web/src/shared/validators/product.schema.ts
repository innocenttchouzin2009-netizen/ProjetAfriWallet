import { z } from '@dopecute/config';

export const ProductImageSchema = z.object({
  url: z.string().url(),
  alt: z.string().min(1),
});

export const VariantSchema = z.object({
  id: z.string().min(1),
  sku: z.string().min(1),
  name: z.string().min(1),
  price: z.number().nonnegative(),
  inStock: z.boolean(),
  attributes: z.record(z.string(), z.string()),
});

export const CustomizationZoneSchema = z.object({
  id: z.string().min(1),
  name: z.string().min(1),
  description: z.string().optional(),
  allowedOptions: z.array(z.string()),
});

export const InventorySchema = z.object({
  quantity: z.number().int().nonnegative(),
  lowStockThreshold: z.number().int().nonnegative().optional(),
});

export const ShippingSchema = z.object({
  weight: z.number().nonnegative(),
  dimensions: z.object({
    width: z.number().nonnegative(),
    height: z.number().nonnegative(),
    depth: z.number().nonnegative(),
  }),
  originCountry: z.string().min(1),
});

export const SEOSchema = z.object({
  title: z.string().min(1),
  description: z.string().min(1),
  keywords: z.array(z.string()),
});

export const CategorySchema = z.object({
  id: z.string().min(1),
  name: z.string().min(1),
  slug: z.string().min(1),
});

export const ProductSchema = z.object({
  id: z.string().min(1),
  sku: z.string().min(1),
  slug: z.string().min(1),
  name: z.string().min(1),
  shortDescription: z.string().min(1),
  description: z.string().min(1),
  category: CategorySchema,
  brand: z.string().min(1),
  price: z.number().nonnegative(),
  compareAtPrice: z.number().nonnegative().optional(),
  currency: z.literal('EUR'),
  images: z.array(ProductImageSchema).min(1),
  variants: z.array(VariantSchema).min(1),
  customizable: z.boolean(),
  customizationZones: z.array(CustomizationZoneSchema),
  inventory: InventorySchema,
  shipping: ShippingSchema,
  seo: SEOSchema,
  status: z.union([z.literal('draft'), z.literal('published')]),
});

export type ProductSchemaType = z.infer<typeof ProductSchema>;
