import type { ShippingProvider } from './ShippingProvider';
import type { CreateShipmentInput, ShipmentResult } from '../types/shipping.types';

function randomDigits(length: number) {
  return Array.from({ length }, () => Math.floor(Math.random() * 10)).join('');
}

function buildTrackingNumber(carrier: CreateShipmentInput['carrier']): string {
  const prefix = carrier === 'DHL' ? 'DHL' : carrier === 'DPD' ? 'DPD' : 'UPS';
  return `${prefix}${randomDigits(12)}`;
}

export class MockShippingProvider implements ShippingProvider {
  async createShipment(input: CreateShipmentInput): Promise<ShipmentResult> {
    const trackingNumber = buildTrackingNumber(input.carrier);

    return {
      carrier: input.carrier,
      trackingNumber,
      status: 'CREATED',
      providerReference: `ship_${input.orderId}_${Date.now()}`,
    };
  }
}
