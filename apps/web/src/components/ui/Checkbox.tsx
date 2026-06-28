type CheckboxProps = React.InputHTMLAttributes<HTMLInputElement>;

export default function Checkbox({ className = '', ...props }: CheckboxProps) {
  return <input type="checkbox" className={`h-5 w-5 rounded border-white/20 text-[#C8A45C] ${className}`} {...props} />;
}
