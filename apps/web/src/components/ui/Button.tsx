type ButtonProps = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: 'primary' | 'secondary' | 'ghost';
};

export default function Button({
  variant = 'primary',
  className = '',
  ...props
}: ButtonProps) {
  const base =
    'inline-flex items-center justify-center rounded-full px-5 py-3 text-sm font-semibold transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#C8A45C]';
  const variants: Record<string, string> = {
    primary: 'bg-[#C8A45C] text-black hover:bg-[#bfa760]',
    secondary: 'border border-white/10 bg-white/5 text-white hover:bg-white/10',
    ghost: 'bg-transparent text-white hover:bg-white/10',
  };

  return <button className={`${base} ${variants[variant] ?? variants.primary} ${className}`} {...props} />;
}
