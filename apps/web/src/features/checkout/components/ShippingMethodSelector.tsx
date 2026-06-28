import type { ShippingMethod } from '../types/checkout.types';

type Props = {
  methods: ShippingMethod[];
  selectedId: string;
  onSelect: (id: string) => void;
};

export default function ShippingMethodSelector({ methods, selectedId, onSelect }: Props) {
  return (
    <div className="mt-6 space-y-3">
      {methods.map((method) => {
        const isSelected = method.id === selectedId;

        return (
          <button
            key={method.id}
            type="button"
            onClick={() => onSelect(method.id)}
            className={`w-full rounded-[24px] border px-5 py-4 text-left ${
              isSelected
                ? 'border-[#C8A45C] bg-[#C8A45C]/10'
                : 'border-white/10 bg-black/20'
            }`}
          >
            <div className="flex items-center justify-between gap-4">
              <div>
                <p className="font-semibold text-white">{method.label}</p>
                <p className="mt-1 text-sm text-white/60">{method.description}</p>
              </div>
              <div className="text-right">
                <p className="font-semibold text-[#C8A45C]">{method.price.toFixed(2)} €</p>
                <p className="mt-1 text-xs uppercase tracking-[0.2em] text-white/50">{method.eta}</p>
              </div>
            </div>
          </button>
        );
      })}
    </div>
  );
}
