import type { NotificationTemplate } from '../types/notification.types';

export function orderDeliveredTemplate(input: { orderId: string; customerName?: string }): NotificationTemplate {
  const name = input.customerName?.trim() || 'client';

  return {
    subject: `Commande livree: ${input.orderId}`,
    text: `Bonjour ${name}, votre commande ${input.orderId} est marquee comme livree.`,
    html: `<p>Bonjour ${name},</p><p>Votre commande <strong>${input.orderId}</strong> est marquee comme livree.</p>`,
  };
}
