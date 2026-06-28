export type ApiResponse<T> = {
  data: T;
  error?: string;
};

export type ApiState<T> = {
  data: T | null;
  error: string | null;
  status: 'idle' | 'pending' | 'success' | 'error';
};
