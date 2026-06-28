type AlertProps = {
  children: React.ReactNode;
  variant?: 'info' | 'success' | 'warning' | 'error';
};

const styles: Record<string, string> = {
  info: 'bg-white/10 text-white',
  success: 'bg-[#C8A45C] text-black',
  warning: 'bg-[#f59e0b] text-black',
  error: 'bg-[#ef4444] text-white',
};

export default function Alert({ children, variant = 'info' }: AlertProps) {
  return <div className={`rounded-3xl p-4 text-sm font-semibold ${styles[variant]}`}>{children}</div>;
}
