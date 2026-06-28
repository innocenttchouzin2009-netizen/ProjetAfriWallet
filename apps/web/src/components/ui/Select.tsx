type SelectProps = React.SelectHTMLAttributes<HTMLSelectElement> & {
  options: { value: string; label: string }[];
};

export default function Select({ options, className = '', ...props }: SelectProps) {
  return (
    <select
      className={`w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-white outline-none transition focus:border-[#C8A45C] focus:ring-2 focus:ring-[#C8A45C]/20 ${className}`}
      {...props}
    >
      {options.map((option) => (
        <option key={option.value} value={option.value}>
          {option.label}
        </option>
      ))}
    </select>
  );
}
