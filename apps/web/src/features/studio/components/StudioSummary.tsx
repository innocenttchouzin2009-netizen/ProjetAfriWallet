import { useCart } from '@/hooks/useCart';
import { StudioDesign } from "../types/studio.types";

type Props = {
  design: StudioDesign;
  totalPrice: number;
  resetDesign: () => void;
  saveDesign: () => void;
};

export default function StudioSummary({
  design,
  totalPrice,
  resetDesign,
  saveDesign,
}: Props) {
  const { addItem, showToast } = useCart();

  const handleAddToCart = () => {
    addItem({
      id: `studio-${design.productName}-${design.color}`,
      name: `${design.productName} • ${design.color}`,
      kind: 'studio-design',
      price: totalPrice,
      quantity: design.quantity,
      description: `${design.placement} • ${design.embroideryType}`,
      metadata: {
        placement: design.placement,
        embroideryType: design.embroideryType,
        quantity: design.quantity,
      },
    });
    showToast('Design Studio ajouté au panier');
  };

  return (
    <div className="rounded-3xl bg-black p-8 text-white">
      <h2 className="text-2xl font-black">Résumé</h2>
      <div className="mt-6 space-y-3 text-white/70">
        <p>Produit : {design.productName}</p>
        <p>Couleur : {design.color}</p>
        <p>Placement : {design.placement}</p>
        <p>Broderie : {design.embroideryType}</p>
        <p>Quantité : {design.quantity}</p>
      </div>
      <p className="mt-8 text-4xl font-black text-[#C8A45C]">
        {totalPrice.toFixed(2)} €
      </p>
      <button
        type="button"
        onClick={handleAddToCart}
        className="mt-6 w-full rounded-full bg-[#C8A45C] px-6 py-4 font-bold text-black"
      >
        Ajouter au panier
      </button>
      <button
        type="button"
        onClick={saveDesign}
        className="mt-4 w-full rounded-full border border-white/20 bg-white/5 px-6 py-4 text-white"
      >
        Sauvegarder mon design
      </button>
      <button
        type="button"
        onClick={resetDesign}
        className="mt-4 w-full rounded-full border border-white/20 px-6 py-4"
      >
        Réinitialiser
      </button>
    </div>
  );
}
