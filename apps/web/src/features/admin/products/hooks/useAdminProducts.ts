"use client";

import { useEffect, useMemo, useState } from 'react';
import type { AdminProduct, AdminProductFormValues } from '../types/admin-product.types';

const emptyForm: AdminProductFormValues = {
  name: '',
  price: 0,
  stock: 0,
  category: '',
  sku: '',
  active: true,
};

export function useAdminProducts() {
  const [products, setProducts] = useState<AdminProduct[]>([]);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formValues, setFormValues] = useState<AdminProductFormValues>(emptyForm);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadProducts = async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/admin/products');
      if (!response.ok) {
        throw new Error('Failed to load products');
      }

      const data = (await response.json()) as AdminProduct[];
      setProducts(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadProducts();
  }, []);

  const resetForm = () => {
    setEditingId(null);
    setFormValues(emptyForm);
  };

  const createProduct = async () => {
    if (!formValues.name.trim()) return;

    const response = await fetch('/api/admin/products', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(formValues),
    });

    if (response.ok) {
      await loadProducts();
    }

    resetForm();
  };

  const updateProduct = async (id: string) => {
    if (!formValues.name.trim()) return;

    const response = await fetch(`/api/admin/products/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(formValues),
    });

    if (response.ok) {
      await loadProducts();
    }

    resetForm();
  };

  const removeProduct = async (id: string) => {
    const response = await fetch(`/api/admin/products/${id}`, { method: 'DELETE' });
    if (response.ok) {
      await loadProducts();
    }
  };

  const startEditing = (product: AdminProduct) => {
    setEditingId(product.id);
    setFormValues({
      name: product.name,
      price: product.price,
      stock: product.stock,
      category: product.category,
      sku: product.sku,
      active: product.active,
    });
  };

  const stats = useMemo(() => {
    const totalProducts = products.length;
    const activeProducts = products.filter((product) => product.active).length;
    const lowStock = products.filter((product) => product.stock < 10).length;

    return { totalProducts, activeProducts, lowStock };
  }, [products]);

  return {
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
    refresh: loadProducts,
  };
}
