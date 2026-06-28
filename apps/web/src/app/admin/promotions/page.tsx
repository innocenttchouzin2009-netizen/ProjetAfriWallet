"use client";

import AdminPromotionForm from '@/features/admin/promotions/components/AdminPromotionForm';
import AdminPromotionTable from '@/features/admin/promotions/components/AdminPromotionTable';
import { useAdminPromotions } from '@/features/admin/promotions/hooks/useAdminPromotions';

export default function AdminPromotionsPage() {
  const {
    promotions,
    loading,
    error,
    editingId,
    formValues,
    setFormValues,
    createPromotion,
    updatePromotion,
    removePromotion,
    startEditing,
    resetForm,
    stats,
  } = useAdminPromotions();

  const handleSubmit = () => {
    if (editingId) {
      void updatePromotion(editingId);
      return;
    }

    void createPromotion();
  };

  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-8 text-white md:px-10 lg:px-16">
      <div className="rounded-[36px] border border-white/10 bg-gradient-to-br from-[#C8A45C]/20 via-black/80 to-black p-8">
        <p className="text-sm uppercase tracking-[0.35em] text-[#C8A45C]">Admin</p>
        <h1 className="mt-4 text-4xl font-black md:text-5xl">Gestion promotions</h1>
        <p className="mt-4 max-w-2xl text-white/70">
          Crée, modifie et active des codes promo avec règles de validité.
        </p>
      </div>

      <div className="mt-8 grid gap-4 md:grid-cols-3">
        <div className="rounded-[24px] border border-white/10 bg-white/5 p-5">
          <p className="text-sm text-white/60">Promotions</p>
          <p className="mt-2 text-3xl font-black text-white">{stats.total}</p>
        </div>
        <div className="rounded-[24px] border border-white/10 bg-white/5 p-5">
          <p className="text-sm text-white/60">Actives</p>
          <p className="mt-2 text-3xl font-black text-white">{stats.activeCount}</p>
        </div>
        <div className="rounded-[24px] border border-white/10 bg-white/5 p-5">
          <p className="text-sm text-white/60">Ciblées catégorie</p>
          <p className="mt-2 text-3xl font-black text-white">{stats.categoryScoped}</p>
        </div>
      </div>

      <div className="mt-8 grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
        <AdminPromotionForm
          values={formValues}
          onChange={setFormValues}
          onSubmit={handleSubmit}
          onCancel={resetForm}
          editingId={editingId}
        />

        <div className="space-y-4">
          {loading ? <p className="text-sm text-white/70">Chargement des promotions...</p> : null}
          {error ? <p className="text-sm text-[#F0B86E]">Erreur: {error}</p> : null}
          <AdminPromotionTable promotions={promotions} onEdit={startEditing} onDelete={(id) => void removePromotion(id)} />
        </div>
      </div>
    </main>
  );
}
