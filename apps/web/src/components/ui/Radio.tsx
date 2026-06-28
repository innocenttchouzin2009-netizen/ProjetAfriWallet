type RadioProps = React.InputHTMLAttributes<HTMLInputElement>;

export default function Radio({ className = '', ...props }: RadioProps) {
  return <input type="radio" className={`h-5 w-5 text-[#C8A45C] ${className}`} {...props} />;
}
