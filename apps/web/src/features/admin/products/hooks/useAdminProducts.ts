"use client";

import { useEffect, useMemo, useState } from 'react';
import type { AdminProduct, AdminProductFormValues } from '../types/admin-product.types';

const emptyForm: AdminProductFormValues = {
  name: '',
  description: '',
  supplierUrl: '',
  supplierName: '',
  supplierSku: '',
  price: 0,
  stock: 0,
  category: 'depremium',
  hatType: 'Snapback',
  sku: '',
  active: true,
};

function fileToDataUrl(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      if (typeof reader.result === 'string') {
        resolve(reader.result);
        return;
      }

      reject(new Error('Invalid file payload'));
    };
    reader.onerror = () => reject(new Error('Unable to read file'));
    reader.readAsDataURL(file);
  });
}

export function useAdminProducts() {
  const [products, setProducts] = useState<AdminProduct[]>([]);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formValues, setFormValues] = useState<AdminProductFormValues>(emptyForm);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);

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
      const created = (await response.json()) as AdminProduct;
      await loadProducts();
      setEditingId(created.id);
      setFormValues({
        name: created.name,
        description: created.description,
        supplierUrl: created.supplierUrl,
        supplierName: created.supplierName,
        supplierSku: created.supplierSku,
        price: created.price,
        stock: created.stock,
        category: created.category,
        hatType: created.hatType,
        sku: created.sku,
        active: created.active,
      });
      return;
    }

    const body = await response.json().catch(() => ({ message: 'Failed to create product' }));
    setError(body.message ?? 'Failed to create product');
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
      setEditingId(id);
      return;
    }

    const body = await response.json().catch(() => ({ message: 'Failed to update product' }));
    setError(body.message ?? 'Failed to update product');
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
      description: product.description,
      supplierUrl: product.supplierUrl,
      supplierName: product.supplierName,
      supplierSku: product.supplierSku,
      price: product.price,
      stock: product.stock,
      category: product.category,
      hatType: product.hatType,
      sku: product.sku,
      active: product.active,
    });
  };

  const uploadProductImage = async (productId: string, file: File) => {
    setUploading(true);
    setError(null);

    try {
      const fileAsDataUrl = await fileToDataUrl(file);
      const uploadResponse = await fetch('/api/admin/uploads/cloudinary', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ file: fileAsDataUrl, fileName: file.name }),
      });

      if (!uploadResponse.ok) {
        const body = await uploadResponse.json().catch(() => ({ message: 'Upload failed' }));
        throw new Error(body.message ?? 'Upload failed');
      }

      const uploaded = (await uploadResponse.json()) as { url: string; publicId: string };

      const imageResponse = await fetch(`/api/admin/products/${productId}/images`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ url: uploaded.url, publicId: uploaded.publicId }),
      });

      if (!imageResponse.ok) {
        const body = await imageResponse.json().catch(() => ({ message: 'Image save failed' }));
        throw new Error(body.message ?? 'Image save failed');
      }

      await loadProducts();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown upload error');
    } finally {
      setUploading(false);
    }
  };

  const deleteProductImage = async (productId: string, imageId: string) => {
    const response = await fetch(`/api/admin/products/${productId}/images/${imageId}`, { method: 'DELETE' });
    if (!response.ok) {
      const body = await response.json().catch(() => ({ message: 'Failed to delete image' }));
      setError(body.message ?? 'Failed to delete image');
      return;
    }

    await loadProducts();
  };

  const setPrimaryImage = async (productId: string, imageId: string) => {
    const response = await fetch(`/api/admin/products/${productId}/images/${imageId}`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ isPrimary: true }),
    });

    if (!response.ok) {
      const body = await response.json().catch(() => ({ message: 'Failed to set main image' }));
      setError(body.message ?? 'Failed to set main image');
      return;
    }

    await loadProducts();
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
    uploadProductImage,
    deleteProductImage,
    setPrimaryImage,
    uploading,
    resetForm,
    stats,
    refresh: loadProducts,
  };
}
