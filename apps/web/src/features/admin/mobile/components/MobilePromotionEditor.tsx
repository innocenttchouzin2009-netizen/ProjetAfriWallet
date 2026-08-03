"use client";

import { useEffect, useState } from 'react';
import { COLLECTION_DEFINITIONS } from '@/features/admin/catalog/data/catalog-taxonomy';

type PromotionItem = {
  id: string;
  code: string;
  discountType: 'PERCENTAGE' | 'FIXED';
  discountValue: number;
  startsAt: string;
  endsAt: string;
  active: boolean;
  scope: 'all' | 'category' | 'collection';
  category: string | null;
  collectionSlug: string | null;
};

type PromotionForm = {
  code: string;
  discountType: 'PERCENTAGE' | 'FIXED';
  discountValue: number;
  startsAt: string;
  endsAt: string;
  active: boolean;
  scope: 'all' | 'category' | 'collection';
  category: string;
  collectionSlug: string;
};

const emptyPromotionForm: PromotionForm = {
  code: '',
  discountType: 'PERCENTAGE',
  discountValue: 10,
  startsAt: '',
  endsAt: '',
  active: true,
  scope: 'all',
  category: '',
  collectionSlug: '',
};

interface MobilePromotionEditorProps {
  onBack: () => void;
}

function toInputDate(value: string) {
  const date = new Date(value);
  const tz = date.getTimezoneOffset() * 60000;
  return new Date(date.getTime() - tz).toISOString().slice(0, 16);
}

export default function MobilePromotionEditor({ onBack }: MobilePromotionEditorProps) {
  const [promotions, setPromotions] = useState<PromotionItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [form, setForm] = useState<PromotionForm>(emptyPromotionForm);

  const loadPromotions = async () => {
    setLoading(true);
    try {
      const response = await fetch('/api/admin/promotions');
      if (!response.ok) throw new Error('Chargement promotions impossible');
      const data = (await response.json()) as PromotionItem[];
      setPromotions(data);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erreur promotions');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadPromotions();
  }, []);

  const savePromotion = async () => {
    if (!form.code.trim() || !form.startsAt || !form.endsAt) {
      setMessage('Code + dates obligatoires.');
      return;
    }

    setSaving(true);
    setMessage(null);

    try {
      const payload = {
        code: form.code.trim().toUpperCase(),
        discountType: form.discountType,
        discountValue: Number(form.discountValue),
        startsAt: new Date(form.startsAt).toISOString(),
        endsAt: new Date(form.endsAt).toISOString(),
        active: form.active,
        scope: form.scope,
        appliesToAll: form.scope === 'all',
        category: form.scope === 'category' ? form.category.trim() : undefined,
        collectionSlug: form.scope === 'collection' ? form.collectionSlug.trim() : undefined,
      };

      const response = await fetch('/api/admin/promotions', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        const body = await response.json().catch(() => ({ message: 'Création promo échouée' }));
        throw new Error(body.message ?? 'Création promo échouée');
      }

      setMessage('Promotion créée.');
      setForm(emptyPromotionForm);
      await loadPromotions();
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erreur promotion');
    } finally {
      setSaving(false);
    }
  };

  return (
    <section className="space-y-3">
      <button onClick={onBack} className="rounded-full border border-black/10 bg-white px-4 py-2 text-sm font-semibold text-[#4a5d78]">Retour</button>

      <div className="rounded-[22px] border border-black/10 bg-white p-4 shadow-[0_10px_20px_rgba(0,0,0,0.06)]">
        <p className="text-sm font-semibold text-[#5d6f87]">Nouvelle promotion</p>

        <div className="mt-3 space-y-3">
          <input value={form.code} onChange={(event) => setForm((prev) => ({ ...prev, code: event.target.value.toUpperCase() }))} placeholder="Code promo" className="w-full rounded-2xl border border-black/10 bg-[#f4f7fc] px-4 py-3 text-sm" />

          <div className="grid grid-cols-2 gap-2">
            <select value={form.discountType} onChange={(event) => setForm((prev) => ({ ...prev, discountType: event.target.value as 'PERCENTAGE' | 'FIXED' }))} className="rounded-2xl border border-black/10 bg-[#f4f7fc] px-4 py-3 text-sm">
              <option value="PERCENTAGE">Pourcentage</option>
              <option value="FIXED">Montant fixe (€)</option>
            </select>
            <input type="number" value={form.discountValue} onChange={(event) => setForm((prev) => ({ ...prev, discountValue: Number(event.target.value) }))} placeholder="Valeur" className="rounded-2xl border border-black/10 bg-[#f4f7fc] px-4 py-3 text-sm" />
          </div>

          <div className="grid grid-cols-2 gap-2">
            <input type="datetime-local" value={form.startsAt} onChange={(event) => setForm((prev) => ({ ...prev, startsAt: event.target.value }))} className="rounded-2xl border border-black/10 bg-[#f4f7fc] px-4 py-3 text-sm" />
            <input type="datetime-local" value={form.endsAt} onChange={(event) => setForm((prev) => ({ ...prev, endsAt: event.target.value }))} className="rounded-2xl border border-black/10 bg-[#f4f7fc] px-4 py-3 text-sm" />
          </div>

          <select value={form.scope} onChange={(event) => setForm((prev) => ({ ...prev, scope: event.target.value as 'all' | 'category' | 'collection' }))} className="w-full rounded-2xl border border-black/10 bg-[#f4f7fc] px-4 py-3 text-sm">
            <option value="all">Tous les produits</option>
            <option value="category">Catégorie</option>
            <option value="collection">Collection</option>
          </select>

          {form.scope === 'category' ? <input value={form.category} onChange={(event) => setForm((prev) => ({ ...prev, category: event.target.value }))} placeholder="Catégorie ciblée" className="w-full rounded-2xl border border-black/10 bg-[#f4f7fc] px-4 py-3 text-sm" /> : null}

          {form.scope === 'collection' ? (
            <select value={form.collectionSlug} onChange={(event) => setForm((prev) => ({ ...prev, collectionSlug: event.target.value }))} className="w-full rounded-2xl border border-black/10 bg-[#f4f7fc] px-4 py-3 text-sm">
              <option value="">Choisir collection</option>
              {COLLECTION_DEFINITIONS.map((item) => (
                <option key={item.slug} value={item.slug}>{item.label}</option>
              ))}
            </select>
          ) : null}

          <label className="flex items-center gap-2 text-sm text-[#4d5f79]">
            <input type="checkbox" checked={form.active} onChange={(event) => setForm((prev) => ({ ...prev, active: event.target.checked }))} /> Promotion active
          </label>

          <button onClick={() => void savePromotion()} disabled={saving} className="w-full rounded-2xl bg-[#1f7aff] px-4 py-3 text-sm font-semibold text-white disabled:opacity-60">
            {saving ? 'Création...' : 'Créer promotion'}
          </button>

          {message ? <p className="text-sm text-[#44607f]">{message}</p> : null}
        </div>
      </div>

      <div className="rounded-[22px] border border-black/10 bg-white p-4 shadow-[0_10px_20px_rgba(0,0,0,0.06)]">
        <p className="text-sm font-semibold text-[#5d6f87]">Promotions existantes</p>
        {loading ? <p className="mt-2 text-sm text-[#647892]">Chargement...</p> : null}
        <ul className="mt-2 space-y-2">
          {promotions.slice(0, 15).map((promotion) => (
            <li key={promotion.id} className="rounded-2xl border border-black/10 bg-[#f8fbff] p-3">
              <p className="font-semibold text-[#22324b]">{promotion.code}</p>
              <p className="text-xs text-[#60728a]">
                {promotion.discountType === 'PERCENTAGE' ? `${promotion.discountValue}%` : `${promotion.discountValue.toFixed(2)} €`} • {promotion.scope}
              </p>
              <p className="text-xs text-[#60728a]">{toInputDate(promotion.startsAt)} → {toInputDate(promotion.endsAt)}</p>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
