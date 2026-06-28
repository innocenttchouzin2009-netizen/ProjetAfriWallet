type SwitchProps = React.InputHTMLAttributes<HTMLInputElement>;

export default function Switch({ className = '', ...props }: SwitchProps) {
  return (
    <label className="relative inline-flex cursor-pointer items-center">
      <input type="checkbox" className="sr-only" {...props} />
      <span className={`h-6 w-11 rounded-full bg-white/10 transition ${className}`} />
    </label>
  );
}
