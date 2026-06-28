"use client";

import type { AdminPromotion } from '../types/admin-promotion.types';

interface AdminPromotionTableProps {
  promotions: AdminPromotion[];
  onEdit: (promotion: AdminPromotion) => void;
  onDelete: (id: string) => void;
}

function formatDiscount(promotion: AdminPromotion) {
  if (promotion.discountType === 'PERCENTAGE') {
    return `${promotion.discountValue}%`;
  }

  return `${promotion.discountValue.toFixed(2)} €`;
}

export default function AdminPromotionTable({ promotions, onEdit, onDelete }: AdminPromotionTableProps) {
  return (
    <div className="overflow-hidden rounded-[28px] border border-white/10 bg-white/5">
      <table className="min-w-full text-left text-sm text-white/80">
        <thead className="bg-black/20 text-white/60">
          <tr>
            <th className="px-4 py-3">Code</th>
            <th className="px-4 py-3">Réduction</th>
            <th className="px-4 py-3">Validité</th>
            <th className="px-4 py-3">Cible</th>
            <th className="px-4 py-3">Usage</th>
            <th className="px-4 py-3">Statut</th>
            <th className="px-4 py-3">Actions</th>
          </tr>
        </thead>
        <tbody>
          {promotions.map((promotion) => (
            <tr key={promotion.id} className="border-t border-white/10">
              <td className="px-4 py-4 font-semibold text-white">{promotion.code}</td>
              <td className="px-4 py-4">{formatDiscount(promotion)}</td>
              <td className="px-4 py-4">
                {new Date(promotion.startsAt).toLocaleDateString()} - {new Date(promotion.endsAt).toLocaleDateString()}
              </td>
              <td className="px-4 py-4">{promotion.appliesToAll ? 'Tous produits' : promotion.category || 'Catégorie'}</td>
              <td className="px-4 py-4">
                {promotion.usageCount}/{promotion.usageLimit ?? '∞'}
              </td>
              <td className="px-4 py-4">
                <span className={`rounded-full px-3 py-1 text-xs ${promotion.active ? 'bg-[#8ED8A2]/20 text-[#8ED8A2]' : 'bg-white/10 text-white/60'}`}>
                  {promotion.active ? 'Actif' : 'Inactif'}
                </span>
              </td>
              <td className="px-4 py-4">
                <div className="flex gap-2">
                  <button onClick={() => onEdit(promotion)} className="rounded-full border border-white/10 px-3 py-1 text-sm text-white/70">
                    Modifier
                  </button>
                  <button onClick={() => onDelete(promotion.id)} className="rounded-full border border-[#F0B86E]/30 px-3 py-1 text-sm text-[#F0B86E]">
                    Supprimer
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
