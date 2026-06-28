"use client";

import { posProducts } from '../data/pos.data';
import type { POSProduct } from '../types/pos.types';

interface POSProductGridProps {
  onAdd: (productId: string) => void;
}

export default function POSProductGrid({ onAdd }: POSProductGridProps) {
  return (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
      {posProducts.map((product) => (
        <button
          key={product.id}
          onClick={() => onAdd(product.id)}
          className="rounded-[24px] border border-white/10 bg-white/5 p-5 text-left transition hover:bg-white/10"
        >
          <div className="flex items-center justify-between">
            <p className="text-lg font-semibold text-white">{product.name}</p>
            <span className="rounded-full border border-[#C8A45C]/40 bg-[#C8A45C]/10 px-3 py-1 text-sm text-[#F5E0AC]">
              {product.stock} en stock
            </span>
          </div>
          <p className="mt-3 text-sm text-white/60">{product.category}</p>
          <div className="mt-4 flex items-center justify-between">
            <p className="text-xl font-black text-white">{product.price.toFixed(2)} €</p>
            <p className="text-sm text-white/50">{product.sku}</p>
          </div>
        </button>
      ))}
    </div>
  );
}
