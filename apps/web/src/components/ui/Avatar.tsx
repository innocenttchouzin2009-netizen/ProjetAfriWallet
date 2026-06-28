type AvatarProps = {
  src?: string;
  alt?: string;
  size?: 'sm' | 'md' | 'lg';
};

const sizes: Record<string, string> = {
  sm: 'h-10 w-10 text-sm',
  md: 'h-12 w-12 text-base',
  lg: 'h-16 w-16 text-xl',
};

export default function Avatar({ src, alt = 'Avatar', size = 'md' }: AvatarProps) {
  return src ? (
    <img src={src} alt={alt} className={`rounded-full object-cover ${sizes[size]}`} />
  ) : (
    <div className={`grid place-items-center rounded-full bg-white/10 text-white ${sizes[size]}`}>
      {alt.charAt(0)}
    </div>
  );
}
