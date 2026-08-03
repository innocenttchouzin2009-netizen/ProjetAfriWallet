'use client';

import { useMemo } from 'react';
import Link from 'next/link';
import ProductFilters from '@/components/ProductFilters';
import SectionTitle from '@/components/SectionTitle';
import ProductGrid from '@/features/catalog/components/ProductGrid';
import { useProducts } from '@/features/catalog/hooks/useProducts';

type CityCollection = {
  slug: 'espremium' | 'esheritage' | 'escoast';
  label: string;
  subtitle: string;
  accent: string;
};

const COLLECTIONS: CityCollection[] = [
  {
    slug: 'espremium',
    label: 'Premium Cities',
    subtitle: 'Madrid, Barcelona, Valencia, Seville, Bilbao and more.',
    accent: 'from-[#AA151B] via-[#F1BF00] to-[#AA151B]',
  },
  {
    slug: 'esheritage',
    label: 'Heritage Cities',
    subtitle: 'Granada, Toledo, Salamanca, Segovia, Santiago and more.',
    accent: 'from-[#8B4513] via-[#C19A6B] to-[#FFF3E0]',
  },
  {
    slug: 'escoast',
    label: 'Coast & Islands',
    subtitle: 'San Sebastian, Cadiz, Marbella, Ibiza, A Coruna and more.',
    accent: 'from-[#0038A8] via-[#FFB81C] to-[#FFFFFF]',
  },
];

const PHOTO_SLICES = [
  { title: 'Madrid', subtitle: 'Puerta de Alcala and Gran Via', start: '#AA151B', end: '#F1BF00' },
  { title: 'Barcelona', subtitle: 'Sagrada Familia and Eixample', start: '#0038A8', end: '#AA151B' },
  { title: 'Seville', subtitle: 'Giralda tower and old river', start: '#8B4513', end: '#C19A6B' },
  { title: 'Valencia', subtitle: 'City of Arts and Sciences', start: '#0038A8', end: '#FFB81C' },
] as const;

const buildSliceDataUrl = (title: string, subtitle: string, start: string, end: string) => {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="900" viewBox="0 0 1200 900">
  <defs>
    <linearGradient id="g" x1="0" x2="1" y1="0" y2="1">
      <stop offset="0%" stop-color="${start}" />
      <stop offset="100%" stop-color="${end}" />
    </linearGradient>
    <filter id="blur"><feGaussianBlur stdDeviation="28" /></filter>
  </defs>
  <rect width="1200" height="900" fill="url(#g)" />
  <circle cx="230" cy="170" r="120" fill="rgba(255,255,255,0.18)" filter="url(#blur)" />
  <circle cx="930" cy="220" r="180" fill="rgba(255,255,255,0.12)" filter="url(#blur)" />
  <rect y="660" width="1200" height="240" fill="rgba(0,0,0,0.18)" />
  <text x="70" y="200" fill="#ffffff" font-family="Arial, sans-serif" font-size="86" font-weight="800">${title}</text>
  <text x="70" y="282" fill="rgba(255,255,255,0.88)" font-family="Arial, sans-serif" font-size="34" font-weight="600">${subtitle}</text>
  <text x="70" y="790" fill="rgba(255,255,255,0.78)" font-family="Arial, sans-serif" font-size="28" font-weight="700">Spain City Collection</text>
</svg>`;

  return `data:image/svg+xml;charset=UTF-8,${encodeURIComponent(svg)}`;
};

export default function SpainCityPage() {
  const { products, loading, error, params, setParams, total, categories, colors } = useProducts({
    pageSize: 12,
    page: 1,
    category: 'espremium',
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
    <main className="min-h-screen bg-[#090A0F] px-6 py-20 text-white md:px-16">
      <section className="mx-auto max-w-6xl">
        <div className="rounded-3xl border border-white/15 bg-[#111217] p-8 shadow-[0_30px_80px_rgba(0,0,0,0.45)]">
          <p className="text-xs font-bold uppercase tracking-[0.3em] text-[#AA151B]">Dope&Cute Studio</p>
          <h1 className="mt-3 text-4xl font-black md:text-5xl">Spain City Collection</h1>
          <p className="mt-3 max-w-3xl text-sm text-white/70 md:text-base">
            Casquettes inspirees des villes espagnoles. Broderie du nom de ville,
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
                      ? 'bg-[#AA151B] text-white'
                      : 'border border-white/20 bg-white/5 text-white hover:bg-white/10',
                  ].join(' ')}
                >
                  {collection.label}
                </button>
              );
            })}
            <Link href="/shop" className="rounded-full border border-white/20 bg-transparent px-5 py-2 text-sm font-semibold text-white transition hover:bg-white/10">
              Retour boutique
            </Link>
          </div>

          <div className={`mt-6 rounded-2xl bg-gradient-to-r ${activeCollection.accent} p-5`}>
            <p className="text-xs font-bold uppercase tracking-[0.24em] text-black/80">Collection active</p>
            <h2 className="mt-2 text-2xl font-black text-black">{activeCollection.label}</h2>
            <p className="mt-1 text-sm text-black/80">{activeCollection.subtitle}</p>
          </div>
        </div>

        <div className="mt-6 overflow-hidden rounded-3xl border border-white/10 bg-white/5">
          <div className="grid gap-0 md:grid-cols-4">
            {PHOTO_SLICES.map((slice, index) => (
              <div key={slice.title} className="relative h-56 overflow-hidden border-b border-white/10 md:h-72 md:border-b-0 md:border-r md:border-r-white/10 last:border-r-0">
                <div className="absolute inset-0 bg-cover bg-center" style={{ backgroundImage: `url(${buildSliceDataUrl(slice.title, slice.subtitle, slice.start, slice.end)})` }} />
                <div className="absolute inset-0 bg-gradient-to-t from-black/85 via-black/20 to-transparent" />
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
                <button type="button" className="rounded-full border border-white/10 px-4 py-2 text-sm transition hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-40" onClick={() => setParams({ ...params, page: Math.max(1, currentPage - 1) })} disabled={currentPage <= 1}>
                  Precedent
                </button>
                <button type="button" className="rounded-full border border-white/10 px-4 py-2 text-sm transition hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-40" onClick={() => setParams({ ...params, page: Math.min(pageCount, currentPage + 1) })} disabled={currentPage >= pageCount}>
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
