export type PromotionDiscountType = 'PERCENTAGE' | 'FIXED';

export interface AdminPromotion {
  id: string;
  code: string;
  discountType: PromotionDiscountType;
  discountValue: number;
  minPurchase: number | null;
  usageLimit: number | null;
  usageCount: number;
  startsAt: string;
  endsAt: string;
  active: boolean;
  appliesToAll: boolean;
  category: string | null;
}

export interface AdminPromotionFormValues {
  code: string;
  discountType: PromotionDiscountType;
  discountValue: number;
  minPurchase: number;
  usageLimit: number;
  startsAt: string;
  endsAt: string;
  active: boolean;
  appliesToAll: boolean;
  category: string;
}
