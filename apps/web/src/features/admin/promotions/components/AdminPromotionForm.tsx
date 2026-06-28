"use client";

import type { AdminPromotionFormValues } from '../types/admin-promotion.types';

interface AdminPromotionFormProps {
  values: AdminPromotionFormValues;
  onChange: (values: AdminPromotionFormValues) => void;
  onSubmit: () => void;
  onCancel: () => void;
  editingId: string | null;
}

export default function AdminPromotionForm({ values, onChange, onSubmit, onCancel, editingId }: AdminPromotionFormProps) {
  const updateField = <K extends keyof AdminPromotionFormValues>(field: K, value: AdminPromotionFormValues[K]) => {
    onChange({ ...values, [field]: value });
  };

  return (
    <div className="rounded-[28px] border border-white/10 bg-white/5 p-6">
      <p className="text-sm uppercase tracking-[0.3em] text-[#C8A45C]">Promotions</p>
      <h2 className="mt-2 text-2xl font-black text-white">{editingId ? 'Modifier un code promo' : 'Créer un code promo'}</h2>

      <div className="mt-6 grid gap-4 md:grid-cols-2">
        <label className="text-sm text-white/70">
          Code promo
          <input value={values.code} onChange={(event) => updateField('code', event.target.value.toUpperCase())} className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white" />
        </label>

        <label className="text-sm text-white/70">
          Type de réduction
          <select value={values.discountType} onChange={(event) => updateField('discountType', event.target.value as AdminPromotionFormValues['discountType'])} className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white">
            <option value="PERCENTAGE">Pourcentage (%)</option>
            <option value="FIXED">Montant fixe (€)</option>
          </select>
        </label>

        <label className="text-sm text-white/70">
          Valeur
          <input type="number" value={values.discountValue} onChange={(event) => updateField('discountValue', Number(event.target.value))} className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white" />
        </label>

        <label className="text-sm text-white/70">
          Minimum d'achat (€)
          <input type="number" value={values.minPurchase} onChange={(event) => updateField('minPurchase', Number(event.target.value))} className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white" />
        </label>

        <label className="text-sm text-white/70">
          Utilisations max
          <input type="number" value={values.usageLimit} onChange={(event) => updateField('usageLimit', Number(event.target.value))} className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white" />
        </label>

        <label className="text-sm text-white/70">
          Début
          <input type="datetime-local" value={values.startsAt} onChange={(event) => updateField('startsAt', event.target.value)} className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white" />
        </label>

        <label className="text-sm text-white/70">
          Fin
          <input type="datetime-local" value={values.endsAt} onChange={(event) => updateField('endsAt', event.target.value)} className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white" />
        </label>

        <label className="flex items-center gap-3 rounded-[20px] border border-white/10 bg-black/20 px-4 py-3 text-sm text-white/70">
          <input type="checkbox" checked={values.active} onChange={(event) => updateField('active', event.target.checked)} />
          Promo active
        </label>

        <label className="flex items-center gap-3 rounded-[20px] border border-white/10 bg-black/20 px-4 py-3 text-sm text-white/70">
          <input type="checkbox" checked={values.appliesToAll} onChange={(event) => updateField('appliesToAll', event.target.checked)} />
          Appliquer à tous les produits
        </label>

        {!values.appliesToAll ? (
          <label className="text-sm text-white/70 md:col-span-2">
            Catégorie ciblée
            <input value={values.category} onChange={(event) => updateField('category', event.target.value)} className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white" />
          </label>
        ) : null}
      </div>

      <div className="mt-6 flex gap-3">
        <button onClick={onSubmit} className="rounded-full bg-[#C8A45C] px-5 py-3 font-semibold text-black">
          {editingId ? 'Enregistrer' : 'Créer'}
        </button>
        <button onClick={onCancel} className="rounded-full border border-white/10 px-5 py-3 font-semibold text-white/70">
          Annuler
        </button>
      </div>
    </div>
  );
}
