type InputProps = React.InputHTMLAttributes<HTMLInputElement>;

export default function Input({ className = '', ...props }: InputProps) {
  return (
    <input
      className={`w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-white outline-none transition placeholder:text-white/40 focus:border-[#C8A45C] focus:ring-2 focus:ring-[#C8A45C]/20 ${className}`}
      {...props}
    />
  );
}
