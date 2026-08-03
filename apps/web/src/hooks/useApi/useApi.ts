import { useEffect, useState } from 'react';
import { AppError } from '@dopecute/types';
import type { ApiState } from '@dopecute/types';

export type UseApiOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  headers?: HeadersInit;
  body?: BodyInit | null;
  parseJson?: boolean;
};

export const useApi = <T = unknown>(url: string, options: UseApiOptions = {}) => {
  const [state, setState] = useState<ApiState<T>>({
    data: null,
    error: null,
    status: 'idle',
  });

  useEffect(() => {
    if (!url) return;

    let isMounted = true;
    const controller = new AbortController();

    const fetchData = async () => {
      setState({ data: null, error: null, status: 'pending' });

      try {
        const response = await fetch(url, {
          method: options.method ?? 'GET',
          headers: {
            'Content-Type': 'application/json',
            ...options.headers,
          },
          body: options.body,
          signal: controller.signal,
        });

        if (!response.ok) {
          const errorText = await response.text();
          throw new AppError(errorText || response.statusText, `HTTP_${response.status}`);
        }

        const data = options.parseJson === false ? (await response.blob()) : (await response.json());
        const payload = { data } as T;

        if (isMounted) {
          setState({ data: payload, error: null, status: 'success' });
        }
      } catch (error) {
        if (!isMounted) return;
        const message = error instanceof AppError ? error.message : error instanceof Error ? error.message : 'Unknown error';
        setState({ data: null, error: message, status: 'error' });
      }
    };

    fetchData();

    return () => {
      isMounted = false;
      controller.abort();
    };
  }, [url, options.method, options.body, options.parseJson, options.headers]);

  return state;
};
