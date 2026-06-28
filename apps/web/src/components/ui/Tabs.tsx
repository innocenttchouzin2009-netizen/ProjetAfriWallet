type TabsProps = {
  tabs: { label: string; value: string }[];
  value: string;
  onChange: (value: string) => void;
};

export default function Tabs({ tabs, value, onChange }: TabsProps) {
  return (
    <div className="flex flex-wrap gap-2">
      {tabs.map((tab) => (
        <button
          key={tab.value}
          onClick={() => onChange(tab.value)}
          className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
            value === tab.value ? 'bg-[#C8A45C] text-black' : 'bg-white/10 text-white hover:bg-white/20'
          }`}
        >
          {tab.label}
        </button>
      ))}
    </div>
  );
}
