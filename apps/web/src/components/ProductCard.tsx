import { useState, type ChangeEvent } from 'react';
import { useCart } from '@/hooks/useCart';
import Image from 'next/image';
import { Product } from '@/features/catalog/types/catalog.types';
import Modal from '@/components/ui/Modal';

type Props = {
  product: Product;
};

export default function ProductCard({ product }: Props) {
  const { addItem, showToast } = useCart();
  const image = product.images[0];
  const [personalizeOpen, setPersonalizeOpen] = useState(false);
  const [customInitials, setCustomInitials] = useState('');
  const [customLogoUrl, setCustomLogoUrl] = useState('');
  const [uploadLabel, setUploadLabel] = useState('');

  const normalizeInitials = (value: string) => value.trim().slice(0, 5).toUpperCase();

  const buildCartItemId = (initials?: string, logoUrl?: string) => {
    const initialsPart = initials ? `init-${initials}` : 'init-none';
    const logoPart = logoUrl ? `logo-${logoUrl.slice(0, 24)}` : 'logo-none';
    return `${product.id}-product-${initialsPart}-${logoPart}`;
  };

  const handleAddToCart = (options?: { initials?: string; logoUrl?: string }) => {
    const initials = options?.initials ? normalizeInitials(options.initials) : undefined;
    const logoUrl = options?.logoUrl?.trim() ? options.logoUrl.trim() : undefined;

    addItem({
      id: buildCartItemId(initials, logoUrl),
      name: product.name,
      kind: 'product',
      price: product.price,
      quantity: 1,
      description: product.category.name,
      image: image?.url,
      metadata: {
        category: product.category.name,
        sku: product.sku,
        customInitials: initials,
        customLogoUrl: logoUrl,
      },
    });

    const customizationLabel = initials || logoUrl ? ' avec personnalisation' : '';
    showToast(`${product.name} ajoute au panier${customizationLabel}`);
  };

  const handleLogoFileChange = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setUploadLabel(file.name);

    const dataUrl = await new Promise<string>((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(typeof reader.result === 'string' ? reader.result : '');
      reader.onerror = () => reject(new Error('Impossible de lire le fichier logo.'));
      reader.readAsDataURL(file);
    });

    if (dataUrl) {
      setCustomLogoUrl(dataUrl);
    }
  };

  const handleAddPersonalized = () => {
    handleAddToCart({ initials: customInitials, logoUrl: customLogoUrl });
    setPersonalizeOpen(false);
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
          onClick={() => handleAddToCart()}
          className="mt-6 w-full rounded-full bg-white py-3 font-bold text-black transition hover:bg-[#C8A45C]"
        >
          Ajouter au panier
        </button>
        <button
          type="button"
          onClick={() => setPersonalizeOpen(true)}
          className="mt-3 w-full rounded-full border border-white/20 py-3 text-sm font-semibold text-white transition hover:bg-white/10"
        >
          Ajouter avec initiales/logo
        </button>
      </div>

      <Modal title={`Personnaliser - ${product.name}`} open={personalizeOpen} onClose={() => setPersonalizeOpen(false)}>
        <div className="space-y-4 text-sm text-white/80">
          <p>Ajoute les initiales du client ou son propre logo avant d&apos;ajouter l&apos;article au panier.</p>

          <div className="space-y-2">
            <label htmlFor={`initials-${product.id}`} className="block font-semibold text-white">Initiales (max 5)</label>
            <input
              id={`initials-${product.id}`}
              type="text"
              value={customInitials}
              onChange={(event) => setCustomInitials(normalizeInitials(event.target.value))}
              placeholder="Ex: DC"
              maxLength={5}
              className="w-full rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-white"
            />
          </div>

          <div className="space-y-2">
            <label htmlFor={`logo-url-${product.id}`} className="block font-semibold text-white">Lien logo (optionnel)</label>
            <input
              id={`logo-url-${product.id}`}
              type="url"
              value={customLogoUrl.startsWith('data:') ? '' : customLogoUrl}
              onChange={(event) => setCustomLogoUrl(event.target.value)}
              placeholder="https://..."
              className="w-full rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-white"
            />
          </div>

          <div className="space-y-2">
            <label htmlFor={`logo-file-${product.id}`} className="block font-semibold text-white">Ou importer le logo</label>
            <input
              id={`logo-file-${product.id}`}
              type="file"
              accept="image/*"
              onChange={(event) => {
                void handleLogoFileChange(event);
              }}
              className="w-full rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-white"
            />
            {uploadLabel ? <p className="text-xs text-white/60">Fichier: {uploadLabel}</p> : null}
          </div>

          <button
            type="button"
            onClick={handleAddPersonalized}
            className="w-full rounded-full bg-[#C8A45C] px-6 py-3 font-bold text-black"
          >
            Ajouter personnalise au panier
          </button>
        </div>
      </Modal>
    </div>
  );
}
