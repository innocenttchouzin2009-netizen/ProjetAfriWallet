type BreadcrumbProps = {
  items: { label: string; href?: string }[];
};

export default function Breadcrumb({ items }: BreadcrumbProps) {
  return (
    <nav className="flex flex-wrap items-center gap-2 text-sm text-white/60">
      {items.map((item, index) => (
        <span key={item.label} className="flex items-center gap-2">
          {item.href ? (
            <a href={item.href} className="hover:text-white">
              {item.label}
            </a>
          ) : (
            <span>{item.label}</span>
          )}
          {index < items.length - 1 && <span className="text-white/30">/</span>}
        </span>
      ))}
    </nav>
  );
}
