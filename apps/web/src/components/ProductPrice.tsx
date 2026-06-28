type ProductPriceProps = {
  price: number;
  badge?: string;
};

export default function ProductPrice({ price, badge }: ProductPriceProps) {
  return (
    <div className="flex flex-col gap-2">
      {badge ? <span className="rounded-full bg-[#C8A45C] px-3 py-1 text-xs font-bold text-black">{badge}</span> : null}
      <span className="text-4xl font-black text-[#C8A45C]">{price.toFixed(2)} €</span>
      <p className="text-sm text-white/70">Livraison rapide et emballage premium.</p>
    </div>
  );
}
