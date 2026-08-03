import { COLLECTION_DEFINITIONS } from '@/features/admin/catalog/data/catalog-taxonomy';

const groupLabels: Record<string, string> = {
  country: 'City Collections',
  regional: 'Collections Regionales',
  special: 'Collections Speciales',
};

export default function AdminCollectionsPage() {
  const grouped = COLLECTION_DEFINITIONS.reduce<Record<string, typeof COLLECTION_DEFINITIONS>>((acc, item) => {
    const current = acc[item.group] ?? [];
    acc[item.group] = [...current, item];
    return acc;
  }, {});

  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-8 text-white md:px-10 lg:px-16">
      <section className="rounded-[36px] border border-white/10 bg-gradient-to-br from-[#C8A45C]/20 via-black/80 to-black p-8">
        <p className="text-sm uppercase tracking-[0.35em] text-[#C8A45C]">Admin</p>
        <h1 className="mt-4 text-4xl font-black md:text-5xl">Collections</h1>
        <p className="mt-4 max-w-2xl text-white/70">
          Pilote les collections du catalogue et utilise ces slugs dans les produits et promotions.
        </p>
      </section>

      <section className="mt-8 grid gap-6 lg:grid-cols-3">
        {Object.entries(grouped).map(([group, items]) => (
          <article key={group} className="rounded-[28px] border border-white/10 bg-white/5 p-6">
            <h2 className="text-xl font-black text-white">{groupLabels[group] ?? group}</h2>
            <ul className="mt-4 space-y-3">
              {items.map((collection) => (
                <li key={collection.slug} className="rounded-2xl border border-white/10 bg-black/20 px-4 py-3">
                  <p className="font-semibold text-white">{collection.label}</p>
                  <p className="mt-1 text-xs uppercase tracking-[0.18em] text-[#C8A45C]">{collection.slug}</p>
                </li>
              ))}
            </ul>
          </article>
        ))}
      </section>
    </main>
  );
}
