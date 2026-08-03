"use client";

import type { AdminProduct } from '../types/admin-product.types';
import { getCollectionLabel } from '@/features/admin/catalog/data/catalog-taxonomy';

interface AdminProductTableProps {
  products: AdminProduct[];
  onEdit: (product: AdminProduct) => void;
  onDelete: (id: string) => void;
}

export default function AdminProductTable({ products, onEdit, onDelete }: AdminProductTableProps) {
  return (
    <div className="overflow-hidden rounded-[28px] border border-white/10 bg-white/5">
      <table className="min-w-full text-left text-sm text-white/80">
        <thead className="bg-black/20 text-white/60">
          <tr>
            <th className="px-4 py-3">Produit</th>
            <th className="px-4 py-3">Image</th>
            <th className="px-4 py-3">SKU</th>
            <th className="px-4 py-3">Fournisseur</th>
            <th className="px-4 py-3">Catégorie</th>
            <th className="px-4 py-3">Type</th>
            <th className="px-4 py-3">Prix</th>
            <th className="px-4 py-3">Stock</th>
            <th className="px-4 py-3">Statut</th>
            <th className="px-4 py-3">Actions</th>
          </tr>
        </thead>
        <tbody>
          {products.map((product) => (
            <tr key={product.id} className="border-t border-white/10">
              <td className="px-4 py-4">
                <p className="font-semibold text-white">{product.name}</p>
                {product.description ? <p className="mt-1 line-clamp-2 text-xs text-white/60">{product.description}</p> : null}
              </td>
              <td className="px-4 py-4">
                {product.primaryImageUrl ? (
                  /* eslint-disable-next-line @next/next/no-img-element */
                  <img src={product.primaryImageUrl} alt={product.name} className="h-12 w-12 rounded-lg border border-white/10 object-cover" />
                ) : (
                  <span className="text-xs text-white/50">Aucune</span>
                )}
              </td>
              <td className="px-4 py-4">{product.sku}</td>
              <td className="px-4 py-4">
                <p>{product.supplierName || 'N/A'}</p>
                {product.supplierSku ? <p className="text-xs text-white/50">{product.supplierSku}</p> : null}
                {product.supplierUrl ? (
                  <a href={product.supplierUrl} target="_blank" rel="noreferrer" className="text-xs text-sky-300 underline-offset-2 hover:underline">
                    Ouvrir lien
                  </a>
                ) : null}
              </td>
              <td className="px-4 py-4">{getCollectionLabel(product.collectionSlug)}</td>
              <td className="px-4 py-4">{product.hatType}</td>
              <td className="px-4 py-4">{product.price.toFixed(2)} €</td>
              <td className="px-4 py-4">{product.stock}</td>
              <td className="px-4 py-4">
                <span className={`rounded-full px-3 py-1 text-xs ${product.active ? 'bg-[#8ED8A2]/20 text-[#8ED8A2]' : 'bg-white/10 text-white/60'}`}>
                  {product.active ? 'Actif' : 'Inactif'}
                </span>
              </td>
              <td className="px-4 py-4">
                <div className="flex gap-2">
                  <button onClick={() => onEdit(product)} className="rounded-full border border-white/10 px-3 py-1 text-sm text-white/70">
                    Modifier
                  </button>
                  <button onClick={() => onDelete(product.id)} className="rounded-full border border-[#F0B86E]/30 px-3 py-1 text-sm text-[#F0B86E]">
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
