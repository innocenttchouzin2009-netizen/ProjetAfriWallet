import { adminStats } from '../data/admin.data';

export default function AdminStatsCards() {
  return (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
      {adminStats.map((stat) => (
        <div key={stat.label} className="rounded-[24px] border border-white/10 bg-white/5 p-5">
          <p className="text-sm text-white/60">{stat.label}</p>
          <p className="mt-3 text-3xl font-black text-white">{stat.value}</p>
          <p
            className={`mt-3 text-sm ${
              stat.tone === 'positive'
                ? 'text-[#8ED8A2]'
                : stat.tone === 'warning'
                  ? 'text-[#F0B86E]'
                  : 'text-white/60'
            }`}
          >
            {stat.change}
          </p>
        </div>
      ))}
    </div>
  );
}
