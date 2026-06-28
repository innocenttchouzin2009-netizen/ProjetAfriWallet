export type NotificationChannel = 'email' | 'sms';

export type NotificationOrderStatus =
  | 'CONFIRMED'
  | 'IN_PRODUCTION'
  | 'SHIPPED'
  | 'DELIVERED';

export interface NotificationRecipient {
  email: string;
  firstName?: string;
  phone?: string;
}

export interface NotificationTemplate {
  subject: string;
  html: string;
  text: string;
}

export interface OrderNotificationContext {
  orderId: string;
  customerName?: string;
  totalCents?: number;
  trackingNumber?: string;
}

export interface EmailMessage {
  to: string;
  subject: string;
  html: string;
  text: string;
}

export interface EmailSendResult {
  success: boolean;
  providerMessageId?: string;
}
