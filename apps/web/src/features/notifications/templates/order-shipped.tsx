import type { NotificationTemplate } from '../types/notification.types';

export function orderShippedTemplate(input: {
  orderId: string;
  customerName?: string;
  trackingNumber?: string;
}): NotificationTemplate {
  const name = input.customerName?.trim() || 'client';
  const tracking = input.trackingNumber ? `Suivi: ${input.trackingNumber}` : 'Numero de suivi a venir';

  return {
    subject: `Commande expediee: ${input.orderId}`,
    text: `Bonjour ${name}, votre commande ${input.orderId} est expediee. ${tracking}.`,
    html: `<p>Bonjour ${name},</p><p>Votre commande <strong>${input.orderId}</strong> est expediee.</p><p>${tracking}</p>`,
  };
}
