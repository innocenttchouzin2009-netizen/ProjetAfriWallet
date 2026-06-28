import type { EmailMessage, EmailSendResult } from '../types/notification.types';

export class EmailProvider {
  async send(message: EmailMessage): Promise<EmailSendResult> {
    // Provider abstraction: replace this stub with Resend/SendGrid/Mailgun later.
    console.log('EMAIL_PROVIDER_SEND', {
      to: message.to,
      subject: message.subject,
    });

    return {
      success: true,
      providerMessageId: `mock-${Date.now()}`,
    };
  }
}
