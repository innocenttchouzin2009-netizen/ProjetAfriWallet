import { z } from '@dopecute/config';

export const PaginationQuerySchema = z.object({
  page: z.preprocess((value) => Number(value), z.number().int().positive()).optional(),
  pageSize: z.preprocess((value) => Number(value), z.number().int().positive()).optional(),
});

export const SearchQuerySchema = z.object({
  query: z.string().min(1).optional(),
});

export type PaginationQuery = z.infer<typeof PaginationQuerySchema>;
export type SearchQuery = z.infer<typeof SearchQuerySchema>;
