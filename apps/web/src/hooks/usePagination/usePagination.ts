import { useMemo, useState } from 'react';

export type PaginationState = {
  page: number;
  pageSize: number;
  total: number;
};

export const usePagination = ({ initialPage = 1, initialPageSize = 12, total = 0 } = {}) => {
  const [page, setPage] = useState(initialPage);
  const [pageSize, setPageSize] = useState(initialPageSize);

  const offset = useMemo(() => (page - 1) * pageSize, [page, pageSize]);
  const totalPages = useMemo(() => Math.max(1, Math.ceil(total / pageSize)), [pageSize, total]);

  const goToPage = (value: number) => {
    setPage(Math.max(1, Math.min(totalPages, value)));
  };

  const nextPage = () => goToPage(page + 1);
  const previousPage = () => goToPage(page - 1);

  const setPageSizeAndReset = (value: number) => {
    setPageSize(value);
    setPage(1);
  };

  return {
    page,
    pageSize,
    total,
    offset,
    totalPages,
    goToPage,
    nextPage,
    previousPage,
    setPageSize: setPageSizeAndReset,
  } as const;
};
