type BadgeProps = {
  children: React.ReactNode;
  variant?: 'default' | 'success' | 'warning';
};

export default function Badge({ children, variant = 'default' }: BadgeProps) {
  const styles = {
    default: 'bg-white/10 text-white',
    success: 'bg-[#C8A45C] text-black',
    warning: 'bg-[#f59e0b] text-black',
  };

  return <span className={`inline-flex rounded-full px-3 py-1 text-xs font-semibold ${styles[variant]}`}>{children}</span>;
}
