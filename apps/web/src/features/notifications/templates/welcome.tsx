import type { NotificationTemplate } from '../types/notification.types';

export function welcomeTemplate(firstName?: string): NotificationTemplate {
  const safeName = firstName?.trim() || 'client';

  return {
    subject: 'Bienvenue chez Dope&Cute Studio',
    text: `Bonjour ${safeName}, bienvenue sur Dope&Cute Studio. Votre compte est pret.`,
    html: `<p>Bonjour ${safeName},</p><p>Bienvenue sur <strong>Dope&Cute Studio</strong>. Votre compte est pret.</p>`,
  };
}
