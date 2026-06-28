export type CartItemKind = 'product' | 'studio-design';

export interface CartItem {
  id: string;
  name: string;
  kind: CartItemKind;
  price: number;
  quantity: number;
  description?: string;
  image?: string;
  metadata?: Record<string, string | number | boolean | undefined>;
}

export interface CartContextValue {
  items: CartItem[];
  itemCount: number;
  subtotal: number;
  toastMessage: string;
  toastVisible: boolean;
  addItem: (item: Omit<CartItem, 'quantity'> & { quantity?: number }) => void;
  removeItem: (id: string) => void;
  updateQuantity: (id: string, quantity: number) => void;
  clearCart: () => void;
  showToast: (message: string) => void;
  hideToast: () => void;
}
