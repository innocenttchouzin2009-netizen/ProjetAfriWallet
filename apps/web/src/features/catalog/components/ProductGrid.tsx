'use client';

import { Product } from '@/features/catalog/types/catalog.types';
import ProductCard from '@/components/ProductCard';

type ProductGridProps = {
  products: Product[];
  loading: boolean;
  error: string | null;
};

export default function ProductGrid({ products, loading, error }: ProductGridProps) {
  if (loading) {
    return <div className="py-16 text-center text-white/70">Chargement des produits...</div>;
  }

  if (error) {
    return <div className="py-16 text-center text-red-400">Erreur : {error}</div>;
  }

  if (products.length === 0) {
    return <div className="py-16 text-center text-white/70">Aucun produit trouvé.</div>;
  }

  return (
    <div className="grid gap-6 sm:grid-cols-2 xl:grid-cols-3">
      {products.map((product) => (
        <ProductCard key={product.id} product={product} />
      ))}
    </div>
  );
}
