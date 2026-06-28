export const theme = {
  colors: {
    brand: {
      primary: '#1f2937',
      secondary: '#7c3aed',
      accent: '#f97316',
      muted: '#f3f4f6',
      surface: '#ffffff',
      border: '#e5e7eb',
      text: '#111827',
      textSecondary: '#6b7280',
    },
    success: '#22c55e',
    warning: '#f59e0b',
    danger: '#ef4444',
    background: '#f8fafc',
  },
  fontFamily: {
    sans: 'Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
    display: 'Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
  },
  spacing: {
    xs: '0.5rem',
    sm: '0.75rem',
    md: '1rem',
    lg: '1.5rem',
    xl: '2rem',
  },
  borderRadius: {
    sm: '0.375rem',
    md: '0.75rem',
    lg: '1.25rem',
  },
  shadows: {
    soft: '0 10px 30px rgba(15, 23, 42, 0.08)',
  },
  breakpoints: {
    sm: '640px',
    md: '768px',
    lg: '1024px',
    xl: '1280px',
  },
} as const;

export type ThemeTokens = typeof theme;
