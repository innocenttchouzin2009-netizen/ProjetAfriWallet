import { useCart } from '@/hooks/useCart';
import Input from './ui/Input';
import Select from './ui/Select';

export default function ProductConfigurator() {
  const { addItem, showToast } = useCart();

  const handleAddToCart = () => {
    addItem({
      id: 'configurator-custom-cap',
      name: 'Casquette personnalisée',
      kind: 'product',
      price: 89,
      quantity: 1,
      description: 'Configurateur personnalisé',
    });
    showToast('Casquette personnalisée ajoutée au panier');
  };

  return (
    <section className="space-y-6 rounded-[32px] border border-white/10 bg-white/5 p-8">
      <h2 className="text-2xl font-black">Configurateur</h2>
      <div className="grid gap-4">
        <Input placeholder="Modèle" />
        <Input placeholder="Couleur" />
        <Input placeholder="Texte / Prénom" />
        <Select
          options={[
            { value: 'logo', label: 'Logo' },
            { value: 'embroidery', label: 'Broderie' },
          ]}
        />
        <Input placeholder="Emplacement" />
      </div>
      <button
        type="button"
        onClick={handleAddToCart}
        className="w-full rounded-full bg-[#C8A45C] px-6 py-4 font-bold text-black"
      >
        Ajouter au panier
      </button>
    </section>
  );
}
