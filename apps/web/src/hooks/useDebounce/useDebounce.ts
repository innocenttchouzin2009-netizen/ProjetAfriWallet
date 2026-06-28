import { useEffect, useState } from 'react';

export const useDebounce = <T>(value: T, delay = 300) => {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => window.clearTimeout(timeout);
  }, [value, delay]);

  return debouncedValue;
};
