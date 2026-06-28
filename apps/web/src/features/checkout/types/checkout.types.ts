export interface CustomerInfo {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
}

export interface ShippingAddress {
  address: string;
  postalCode: string;
  city: string;
  country: string;
}

export interface ShippingMethod {
  id: string;
  label: string;
  description: string;
  price: number;
  eta: string;
}

export interface CheckoutFormValues {
  customer: CustomerInfo;
  address: ShippingAddress;
  shippingMethodId: string;
  paymentProvider: 'stripe' | 'paypal';
}

export interface CheckoutState {
  values: CheckoutFormValues;
  isSubmitting: boolean;
  isComplete: boolean;
  errorMessage?: string;
}

export interface CheckoutSubmitResult {
  ok: boolean;
  redirectUrl?: string;
  stripeSessionId?: string;
  requiresAction?: boolean;
  message?: string;
}
