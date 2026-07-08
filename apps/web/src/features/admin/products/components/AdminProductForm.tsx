"use client";

import { ChangeEvent, useMemo, useState } from 'react';
import type { AdminProductFormValues, AdminProductImage } from '../types/admin-product.types';

interface AdminProductFormProps {
  values: AdminProductFormValues;
  images: AdminProductImage[];
  uploading: boolean;
  onChange: (values: AdminProductFormValues) => void;
  onSubmit: () => void;
  onCancel: () => void;
  onUploadImage: (file: File) => void;
  onDeleteImage: (imageId: string) => void;
  onSetPrimaryImage: (imageId: string) => void;
  editingId: string | null;
}

export default function AdminProductForm({
  values,
  images,
  uploading,
  onChange,
  onSubmit,
  onCancel,
  onUploadImage,
  onDeleteImage,
  onSetPrimaryImage,
  editingId,
}: AdminProductFormProps) {
  const [localPreview, setLocalPreview] = useState<string | null>(null);

  const updateField = <K extends keyof AdminProductFormValues>(field: K, value: AdminProductFormValues[K]) => {
    onChange({ ...values, [field]: value });
  };

  const gallery = useMemo(() => images, [images]);

  const handleFileSelect = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setLocalPreview(URL.createObjectURL(file));

    if (!editingId) {
      event.target.value = '';
      return;
    }

    onUploadImage(file);
    event.target.value = '';
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

        <label className="text-sm text-white/70 md:col-span-2">
          Description
          <textarea
            value={values.description}
            onChange={(event) => updateField('description', event.target.value)}
            rows={4}
            className="mt-2 w-full rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-white"
          />
        </label>

        <div className="rounded-[20px] border border-white/10 bg-black/20 p-4 text-sm text-white/70 md:col-span-2">
          <p className="text-xs uppercase tracking-[0.2em] text-[#C8A45C]">Informations fournisseur</p>

          <div className="mt-3 grid gap-4 md:grid-cols-2">
            <label className="text-sm text-white/70 md:col-span-2">
              Lien fournisseur (AliExpress)
              <input
                value={values.supplierUrl}
                onChange={(event) => updateField('supplierUrl', event.target.value)}
                className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white"
                placeholder="https://www.aliexpress.com/item/..."
              />
            </label>

            <label className="text-sm text-white/70">
              Nom fournisseur
              <input
                value={values.supplierName}
                onChange={(event) => updateField('supplierName', event.target.value)}
                className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white"
                placeholder="AliExpress"
              />
            </label>

            <label className="text-sm text-white/70">
              SKU fournisseur
              <input
                value={values.supplierSku}
                onChange={(event) => updateField('supplierSku', event.target.value)}
                className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white"
                placeholder="ALI-12345"
              />
            </label>
          </div>
        </div>

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

        <div className="rounded-[20px] border border-white/10 bg-black/20 p-4 text-sm text-white/70 md:col-span-2">
          <div className="flex flex-wrap items-center gap-3">
            <label className="inline-flex cursor-pointer rounded-full border border-white/20 px-4 py-2 text-sm text-white">
              Importer photo
              <input type="file" accept="image/*" className="hidden" onChange={handleFileSelect} />
            </label>
            {uploading ? <span className="text-[#C8A45C]">Upload en cours...</span> : null}
            {!editingId ? <span>Crée le produit avant l’upload définitif.</span> : null}
          </div>

          {localPreview ? (
            <div className="mt-3">
              <p className="mb-2 text-xs uppercase tracking-[0.2em] text-white/50">Prévisualisation locale</p>
              <img src={localPreview} alt="Prévisualisation" className="h-24 w-24 rounded-xl border border-white/10 object-cover" />
            </div>
          ) : null}

          <div className="mt-4 grid grid-cols-2 gap-3 md:grid-cols-4">
            {gallery.map((image) => (
              <div key={image.id} className="rounded-xl border border-white/10 bg-black/40 p-2">
                <img src={image.url} alt="Photo produit" className="h-24 w-full rounded-lg object-cover" />
                <div className="mt-2 flex flex-wrap gap-2">
                  <button
                    onClick={() => onSetPrimaryImage(image.id)}
                    className={`rounded-full px-3 py-1 text-xs ${image.isPrimary ? 'bg-[#8ED8A2]/20 text-[#8ED8A2]' : 'border border-white/20 text-white/70'}`}
                  >
                    {image.isPrimary ? 'Principale' : 'Définir principale'}
                  </button>
                  <button onClick={() => onDeleteImage(image.id)} className="rounded-full border border-[#F0B86E]/30 px-3 py-1 text-xs text-[#F0B86E]">
                    Supprimer
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
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
