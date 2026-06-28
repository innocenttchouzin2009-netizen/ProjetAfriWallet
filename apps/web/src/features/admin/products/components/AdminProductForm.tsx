"use client";

import type { AdminProductFormValues } from '../types/admin-product.types';

interface AdminProductFormProps {
  values: AdminProductFormValues;
  onChange: (values: AdminProductFormValues) => void;
  onSubmit: () => void;
  onCancel: () => void;
  editingId: string | null;
}

export default function AdminProductForm({ values, onChange, onSubmit, onCancel, editingId }: AdminProductFormProps) {
  const updateField = <K extends keyof AdminProductFormValues>(field: K, value: AdminProductFormValues[K]) => {
    onChange({ ...values, [field]: value });
  };

  return (
    <div className="rounded-[28px] border border-white/10 bg-white/5 p-6">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm uppercase tracking-[0.3em] text-[#C8A45C]">Produits</p>
          <h2 className="mt-2 text-2xl font-black text-white">{editingId ? 'Modifier un produit' : 'Ajouter un produit'}</h2>
        </div>
      </div>

      <div className="mt-6 grid gap-4 md:grid-cols-2">
        <label className="text-sm text-white/70">
          Nom
          <input value={values.name} onChange={(event) => updateField('name', event.target.value)} className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white" />
        </label>

        <label className="text-sm text-white/70">
          SKU
          <input value={values.sku} onChange={(event) => updateField('sku', event.target.value)} className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white" />
        </label>

        <label className="text-sm text-white/70">
          Catégorie
          <input value={values.category} onChange={(event) => updateField('category', event.target.value)} className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white" />
        </label>

        <label className="text-sm text-white/70">
          Prix (€)
          <input type="number" value={values.price} onChange={(event) => updateField('price', Number(event.target.value))} className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white" />
        </label>

        <label className="text-sm text-white/70">
          Stock
          <input type="number" value={values.stock} onChange={(event) => updateField('stock', Number(event.target.value))} className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white" />
        </label>

        <label className="flex items-center gap-3 rounded-[20px] border border-white/10 bg-black/20 px-4 py-3 text-sm text-white/70">
          <input type="checkbox" checked={values.active} onChange={(event) => updateField('active', event.target.checked)} />
          Produit actif
        </label>
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
