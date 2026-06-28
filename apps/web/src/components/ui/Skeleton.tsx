type SkeletonProps = {
  className?: string;
};

export default function Skeleton({ className = '' }: SkeletonProps) {
  return <div className={`animate-pulse rounded-3xl bg-white/10 ${className}`} />;
}
