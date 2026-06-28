import { useEffect, useState } from 'react';
import { GetProductsParams, GetProductsResult } from '@/features/catalog/use-cases/GetProductsUseCase';
import { Product } from '@/features/catalog/types/catalog.types';

export type UseProductsState = {
  products: Product[];
  total: number;
  loading: boolean;
  error: string | null;
};

type FilterOption = {
  value: string;
  label: string;
};

export const useProducts = (initialParams: GetProductsParams = {}) => {
  const [params, setParams] = useState<GetProductsParams>(initialParams);
  const [result, setResult] = useState<GetProductsResult>({ products: [], total: 0 });
  const [categories, setCategories] = useState<FilterOption[]>([]);
  const [colors, setColors] = useState<FilterOption[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;

    const run = async () => {
      setLoading(true);
      setError(null);

      try {
        const search = new URLSearchParams();
        if (params.query) search.set('query', params.query);
        if (params.category) search.set('category', params.category);
        if (params.color) search.set('color', params.color);
        if (params.minPrice !== undefined) search.set('minPrice', String(params.minPrice));
        if (params.maxPrice !== undefined) search.set('maxPrice', String(params.maxPrice));
        if (params.inStock !== undefined) search.set('inStock', String(params.inStock));
        if (params.sort) search.set('sort', params.sort);
        search.set('page', String(params.page ?? 1));
        search.set('pageSize', String(params.pageSize ?? 12));

        const response = await fetch(`/api/catalog/products?${search.toString()}`);
        if (!response.ok) {
          throw new Error('Failed to load products');
        }

        const data = await response.json();
        if (!mounted) return;

        setResult({ products: data.products, total: data.total });
        setCategories(data.categories ?? []);
        setColors(data.colors ?? []);
      } catch (err) {
        if (!mounted) return;
        setError(err instanceof Error ? err.message : 'Unknown error');
        setResult({ products: [], total: 0 });
      } finally {
        if (mounted) setLoading(false);
      }
    };

    run();

    return () => {
      mounted = false;
    };
  }, [params]);

  const refresh = () => {
    setParams((current) => ({ ...current }));
  };

  return {
    products: result.products,
    total: result.total,
    loading,
    error,
    categories,
    colors,
    params,
    setParams,
    refresh,
  } as const;
};
