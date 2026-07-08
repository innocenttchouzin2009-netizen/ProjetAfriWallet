'use client';

import { useMemo } from 'react';
import Link from 'next/link';
import ProductFilters from '@/components/ProductFilters';
import SectionTitle from '@/components/SectionTitle';
import ProductGrid from '@/features/catalog/components/ProductGrid';
import { useProducts } from '@/features/catalog/hooks/useProducts';

type CityCollection = {
  slug: 'chpremium' | 'chheritage' | 'chalps';
  label: string;
  subtitle: string;
  accent: string;
};

const COLLECTIONS: CityCollection[] = [
  {
    slug: 'chpremium',
    label: 'Premium Cities',
    subtitle: 'Zurich, Geneva, Basel, Bern, Lausanne and more.',
    accent: 'from-[#DA291C] via-[#FFFFFF] to-[#111111]',
  },
  {
    slug: 'chheritage',
    label: 'Heritage Cities',
    subtitle: 'Fribourg, Sion, Neuchatel, Schaffhausen, Solothurn and more.',
    accent: 'from-[#8B0000] via-[#CC0000] to-[#FFFFFF]',
  },
  {
    slug: 'chalps',
    label: 'Alps & Lakes',
    subtitle: 'Interlaken, Davos, Zermatt, Montreux, Grindelwald and more.',
    accent: 'from-[#123B66] via-[#D62828] to-[#F1FAEE]',
  },
];

export default function SuisseCityPage() {
  const { products, loading, error, params, setParams, total, categories, colors } = useProducts({
    pageSize: 12,
    page: 1,
    category: 'chpremium',
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
    <main className="min-h-screen bg-[#070707] px-6 py-20 text-white md:px-16">
      <section className="mx-auto max-w-6xl">
        <div className="rounded-3xl border border-white/15 bg-[#0D0D0D] p-8 shadow-[0_30px_80px_rgba(0,0,0,0.45)]">
          <p className="text-xs font-bold uppercase tracking-[0.3em] text-[#D62828]">Dope&Cute Studio</p>
          <h1 className="mt-3 text-4xl font-black md:text-5xl">Suisse City Collection</h1>
          <p className="mt-3 max-w-3xl text-sm text-white/70 md:text-base">
            Casquettes inspirees des villes suisses. Broderie du nom de ville,
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
                      ? 'bg-[#D62828] text-white'
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
            <p className="text-xs font-bold uppercase tracking-[0.24em] text-black/80">Collection active</p>
            <h2 className="mt-2 text-2xl font-black text-black">{activeCollection.label}</h2>
            <p className="mt-1 text-sm text-black/80">{activeCollection.subtitle}</p>
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
