"use client";

import AdminProductForm from '@/features/admin/products/components/AdminProductForm';
import AdminProductTable from '@/features/admin/products/components/AdminProductTable';
import { useAdminProducts } from '@/features/admin/products/hooks/useAdminProducts';

export default function AdminProductsPage() {
  const {
    products,
    loading,
    error,
    editingId,
    formValues,
    setFormValues,
    createProduct,
    updateProduct,
    removeProduct,
    startEditing,
    resetForm,
    stats,
  } = useAdminProducts();

  const handleSubmit = () => {
    if (editingId) {
      updateProduct(editingId);
      return;
    }

    createProduct();
  };

  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-8 text-white md:px-10 lg:px-16">
      <div className="rounded-[36px] border border-white/10 bg-gradient-to-br from-[#C8A45C]/20 via-black/80 to-black p-8">
        <p className="text-sm uppercase tracking-[0.35em] text-[#C8A45C]">Admin</p>
        <h1 className="mt-4 text-4xl font-black md:text-5xl">Gestion produits</h1>
        <p className="mt-4 max-w-2xl text-white/70">
          Crée, modifie ou supprime des produits depuis l’interface d’administration.
        </p>
      </div>

      <div className="mt-8 grid gap-4 md:grid-cols-3">
        <div className="rounded-[24px] border border-white/10 bg-white/5 p-5">
          <p className="text-sm text-white/60">Produits</p>
          <p className="mt-2 text-3xl font-black text-white">{stats.totalProducts}</p>
        </div>
        <div className="rounded-[24px] border border-white/10 bg-white/5 p-5">
          <p className="text-sm text-white/60">Actifs</p>
          <p className="mt-2 text-3xl font-black text-white">{stats.activeProducts}</p>
        </div>
        <div className="rounded-[24px] border border-white/10 bg-white/5 p-5">
          <p className="text-sm text-white/60">Stock faible</p>
          <p className="mt-2 text-3xl font-black text-white">{stats.lowStock}</p>
        </div>
      </div>

      <div className="mt-8 grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
        <AdminProductForm
          values={formValues}
          onChange={setFormValues}
          onSubmit={handleSubmit}
          onCancel={resetForm}
          editingId={editingId}
        />

        <div className="space-y-4">
          {loading ? <p className="text-sm text-white/70">Chargement des produits...</p> : null}
          {error ? <p className="text-sm text-[#F0B86E]">Erreur: {error}</p> : null}
          <AdminProductTable products={products} onEdit={startEditing} onDelete={removeProduct} />
        </div>
      </div>
    </main>
  );
}
