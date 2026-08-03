'use client';

import { useMemo } from 'react';
import Link from 'next/link';
import ProductFilters from '@/components/ProductFilters';
import SectionTitle from '@/components/SectionTitle';
import ProductGrid from '@/features/catalog/components/ProductGrid';
import { useProducts } from '@/features/catalog/hooks/useProducts';

type CityCollection = {
  slug: 'frpremium' | 'frheritage' | 'frriviera';
  label: string;
  subtitle: string;
  accent: string;
};

const COLLECTIONS: CityCollection[] = [
  {
    slug: 'frpremium',
    label: 'Premium Cities',
    subtitle: 'Paris, Lyon, Marseille, Toulouse, Nice and more.',
    accent: 'from-[#1D3557] via-[#457B9D] to-[#E63946]',
  },
  {
    slug: 'frheritage',
    label: 'Heritage Cities',
    subtitle: 'Rouen, Reims, Dijon, Tours, Avignon and more.',
    accent: 'from-[#3C1642] via-[#086375] to-[#F4A259]',
  },
  {
    slug: 'frriviera',
    label: 'Riviera & Alps',
    subtitle: 'Cannes, Annecy, Chamonix, Biarritz, La Rochelle and more.',
    accent: 'from-[#1B263B] via-[#415A77] to-[#E63946]',
  },
];

const PHOTO_SLICES = [
  {
    title: 'Paris',
    query: 'Paris France Eiffel Tower city',
    position: 'center top',
  },
  {
    title: 'Lyon',
    query: 'Lyon France old town city',
    position: 'center 35%',
  },
  {
    title: 'Marseille',
    query: 'Marseille France vieux port city',
    position: 'center 45%',
  },
  {
    title: 'Nice',
    query: 'Nice France promenade des anglais city',
    position: 'center bottom',
  },
] as const;

const buildPhotoUrl = (query: string) => `https://source.unsplash.com/featured/1200x900/?${encodeURIComponent(query)}`;

export default function FranceCityPage() {
  const { products, loading, error, params, setParams, total, categories, colors } = useProducts({
    pageSize: 12,
    page: 1,
    category: 'frpremium',
    query: 'City Cap',
  });

  const activeCollection = useMemo(
    () => COLLECTIONS.find((collection) => collection.slug === params.category) ?? COLLECTIONS[0],
    [params.category],
  );

  const currentPage = params.page ?? 1;
  const pageSize = params.pageSize ?? 12;
  const pageCount = Math.max(1, Math.ceil(total / pageSize));

  return (
    <main className="min-h-screen bg-[#070A12] px-6 py-20 text-white md:px-16">
      <section className="mx-auto max-w-6xl">
        <div className="rounded-3xl border border-white/15 bg-[#0E1017] p-8 shadow-[0_30px_80px_rgba(0,0,0,0.45)]">
          <p className="text-xs font-bold uppercase tracking-[0.3em] text-[#8EC5FF]">Dope&Cute Studio</p>
          <h1 className="mt-3 text-4xl font-black md:text-5xl">France City Collection</h1>
          <p className="mt-3 max-w-3xl text-sm text-white/70 md:text-base">
            Casquettes inspirees des villes francaises. Broderie du nom de ville,
            silhouette du monument, details D&C et personnalisation client.
          </p>

          <div className="mt-6 flex flex-wrap gap-3">
            {COLLECTIONS.map((collection) => {
              const isActive = params.category === collection.slug;
              return (
                <button
                  key={collection.slug}
                  type="button"
                  onClick={() => setParams({ ...params, category: collection.slug, query: 'City Cap', page: 1 })}
                  className={[
                    'rounded-full px-5 py-2 text-sm font-semibold transition',
                    isActive
                      ? 'bg-[#8EC5FF] text-black'
                      : 'border border-white/20 bg-white/5 text-white hover:bg-white/10',
                  ].join(' ')}
                >
                  {collection.label}
                </button>
              );
            })}
            <Link
              href="/shop"
              className="rounded-full border border-white/20 bg-transparent px-5 py-2 text-sm font-semibold text-white transition hover:bg-white/10"
            >
              Retour boutique
            </Link>
          </div>

          <div className={`mt-6 rounded-2xl bg-gradient-to-r ${activeCollection.accent} p-5`}>
            <p className="text-xs font-bold uppercase tracking-[0.24em] text-white/90">Collection active</p>
            <h2 className="mt-2 text-2xl font-black">{activeCollection.label}</h2>
            <p className="mt-1 text-sm text-white/85">{activeCollection.subtitle}</p>
          </div>
        </div>

        <div className="mt-6 overflow-hidden rounded-3xl border border-white/10 bg-white/5">
          <div className="grid gap-0 md:grid-cols-4">
            {PHOTO_SLICES.map((slice, index) => (
              <div key={slice.title} className="relative h-56 overflow-hidden border-b border-white/10 md:h-72 md:border-b-0 md:border-r md:border-r-white/10 last:border-r-0">
                <div
                  className="absolute inset-0 bg-cover bg-center"
                  style={{
                    backgroundImage: `url(${buildPhotoUrl(slice.query)})`,
                    backgroundPosition: slice.position,
                  }}
                />
                <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/20 to-transparent" />
                <div className="absolute inset-x-0 bottom-0 p-4">
                  <p className="text-xs font-bold uppercase tracking-[0.24em] text-white/70">Photo slice {index + 1}</p>
                  <h3 className="mt-1 text-xl font-black">{slice.title}</h3>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="mt-10 grid gap-6 lg:grid-cols-[2fr_1fr]">
          <div className="space-y-6">
            <SectionTitle title="City Caps" subtitle="Design Your Identity" />
            <ProductGrid products={products} loading={loading} error={error} />

            <div className="flex flex-col gap-4 rounded-3xl border border-white/10 bg-white/5 p-6 text-white sm:flex-row sm:items-center sm:justify-between">
              <div className="space-y-1">
                <div>{total} produits trouves</div>
                <div className="text-sm text-white/70">Page {currentPage} sur {pageCount}</div>
              </div>

              <div className="flex flex-wrap items-center gap-3">
                <button
                  type="button"
                  className="rounded-full border border-white/10 px-4 py-2 text-sm transition hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-40"
                  onClick={() => setParams({ ...params, page: Math.max(1, currentPage - 1) })}
                  disabled={currentPage <= 1}
                >
                  Precedent
                </button>
                <button
                  type="button"
                  className="rounded-full border border-white/10 px-4 py-2 text-sm transition hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-40"
                  onClick={() => setParams({ ...params, page: Math.min(pageCount, currentPage + 1) })}
                  disabled={currentPage >= pageCount}
                >
                  Suivant
                </button>
              </div>
            </div>
          </div>

          <ProductFilters
            params={params}
            categories={categories}
            colors={colors}
            onChange={(next) => setParams({ ...params, ...next })}
            onReset={() => setParams({ page: 1, pageSize: 12, category: activeCollection.slug, query: 'City Cap' })}
          />
        </div>
      </section>
    </main>
  );
}
