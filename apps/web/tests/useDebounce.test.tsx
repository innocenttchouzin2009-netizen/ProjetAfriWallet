import { renderHook, waitFor } from '@testing-library/react';
import { useDebounce } from '@/hooks/useDebounce/useDebounce';

describe('useDebounce', () => {
  it('returns the initial value and updates after delay', async () => {
    const { result, rerender } = renderHook(({ value, delay }) => useDebounce(value, delay), {
      initialProps: { value: 'hello', delay: 50 },
    });

    expect(result.current).toBe('hello');

    rerender({ value: 'world', delay: 50 });

    await waitFor(() => {
      expect(result.current).toBe('world');
    });
  });
});
