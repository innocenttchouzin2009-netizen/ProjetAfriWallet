"use client";

import { useState } from 'react';
import type { InventoryItem } from '@/features/inventory/types/inventory.types';

type Props = {
  items: InventoryItem[];
  adjustingVariantId: string | null;
  onAdjust: (variantId: string, quantityDelta: number, reason?: string) => Promise<void> | void;
};

export default function AdminInventoryTable({ items, adjustingVariantId, onAdjust }: Props) {
  const [deltas, setDeltas] = useState<Record<string, number>>({});
  const [reasons, setReasons] = useState<Record<string, string>>({});

  return (
    <section className="overflow-hidden rounded-[28px] border border-white/10 bg-white/5">
      <table className="min-w-full text-left text-sm text-white/80">
        <thead className="bg-black/20 text-white/60">
          <tr>
            <th className="px-4 py-3">Produit / SKU</th>
            <th className="px-4 py-3">Stock</th>
            <th className="px-4 py-3">Alerte</th>
            <th className="px-4 py-3">Ajustement manuel</th>
            <th className="px-4 py-3">Historique (recent)</th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => {
            const delta = deltas[item.variantId] ?? 0;
            const reason = reasons[item.variantId] ?? '';

            return (
              <tr key={item.variantId} className="border-t border-white/10 align-top">
                <td className="px-4 py-4">
                  <p className="font-semibold text-white">{item.productName}</p>
                  <p className="text-xs text-white/55">{item.variantName}</p>
                  <p className="mt-1 text-xs text-[#C8A45C]">{item.sku}</p>
                </td>
                <td className="px-4 py-4">
                  <p className="text-lg font-black text-white">{item.stock}</p>
                  <p className="text-xs text-white/50">Seuil: {item.lowStockThreshold}</p>
                </td>
                <td className="px-4 py-4">
                  <span
                    className={`rounded-full px-3 py-1 text-xs ${
                      item.lowStockAlert ? 'bg-[#F0B86E]/20 text-[#F0B86E]' : 'bg-[#8ED8A2]/20 text-[#8ED8A2]'
                    }`}
                  >
                    {item.lowStockAlert ? 'Stock faible' : 'OK'}
                  </span>
                </td>
                <td className="px-4 py-4">
                  <div className="flex flex-col gap-2">
                    <input
                      type="number"
                      value={delta}
                      onChange={(event) =>
                        setDeltas((current) => ({
                          ...current,
                          [item.variantId]: Number(event.target.value),
                        }))
                      }
                      className="w-28 rounded-full border border-white/10 bg-black/30 px-3 py-2 text-sm text-white"
                    />
                    <input
                      type="text"
                      value={reason}
                      onChange={(event) =>
                        setReasons((current) => ({
                          ...current,
                          [item.variantId]: event.target.value,
                        }))
                      }
                      placeholder="Raison"
                      className="w-44 rounded-full border border-white/10 bg-black/30 px-3 py-2 text-xs text-white"
                    />
                    <button
                      onClick={() => onAdjust(item.variantId, delta, reason || undefined)}
                      disabled={adjustingVariantId === item.variantId || delta === 0}
                      className="w-fit rounded-full bg-[#C8A45C] px-3 py-1 text-xs font-bold text-black disabled:opacity-50"
                    >
                      {adjustingVariantId === item.variantId ? 'Ajustement...' : 'Appliquer'}
                    </button>
                  </div>
                </td>
                <td className="px-4 py-4">
                  <div className="space-y-2 text-xs text-white/70">
                    {item.recentMovements.length === 0 ? (
                      <p className="text-white/40">Aucun mouvement recent.</p>
                    ) : (
                      item.recentMovements.map((movement) => (
                        <div key={movement.id} className="rounded-xl border border-white/10 bg-black/20 px-3 py-2">
                          <p>
                            {movement.type} {movement.quantityDelta > 0 ? '+' : ''}
                            {movement.quantityDelta} ({movement.source})
                          </p>
                          <p className="text-white/40">{new Date(movement.createdAt).toLocaleString('fr-FR')}</p>
                          {movement.reason ? <p className="text-white/55">{movement.reason}</p> : null}
                        </div>
                      ))
                    )}
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </section>
  );
}
