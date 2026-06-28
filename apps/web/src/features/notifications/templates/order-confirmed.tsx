import type { NotificationTemplate } from '../types/notification.types';

export function orderConfirmedTemplate(input: {
  orderId: string;
  customerName?: string;
  totalCents?: number;
}): NotificationTemplate {
  const amount = input.totalCents !== undefined ? `${(input.totalCents / 100).toFixed(2)} EUR` : 'Montant indisponible';
  const name = input.customerName?.trim() || 'client';

  return {
    subject: `Commande confirmee: ${input.orderId}`,
    text: `Bonjour ${name}, votre commande ${input.orderId} est confirmee. Total: ${amount}.`,
    html: `<p>Bonjour ${name},</p><p>Votre commande <strong>${input.orderId}</strong> est confirmee.</p><p>Total: ${amount}.</p>`,
  };
}
