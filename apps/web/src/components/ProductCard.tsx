import { useCart } from '@/hooks/useCart';
import Image from 'next/image';
import { Product } from '@/features/catalog/types/catalog.types';

type Props = {
  product: Product;
};

export default function ProductCard({ product }: Props) {
  const { addItem, showToast } = useCart();
  const image = product.images[0];

  const handleAddToCart = () => {
    addItem({
      id: `${product.id}-product`,
      name: product.name,
      kind: 'product',
      price: product.price,
      quantity: 1,
      description: product.category.name,
      image: image?.url,
      metadata: {
        category: product.category.name,
        sku: product.sku,
      },
    });
    showToast(`${product.name} ajouté au panier`);
  };

  return (
    <div className="overflow-hidden rounded-3xl border border-white/10 bg-white/5 transition hover:-translate-y-2">
      <div className="relative h-72 overflow-hidden bg-neutral-950">
        {image ? (
          <Image
            src={image.url}
            alt={image.alt}
            fill
            sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 33vw"
            className="object-cover"
            loading="lazy"
          />
        ) : (
          <div className="flex h-full items-center justify-center bg-gradient-to-br from-neutral-900 to-neutral-700">
            <span className="text-6xl font-black text-white/20">D&C</span>
          </div>
        )}
      </div>

      <div className="p-6">
        {product.compareAtPrice && (
          <span className="rounded-full bg-[#C8A45C] px-3 py-1 text-xs font-bold text-black">
            -{Math.round((1 - product.price / product.compareAtPrice) * 100)}%
          </span>
        )}

        <h3 className="mt-4 text-2xl font-bold text-white">{product.name}</h3>
        <p className="mt-2 text-white/60">{product.category.name}</p>
        <p className="mt-5 text-3xl font-black text-[#C8A45C]">{product.price.toFixed(2)} €</p>
        <button
          type="button"
          onClick={handleAddToCart}
          className="mt-6 w-full rounded-full bg-white py-3 font-bold text-black transition hover:bg-[#C8A45C]"
        >
          Ajouter au panier
        </button>
      </div>
    </div>
  );
}
