import type { NotificationTemplate } from '../types/notification.types';

export function orderInProductionTemplate(input: { orderId: string; customerName?: string }): NotificationTemplate {
  const name = input.customerName?.trim() || 'client';

  return {
    subject: `Commande en production: ${input.orderId}`,
    text: `Bonjour ${name}, votre commande ${input.orderId} vient d'entrer en production.`,
    html: `<p>Bonjour ${name},</p><p>Votre commande <strong>${input.orderId}</strong> vient d'entrer en production.</p>`,
  };
}
