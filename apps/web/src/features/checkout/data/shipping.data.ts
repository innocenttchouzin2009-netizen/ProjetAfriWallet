import type { ShippingMethod } from '../types/checkout.types';

export const shippingMethods: ShippingMethod[] = [
  {
    id: 'standard',
    label: 'Livraison standard',
    description: 'Livraison en 3 à 5 jours ouvrés',
    price: 7.9,
    eta: '3–5 jours',
  },
  {
    id: 'express',
    label: 'Livraison express',
    description: 'Priorité express pour les commandes urgentes',
    price: 14.9,
    eta: '24h',
  },
  {
    id: 'pickup',
    label: 'Retrait en boutique',
    description: 'Retrait gratuit à Paris',
    price: 0,
    eta: 'Le jour même',
  },
];
