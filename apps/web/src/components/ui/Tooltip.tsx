type TooltipProps = {
  content: string;
  children: React.ReactNode;
};

export default function Tooltip({ content, children }: TooltipProps) {
  return (
    <div className="group relative inline-flex">
      {children}
      <div className="pointer-events-none absolute bottom-full mb-2 hidden rounded-xl bg-black px-3 py-2 text-xs text-white group-hover:block">
        {content}
      </div>
    </div>
  );
}
