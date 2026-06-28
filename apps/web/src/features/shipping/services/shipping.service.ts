import { OrderStatus } from '@prisma/client';
import { prisma } from '@/lib/prisma';
import { AuditService } from '@/features/audit/services/audit.service';
import { NotificationService } from '@/features/notifications/services/notification.service';
import { MockShippingProvider } from '../providers/mock-shipping.provider';
import type { ShippingProvider } from '../providers/ShippingProvider';
import type { ShipmentResult, ShippingCarrier } from '../types/shipping.types';

export class ShippingService {
  constructor(
    private readonly provider: ShippingProvider = new MockShippingProvider(),
    private readonly notificationService: NotificationService = new NotificationService(),
  ) {}

  async shipOrder(orderId: string, carrier: ShippingCarrier): Promise<ShipmentResult> {
    const order = await prisma.order.findUnique({
      where: { id: orderId },
      include: { user: true, shippingAddress: true },
    });

    if (!order) {
      throw new Error('Order not found.');
    }

    if (order.status === OrderStatus.DELIVERED) {
      throw new Error('Delivered order cannot be shipped again.');
    }

    const shipment = await this.provider.createShipment({
      orderId,
      carrier,
      destinationCountry: order.shippingAddress?.country,
    });

    await prisma.order.update({
      where: { id: orderId },
      data: {
        status: OrderStatus.SHIPPED,
      },
    });

    await AuditService.log({
      action: 'SHIPPING_CREATED',
      entity: 'Shipping',
      entityId: orderId,
      payload: {
        carrier: shipment.carrier,
        trackingNumber: shipment.trackingNumber,
        shippingStatus: shipment.status,
        providerReference: shipment.providerReference,
      },
    });

    await AuditService.log({
      action: 'ORDER_STATUS_UPDATED',
      entity: 'Order',
      entityId: orderId,
      payload: {
        previousStatus: order.status,
        nextStatus: OrderStatus.SHIPPED,
        source: 'shipping_service',
      },
    });

    if (order.user?.email) {
      await this.notificationService.sendOrderStatusUpdate(
        'SHIPPED',
        {
          email: order.user.email,
          firstName: order.user.firstName,
        },
        {
          orderId: order.id,
          customerName: `${order.user.firstName} ${order.user.lastName}`.trim(),
          totalCents: order.totalCents,
          trackingNumber: shipment.trackingNumber,
        },
      );
    }

    return shipment;
  }
}
