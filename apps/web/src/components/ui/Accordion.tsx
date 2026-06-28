type AccordionItem = {
  title: string;
  content: React.ReactNode;
};

type AccordionProps = {
  items: AccordionItem[];
};

export default function Accordion({ items }: AccordionProps) {
  return (
    <div className="space-y-4">
      {items.map((item) => (
        <details key={item.title} className="rounded-3xl border border-white/10 bg-white/5 p-6">
          <summary className="cursor-pointer text-lg font-semibold">{item.title}</summary>
          <div className="mt-4 text-white/60">{item.content}</div>
        </details>
      ))}
    </div>
  );
}
