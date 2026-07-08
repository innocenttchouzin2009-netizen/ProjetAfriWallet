'use client';

import dynamic from 'next/dynamic';
import Link from 'next/link';
import CategoryCard from '@/components/CategoryCard';
import ProductFilters from '@/components/ProductFilters';
import SectionTitle from '@/components/SectionTitle';
import ShoppingCart from '@/components/ShoppingCart';
import Hero from '@/components/Hero';
import Footer from '@/components/Footer';
import { useProducts } from '@/features/catalog/hooks/useProducts';

const ProductGrid = dynamic(() => import('@/features/catalog/components/ProductGrid'));

export default function ShopPage() {
  const { products, loading, error, params, setParams, total, categories, colors } = useProducts({ pageSize: 12, page: 1 });
  const currentPage = params.page ?? 1;
  const pageSize = params.pageSize ?? 12;
  const pageCount = Math.max(1, Math.ceil(total / pageSize));

  const applyCollection = (category?: string) => {
    setParams({
      ...params,
      category,
      query: category ? 'City Cap' : params.query,
      page: 1,
    });
  };

  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-24 text-white md:px-16">
      <Hero />

      <section className="mx-auto mt-20 max-w-6xl">
        <SectionTitle title="Boutique" subtitle="Collections premium Dope&Cute Studio" />

        <div className="grid gap-6 lg:grid-cols-[1.2fr_0.8fr]">
          <div className="space-y-6">
            <div className="grid gap-6 md:grid-cols-3">
              <CategoryCard title="Baseball Cap" />
              <CategoryCard title="Snapback" />
              <CategoryCard title="Trucker" />
            </div>

            <div className="rounded-3xl border border-[#C8A45C]/30 bg-gradient-to-br from-[#20170A] via-[#111111] to-[#0D0D0D] p-6 text-white">
              <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                <div>
                  <p className="text-xs font-bold uppercase tracking-[0.25em] text-[#C8A45C]">Dope&Cute Studio</p>
                  <h3 className="mt-2 text-2xl font-black">Germany City Collection</h3>
                  <p className="mt-2 text-sm text-white/75">
                    Casquettes brodees inspirees des villes allemandes: Premium, Regionale et NRW.
                  </p>
                </div>
                <div className="grid gap-2 sm:grid-cols-2">
                  <Link
                    href="/shop/germany-city"
                    className="rounded-full border border-[#C8A45C] bg-[#C8A45C] px-4 py-2 text-center text-sm font-bold text-black transition hover:opacity-90 sm:col-span-2"
                  >
                    Page complete Germany City
                  </Link>
                  <button
                    type="button"
                    onClick={() => applyCollection('premium')}
                    className="rounded-full border border-white/20 bg-white/5 px-4 py-2 text-sm font-semibold transition hover:bg-white/10"
                  >
                    Premium Cities
                  </button>
                  <button
                    type="button"
                    onClick={() => applyCollection('regional')}
                    className="rounded-full border border-white/20 bg-white/5 px-4 py-2 text-sm font-semibold transition hover:bg-white/10"
                  >
                    Regional Cities
                  </button>
                  <button
                    type="button"
                    onClick={() => applyCollection('nrw')}
                    className="rounded-full border border-white/20 bg-white/5 px-4 py-2 text-sm font-semibold transition hover:bg-white/10"
                  >
                    NRW Cities
                  </button>
                  <button
                    type="button"
                    onClick={() => applyCollection(undefined)}
                    className="rounded-full border border-[#C8A45C] bg-[#C8A45C] px-4 py-2 text-sm font-bold text-black transition hover:opacity-90"
                  >
                    Voir toute la boutique
                  </button>
                </div>
              </div>
            </div>

            <div className="rounded-3xl border border-[#5FA8FF]/30 bg-gradient-to-br from-[#091429] via-[#111111] to-[#1B1A32] p-6 text-white">
              <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                <div>
                  <p className="text-xs font-bold uppercase tracking-[0.25em] text-[#8EC5FF]">Dope&Cute Studio</p>
                  <h3 className="mt-2 text-2xl font-black">France City Collection</h3>
                  <p className="mt-2 text-sm text-white/75">
                    Casquettes inspirees des villes francaises: Premium, Heritage et Riviera-Alpes.
                  </p>
                </div>
                <div className="grid gap-2 sm:grid-cols-2">
                  <Link
                    href="/shop/france-city"
                    className="rounded-full border border-[#8EC5FF] bg-[#8EC5FF] px-4 py-2 text-center text-sm font-bold text-black transition hover:opacity-90 sm:col-span-2"
                  >
                    Page complete France City
                  </Link>
                  <button
                    type="button"
                    onClick={() => applyCollection('frpremium')}
                    className="rounded-full border border-white/20 bg-white/5 px-4 py-2 text-sm font-semibold transition hover:bg-white/10"
                  >
                    Premium Cities
                  </button>
                  <button
                    type="button"
                    onClick={() => applyCollection('frheritage')}
                    className="rounded-full border border-white/20 bg-white/5 px-4 py-2 text-sm font-semibold transition hover:bg-white/10"
                  >
                    Heritage Cities
                  </button>
                  <button
                    type="button"
                    onClick={() => applyCollection('frriviera')}
                    className="rounded-full border border-white/20 bg-white/5 px-4 py-2 text-sm font-semibold transition hover:bg-white/10"
                  >
                    Riviera-Alpes
                  </button>
                  <button
                    type="button"
                    onClick={() => applyCollection(undefined)}
                    className="rounded-full border border-[#8EC5FF] bg-[#8EC5FF] px-4 py-2 text-sm font-bold text-black transition hover:opacity-90"
                  >
                    Voir toute la boutique
                  </button>
                </div>
              </div>
            </div>

            <div className="rounded-3xl border border-[#FFD166]/35 bg-gradient-to-br from-[#19120A] via-[#101010] to-[#2A1A1A] p-6 text-white">
              <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                <div>
                  <p className="text-xs font-bold uppercase tracking-[0.25em] text-[#FFD166]">Dope&Cute Studio</p>
                  <h3 className="mt-2 text-2xl font-black">Belgique City Collection</h3>
                  <p className="mt-2 text-sm text-white/75">
                    Casquettes inspirees des villes belges: Premium, Heritage et Ardennes-Cote.
                  </p>
                </div>
                <div className="grid gap-2 sm:grid-cols-2">
                  <Link
                    href="/shop/belgique-city"
                    className="rounded-full border border-[#FFD166] bg-[#FFD166] px-4 py-2 text-center text-sm font-bold text-black transition hover:opacity-90 sm:col-span-2"
                  >
                    Page complete Belgique City
                  </Link>
                  <button
                    type="button"
                    onClick={() => applyCollection('bepremium')}
                    className="rounded-full border border-white/20 bg-white/5 px-4 py-2 text-sm font-semibold transition hover:bg-white/10"
                  >
                    Premium Cities
                  </button>
                  <button
                    type="button"
                    onClick={() => applyCollection('beheritage')}
                    className="rounded-full border border-white/20 bg-white/5 px-4 py-2 text-sm font-semibold transition hover:bg-white/10"
                  >
                    Heritage Cities
                  </button>
                  <button
                    type="button"
                    onClick={() => applyCollection('beardennes')}
                    className="rounded-full border border-white/20 bg-white/5 px-4 py-2 text-sm font-semibold transition hover:bg-white/10"
                  >
                    Ardennes-Cote
                  </button>
                  <button
                    type="button"
                    onClick={() => applyCollection(undefined)}
                    className="rounded-full border border-[#FFD166] bg-[#FFD166] px-4 py-2 text-sm font-bold text-black transition hover:opacity-90"
                  >
                    Voir toute la boutique
                  </button>
                </div>
              </div>
            </div>

            <div className="rounded-3xl border border-[#D62828]/35 bg-gradient-to-br from-[#121212] via-[#111111] to-[#1B1B1B] p-6 text-white">
              <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                <div>
                  <p className="text-xs font-bold uppercase tracking-[0.25em] text-[#F1FAEE]">Dope&Cute Studio</p>
                  <h3 className="mt-2 text-2xl font-black">Suisse City Collection</h3>
                  <p className="mt-2 text-sm text-white/75">
                    Casquettes inspirees des villes suisses: Premium, Heritage et Alps & Lakes.
                  </p>
                </div>
                <div className="grid gap-2 sm:grid-cols-2">
                  <Link
                    href="/shop/suisse-city"
                    className="rounded-full border border-[#D62828] bg-[#D62828] px-4 py-2 text-center text-sm font-bold text-white transition hover:opacity-90 sm:col-span-2"
                  >
                    Page complete Suisse City
                  </Link>
                  <button
                    type="button"
                    onClick={() => applyCollection('chpremium')}
                    className="rounded-full border border-white/20 bg-white/5 px-4 py-2 text-sm font-semibold transition hover:bg-white/10"
                  >
                    Premium Cities
                  </button>
                  <button
                    type="button"
                    onClick={() => applyCollection('chheritage')}
                    className="rounded-full border border-white/20 bg-white/5 px-4 py-2 text-sm font-semibold transition hover:bg-white/10"
                  >
                    Heritage Cities
                  </button>
                  <button
                    type="button"
                    onClick={() => applyCollection('chalps')}
                    className="rounded-full border border-white/20 bg-white/5 px-4 py-2 text-sm font-semibold transition hover:bg-white/10"
                  >
                    Alps & Lakes
                  </button>
                  <button
                    type="button"
                    onClick={() => applyCollection(undefined)}
                    className="rounded-full border border-[#D62828] bg-[#D62828] px-4 py-2 text-sm font-bold text-white transition hover:opacity-90"
                  >
                    Voir toute la boutique
                  </button>
                </div>
              </div>
            </div>

            <div className="grid gap-6 lg:grid-cols-[2fr_1fr]">
              <div className="space-y-6">
                <SectionTitle title="Nos meilleurs produits" />
                <ProductGrid products={products} loading={loading} error={error} />
                <div className="flex flex-col gap-4 rounded-3xl border border-white/10 bg-white/5 p-6 text-white sm:flex-row sm:items-center sm:justify-between">
                  <div className="space-y-1">
                    <div>{total} produits trouvés</div>
                    <div className="text-sm text-white/70">Page {currentPage} sur {pageCount}</div>
                  </div>

                  <div className="flex flex-wrap items-center gap-3">
                    <button
                      type="button"
                      className="rounded-full border border-white/10 px-4 py-2 text-sm transition hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-40"
                      onClick={() => setParams({ ...params, page: Math.max(1, currentPage - 1) })}
                      disabled={currentPage <= 1}
                    >
                      Précédent
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
                onReset={() => setParams({ page: 1, pageSize: 12 })}
              />
            </div>
          </div>

          <ShoppingCart />
        </div>
      </section>

      <Footer />
    </main>
  );
}
