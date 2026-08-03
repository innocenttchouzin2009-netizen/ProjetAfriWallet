'use client';

import { useMemo } from 'react';
import Link from 'next/link';
import ProductFilters from '@/components/ProductFilters';
import SectionTitle from '@/components/SectionTitle';
import ProductGrid from '@/features/catalog/components/ProductGrid';
import { useProducts } from '@/features/catalog/hooks/useProducts';

type CityCollection = {
  slug: 'premium' | 'regional' | 'nrw';
  label: string;
  subtitle: string;
  accent: string;
};

const COLLECTIONS: CityCollection[] = [
  {
    slug: 'premium',
    label: 'Premium Cities',
    subtitle: 'Berlin, Hamburg, Munich, Cologne, Frankfurt and more.',
    accent: 'from-[#2A2A72] via-[#009FFD] to-[#2A2A72]',
  },
  {
    slug: 'regional',
    label: 'Regional Cities',
    subtitle: 'Bremen, Hanover, Heidelberg, Freiburg, Mainz and more.',
    accent: 'from-[#1B4332] via-[#2D6A4F] to-[#1B4332]',
  },
  {
    slug: 'nrw',
    label: 'NRW Cities',
    subtitle: 'Solingen, Wuppertal, Essen, Dortmund, Duisburg and more.',
    accent: 'from-[#3A0CA3] via-[#7209B7] to-[#3A0CA3]',
  },
];

const SLICE_IMAGE = 'https://images.openai.com/static-rsc-4/BhZjJTQHdgoOiiQXbF3q7pMZ48wKJRsY8CQgU7R13Wv5kiD1y18Muwfx_kuh2Zh2XjoeAv5Exik70PLlfgLaeIX39RxGuI6rIy1h36MTrgEsNE_MVhLMnkGCIGEDUAo20zi98z3yuWdCU36gG5Go0_IaFIKwdcFh45QHWhrNhj9l_UbmuEbKTkeYYP5DzY4x?purpose=fullsize';

export default function GermanyCityPage() {
  const { products, loading, error, params, setParams, total, categories, colors } = useProducts({
    pageSize: 12,
    page: 1,
    category: 'premium',
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
    <main className="min-h-screen bg-[#080808] px-6 py-20 text-white md:px-16">
      <section className="mx-auto max-w-6xl">
        <div className="rounded-3xl border border-white/15 bg-[#0F0F0F] p-8 shadow-[0_30px_80px_rgba(0,0,0,0.45)]">
          <p className="text-xs font-bold uppercase tracking-[0.3em] text-[#C8A45C]">Dope&Cute Studio</p>
          <h1 className="mt-3 text-4xl font-black md:text-5xl">Germany City Collection</h1>
          <p className="mt-3 max-w-3xl text-sm text-white/70 md:text-base">
            Casquettes inspirees des grandes villes allemandes. Broderie du nom de ville,
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
                      ? 'bg-[#C8A45C] text-black'
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
            {[
              { label: 'Berlin slice', position: 'center top' },
              { label: 'Hamburg slice', position: 'center 28%' },
              { label: 'Munich slice', position: 'center 56%' },
              { label: 'Cologne slice', position: 'center bottom' },
            ].map((slice, index) => (
              <div key={slice.label} className="relative h-56 overflow-hidden border-b border-white/10 md:h-72 md:border-b-0 md:border-r md:border-r-white/10 last:border-r-0">
                <div
                  className="absolute inset-0 bg-cover bg-center"
                  style={{
                    backgroundImage: `url(${SLICE_IMAGE})`,
                    backgroundPosition: slice.position,
                  }}
                />
                <div className="absolute inset-0 bg-gradient-to-t from-black/75 via-black/20 to-transparent" />
                <div className="absolute inset-x-0 bottom-0 p-4">
                  <p className="text-xs font-bold uppercase tracking-[0.24em] text-white/70">Slice {index + 1}</p>
                  <h3 className="mt-1 text-xl font-black">{slice.label}</h3>
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
