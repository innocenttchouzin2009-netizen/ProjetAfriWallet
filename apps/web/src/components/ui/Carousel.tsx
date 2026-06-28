type CarouselProps = {
  items: React.ReactNode[];
};

export default function Carousel({ items }: CarouselProps) {
  return (
    <div className="flex gap-4 overflow-x-auto pb-4">
      {items.map((item, index) => (
        <div key={index} className="min-w-[280px] rounded-3xl border border-white/10 bg-white/5 p-6">
          {item}
        </div>
      ))}
    </div>
  );
}
