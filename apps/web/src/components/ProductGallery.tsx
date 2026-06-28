import { Product } from '@/features/catalog/types/catalog.types';
import ProductCard from './ProductCard';

type ProductGalleryProps = {
  products: Product[];
};

export default function ProductGallery({ products }: ProductGalleryProps) {
  return (
    <section className="grid gap-6 md:grid-cols-2 xl:grid-cols-3">
      {products.map((product) => (
        <ProductCard key={product.id} product={product} />
      ))}
    </section>
  );
}
