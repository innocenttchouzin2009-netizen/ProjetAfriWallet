"use client";

import { ChangeEvent, useEffect, useMemo, useRef, useState } from 'react';
import { COLLECTION_DEFINITIONS, HAT_TYPE_OPTIONS } from '@/features/admin/catalog/data/catalog-taxonomy';

type AdminProduct = {
  id: string;
  name: string;
  description: string;
  price: number;
  stock: number;
  category: string;
  collectionSlug: string;
  hatType: string;
  sku: string;
  active: boolean;
  images: Array<{ id: string; url: string; isPrimary: boolean }>;
};

type ProductForm = {
  name: string;
  description: string;
  price: number;
  stock: number;
  category: string;
  hatType: string;
  sku: string;
  active: boolean;
};

const emptyForm: ProductForm = {
  name: '',
  description: '',
  price: 20,
  stock: 1,
  category: 'espremium',
  hatType: 'Snapback',
  sku: '',
  active: true,
};

interface MobileProductEditorProps {
  onBack: () => void;
}

function fileToDataUrl(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      if (typeof reader.result === 'string') {
        resolve(reader.result);
        return;
      }
      reject(new Error('Fichier invalide'));
    };
    reader.onerror = () => reject(new Error('Lecture photo impossible'));
    reader.readAsDataURL(file);
  });
}

export default function MobileProductEditor({ onBack }: MobileProductEditorProps) {
  const [products, setProducts] = useState<AdminProduct[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [form, setForm] = useState<ProductForm>(emptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const cameraInputRef = useRef<HTMLInputElement | null>(null);
  const galleryInputRef = useRef<HTMLInputElement | null>(null);

  const selected = useMemo(() => products.find((item) => item.id === selectedId) ?? null, [products, selectedId]);

  const loadProducts = async () => {
    setLoading(true);
    setMessage(null);
    try {
      const response = await fetch('/api/admin/products');
      if (!response.ok) throw new Error('Chargement produits impossible');
      const data = (await response.json()) as AdminProduct[];
      setProducts(data);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erreur produit');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadProducts();
  }, []);

  useEffect(() => {
    if (!selected) return;
    setForm({
      name: selected.name,
      description: selected.description,
      price: selected.price,
      stock: selected.stock,
      category: selected.collectionSlug || selected.category,
      hatType: selected.hatType || 'Snapback',
      sku: selected.sku,
      active: selected.active,
    });
  }, [selected]);

  const createOrUpdate = async () => {
    if (!form.name.trim()) {
      setMessage('Le nom est obligatoire.');
      return;
    }

    setSaving(true);
    setMessage(null);

    const payload = {
      ...form,
      name: form.name.trim(),
      description: form.description.trim(),
      sku: form.sku.trim() || `MOBILE-${Date.now()}`,
    };

    try {
      const response = selectedId
        ? await fetch(`/api/admin/products/${selectedId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload),
          })
        : await fetch('/api/admin/products', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload),
          });

      if (!response.ok) {
        const body = await response.json().catch(() => ({ message: 'Sauvegarde échouée' }));
        throw new Error(body.message ?? 'Sauvegarde échouée');
      }

      const saved = (await response.json()) as AdminProduct;
      setSelectedId(saved.id);
      setMessage(selectedId ? 'Produit mis à jour.' : 'Produit créé.');
      await loadProducts();
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erreur sauvegarde');
    } finally {
      setSaving(false);
    }
  };

  const uploadPhoto = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = '';

    if (!file || !selectedId) {
      if (!selectedId) setMessage('Crée le produit avant l\'upload photo.');
      return;
    }

    setUploading(true);
    setMessage(null);

    try {
      const fileAsDataUrl = await fileToDataUrl(file);
      const uploadResponse = await fetch('/api/admin/uploads/cloudinary', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ file: fileAsDataUrl, fileName: file.name }),
      });

      if (!uploadResponse.ok) {
        const body = await uploadResponse.json().catch(() => ({ message: 'Upload impossible' }));
        throw new Error(body.message ?? 'Upload impossible');
      }

      const uploaded = (await uploadResponse.json()) as { url: string; publicId: string };

      const imageResponse = await fetch(`/api/admin/products/${selectedId}/images`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ url: uploaded.url, publicId: uploaded.publicId, isPrimary: true }),
      });

      if (!imageResponse.ok) throw new Error('Enregistrement photo impossible');
      await loadProducts();
      setMessage('Photo importée.');
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erreur upload');
    } finally {
      setUploading(false);
    }
  };

  return (
    <section className="space-y-3">
      <button onClick={onBack} className="rounded-full border border-black/10 bg-white px-4 py-2 text-sm font-semibold text-[#4a5d78]">Retour</button>

      <div className="rounded-[22px] border border-black/10 bg-white p-4 shadow-[0_10px_20px_rgba(0,0,0,0.06)]">
        <p className="text-sm font-semibold text-[#5d6f87]">Produits existants</p>
        {loading ? <p className="mt-2 text-sm text-[#647892]">Chargement...</p> : null}
        <div className="mt-2 flex gap-2 overflow-x-auto pb-1">
          <button
            onClick={() => {
              setSelectedId(null);
              setForm(emptyForm);
              setMessage(null);
            }}
            className="whitespace-nowrap rounded-full border border-[#1f7aff]/30 bg-[#1f7aff]/10 px-3 py-2 text-xs font-semibold text-[#1f7aff]"
          >
            + Nouveau
          </button>
          {products.slice(0, 20).map((product) => (
            <button
              key={product.id}
              onClick={() => setSelectedId(product.id)}
              className={`whitespace-nowrap rounded-full border px-3 py-2 text-xs font-semibold ${selectedId === product.id ? 'border-[#1f7aff] bg-[#1f7aff] text-white' : 'border-black/10 bg-white text-[#4d5f79]'}`}
            >
              {product.name}
            </button>
          ))}
        </div>
      </div>

      <div className="rounded-[22px] border border-black/10 bg-white p-4 shadow-[0_10px_20px_rgba(0,0,0,0.06)]">
        <p className="text-sm font-semibold text-[#5d6f87]">Éditeur produit</p>

        <div className="mt-3 space-y-3">
          <input value={form.name} onChange={(event) => setForm((prev) => ({ ...prev, name: event.target.value }))} placeholder="Titre produit" className="w-full rounded-2xl border border-black/10 bg-[#f4f7fc] px-4 py-3 text-sm" />
          <textarea value={form.description} onChange={(event) => setForm((prev) => ({ ...prev, description: event.target.value }))} placeholder="Description" rows={3} className="w-full rounded-2xl border border-black/10 bg-[#f4f7fc] px-4 py-3 text-sm" />

          <div className="grid grid-cols-2 gap-2">
            <input type="number" value={form.price} onChange={(event) => setForm((prev) => ({ ...prev, price: Number(event.target.value) }))} placeholder="Prix" className="rounded-2xl border border-black/10 bg-[#f4f7fc] px-4 py-3 text-sm" />
            <input type="number" value={form.stock} onChange={(event) => setForm((prev) => ({ ...prev, stock: Number(event.target.value) }))} placeholder="Stock" className="rounded-2xl border border-black/10 bg-[#f4f7fc] px-4 py-3 text-sm" />
          </div>

          <input value={form.sku} onChange={(event) => setForm((prev) => ({ ...prev, sku: event.target.value }))} placeholder="SKU" className="w-full rounded-2xl border border-black/10 bg-[#f4f7fc] px-4 py-3 text-sm" />

          <select value={form.category} onChange={(event) => setForm((prev) => ({ ...prev, category: event.target.value }))} className="w-full rounded-2xl border border-black/10 bg-[#f4f7fc] px-4 py-3 text-sm">
            {COLLECTION_DEFINITIONS.map((item) => (
              <option key={item.slug} value={item.slug}>{item.label}</option>
            ))}
          </select>

          <select value={form.hatType} onChange={(event) => setForm((prev) => ({ ...prev, hatType: event.target.value }))} className="w-full rounded-2xl border border-black/10 bg-[#f4f7fc] px-4 py-3 text-sm">
            {HAT_TYPE_OPTIONS.map((item) => (
              <option key={item} value={item}>{item}</option>
            ))}
          </select>

          <label className="flex items-center gap-2 text-sm text-[#4d5f79]">
            <input type="checkbox" checked={form.active} onChange={(event) => setForm((prev) => ({ ...prev, active: event.target.checked }))} /> Produit publié
          </label>

          <div className="grid grid-cols-2 gap-2">
            <button onClick={() => void createOrUpdate()} disabled={saving} className="rounded-2xl bg-[#1f7aff] px-4 py-3 text-sm font-semibold text-white disabled:opacity-60">
              {saving ? 'Sauvegarde...' : selectedId ? 'Mettre à jour' : 'Créer'}
            </button>

            <button
              onClick={() => cameraInputRef.current?.click()}
              disabled={uploading || !selectedId}
              className="rounded-2xl border border-black/10 bg-[#eef4ff] px-4 py-3 text-sm font-semibold text-[#215cad] disabled:opacity-60"
            >
              {uploading ? 'Upload...' : 'Prendre une photo'}
            </button>
          </div>

          <button
            onClick={() => galleryInputRef.current?.click()}
            disabled={uploading || !selectedId}
            className="w-full rounded-2xl border border-black/10 bg-[#f4f7fc] px-4 py-3 text-sm font-semibold text-[#4d5f79] disabled:opacity-60"
          >
            Importer depuis la galerie
          </button>

          <input ref={cameraInputRef} type="file" accept="image/*" capture="environment" className="hidden" onChange={uploadPhoto} />
          <input ref={galleryInputRef} type="file" accept="image/*" className="hidden" onChange={uploadPhoto} />

          {!selectedId ? <p className="text-xs text-[#6a7a92]">Crée d&apos;abord le produit pour activer l&apos;upload photo.</p> : null}

          {selected?.images?.length ? (
            <div className="rounded-2xl border border-black/10 bg-[#f8fbff] p-3">
              <p className="mb-2 text-xs font-semibold uppercase tracking-[0.12em] text-[#6d7f97]">Photos</p>
              <div className="flex gap-2 overflow-x-auto">
                {selected.images.map((image) => (
                  /* eslint-disable-next-line @next/next/no-img-element */
                  <img key={image.id} src={image.url} alt="Produit" className="h-20 w-20 rounded-xl border border-black/10 object-cover" />
                ))}
              </div>
            </div>
          ) : null}

          {message ? <p className="text-sm text-[#44607f]">{message}</p> : null}
        </div>
      </div>
    </section>
  );
}
