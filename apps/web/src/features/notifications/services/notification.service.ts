import type { NotificationOrderStatus, NotificationRecipient, OrderNotificationContext } from '../types/notification.types';
import { EmailProvider } from '../providers/email.provider';
import { orderConfirmedTemplate } from '../templates/order-confirmed';
import { orderInProductionTemplate } from '../templates/order-in-production';
import { orderShippedTemplate } from '../templates/order-shipped';
import { orderDeliveredTemplate } from '../templates/order-delivered';
import { welcomeTemplate } from '../templates/welcome';

export class NotificationService {
  constructor(private readonly emailProvider = new EmailProvider()) {}

  async sendWelcome(recipient: NotificationRecipient): Promise<void> {
    const template = welcomeTemplate(recipient.firstName);

    await this.emailProvider.send({
      to: recipient.email,
      subject: template.subject,
      html: template.html,
      text: template.text,
    });
  }

  async sendOrderConfirmed(recipient: NotificationRecipient, context: OrderNotificationContext): Promise<void> {
    const template = orderConfirmedTemplate({
      orderId: context.orderId,
      customerName: context.customerName,
      totalCents: context.totalCents,
    });

    await this.emailProvider.send({
      to: recipient.email,
      subject: template.subject,
      html: template.html,
      text: template.text,
    });
  }

  async sendOrderStatusUpdate(
    status: NotificationOrderStatus,
    recipient: NotificationRecipient,
    context: OrderNotificationContext,
  ): Promise<void> {
    const template =
      status === 'CONFIRMED'
        ? orderConfirmedTemplate({
            orderId: context.orderId,
            customerName: context.customerName,
            totalCents: context.totalCents,
          })
        : status === 'IN_PRODUCTION'
          ? orderInProductionTemplate({
              orderId: context.orderId,
              customerName: context.customerName,
            })
          : status === 'SHIPPED'
            ? orderShippedTemplate({
                orderId: context.orderId,
                customerName: context.customerName,
                trackingNumber: context.trackingNumber,
              })
            : orderDeliveredTemplate({
                orderId: context.orderId,
                customerName: context.customerName,
              });

    await this.emailProvider.send({
      to: recipient.email,
      subject: template.subject,
      html: template.html,
      text: template.text,
    });
  }
}
