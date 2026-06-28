import type { CreateShipmentInput, ShipmentResult } from '../types/shipping.types';

export interface ShippingProvider {
  createShipment(input: CreateShipmentInput): Promise<ShipmentResult>;
}
