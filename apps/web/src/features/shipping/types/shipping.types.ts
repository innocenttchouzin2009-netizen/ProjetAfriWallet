export type ShippingCarrier = 'DHL' | 'DPD' | 'UPS';

export type ShippingStatus = 'CREATED' | 'IN_TRANSIT' | 'DELIVERED';

export interface CreateShipmentInput {
  orderId: string;
  carrier: ShippingCarrier;
  destinationCountry?: string;
}

export interface ShipmentResult {
  carrier: ShippingCarrier;
  trackingNumber: string;
  status: ShippingStatus;
  providerReference: string;
}
