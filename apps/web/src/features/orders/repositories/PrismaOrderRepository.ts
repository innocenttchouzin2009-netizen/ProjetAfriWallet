import { OrderStatus } from '@prisma/client';
import { prisma } from '@/lib/prisma';
import {
  OrderStockError,
  OrderStockService,
  type OrderStockValidatedLine,
} from '@/features/orders/services/order-stock.service';

type OrderLineInput = {
  name: string;
  quantity: number;
  unitPrice: number;
  sku?: string;
  customInitials?: string;
  customLogoUrl?: string;
};

type OnlineOrderInput = {
  customer: {
    firstName: string;
    lastName: string;
    email: string;
  };
  address: {
    address: string;
    city: string;
    postalCode: string;
    country: string;
  };
  items: OrderLineInput[];
  shippingCents: number;
  discountCents?: number;
};

type POSOrderInput = {
  cashierEmail?: string;
  items: OrderLineInput[];
  discountCents?: number;
  paymentMethod: 'cash' | 'card';
};

type UnifiedOrderFilter = {
  limit?: number;
  status?: OrderStatus;
  channel?: 'online' | 'pos';
};

export class OrderValidationError extends OrderStockError {}

function toCents(value: number): number {
  return Math.round(value * 100);
}

export class PrismaOrderRepository {
  private toOrderItemsCreateData(lines: OrderStockValidatedLine[]) {
    return lines.map((line) => ({
      productVariantId: line.productVariantId,
      quantity: line.quantity,
      unitPriceCents: line.unitPriceCents,
      totalPriceCents: line.totalPriceCents,
      customInitials: line.customInitials,
      customLogoUrl: line.customLogoUrl,
    }));
  }

  async createOnlineOrder(input: OnlineOrderInput): Promise<{ id: string }> {
    const subtotalFromInputCents = input.items.reduce((sum, item) => sum + toCents(item.unitPrice) * item.quantity, 0);
    const discountCents = input.discountCents ?? 0;
    const shippingCents = input.shippingCents;

    const user = await prisma.user.upsert({
      where: { email: input.customer.email.trim().toLowerCase() },
      update: {
        firstName: input.customer.firstName,
        lastName: input.customer.lastName,
      },
      create: {
        email: input.customer.email.trim().toLowerCase(),
        passwordHash: 'not-set',
        firstName: input.customer.firstName,
        lastName: input.customer.lastName,
        role: 'CLIENT',
      },
    });

    const address = await prisma.address.create({
      data: {
        userId: user.id,
        label: 'Livraison',
        line1: input.address.address,
        city: input.address.city,
        postalCode: input.address.postalCode,
        country: input.address.country,
      },
    });

    const order = await prisma.$transaction(async (tx) => {
      const lineItems: OrderStockValidatedLine[] = await OrderStockService.validateAndDecrementInTransaction(
        tx,
        input.items,
        'ONLINE',
      );
      const subtotalCents = lineItems.reduce((sum, item) => sum + item.totalPriceCents, 0) || subtotalFromInputCents;
      const totalCents = subtotalCents - discountCents + shippingCents;

      return tx.order.create({
        data: {
          userId: user.id,
          shippingAddressId: address.id,
          status: OrderStatus.CONFIRMED,
          subtotalCents,
          discountCents,
          shippingCents,
          totalCents,
          paymentMethod: 'card',
          paymentReference: `ONLINE-${Date.now()}`,
          items: {
            create: this.toOrderItemsCreateData(lineItems),
          },
        },
      });
    });

    return { id: order.id };
  }

  async createPOSOrder(input: POSOrderInput): Promise<{ id: string }> {
    const subtotalFromInputCents = input.items.reduce((sum, item) => sum + toCents(item.unitPrice) * item.quantity, 0);
    const discountCents = input.discountCents ?? 0;

    const cashier = await prisma.user.upsert({
      where: { email: (input.cashierEmail ?? 'pos@dopecute.studio').trim().toLowerCase() },
      update: {},
      create: {
        email: (input.cashierEmail ?? 'pos@dopecute.studio').trim().toLowerCase(),
        passwordHash: 'not-set',
        firstName: 'POS',
        lastName: 'Cashier',
        role: 'VENDOR',
      },
    });

    const order = await prisma.$transaction(async (tx) => {
      const lineItems: OrderStockValidatedLine[] = await OrderStockService.validateAndDecrementInTransaction(
        tx,
        input.items,
        'POS',
      );
      const subtotalCents = lineItems.reduce((sum, item) => sum + item.totalPriceCents, 0) || subtotalFromInputCents;
      const totalCents = Math.max(0, subtotalCents - discountCents);

      return tx.order.create({
        data: {
          userId: cashier.id,
          status: OrderStatus.READY,
          subtotalCents,
          discountCents,
          shippingCents: 0,
          totalCents,
          paymentMethod: input.paymentMethod,
          paymentReference: `POS-${Date.now()}`,
          items: {
            create: this.toOrderItemsCreateData(lineItems),
          },
        },
      });
    });

    return { id: order.id };
  }

  async findRecentOrders(filters: UnifiedOrderFilter = {}) {
    const where: {
      status?: OrderStatus;
      paymentReference?: { startsWith: string };
    } = {};

    if (filters.status) {
      where.status = filters.status;
    }

    if (filters.channel === 'online') {
      where.paymentReference = { startsWith: 'ONLINE-' };
    }

    if (filters.channel === 'pos') {
      where.paymentReference = { startsWith: 'POS-' };
    }

    const orders = await prisma.order.findMany({
      where,
      include: {
        user: true,
      },
      orderBy: {
        createdAt: 'desc',
      },
      take: filters.limit ?? 10,
    });

    return orders.map((order) => {
      const channel = order.paymentReference?.startsWith('POS-') ? 'Boutique' : 'Online';
      const statusLabelMap: Record<OrderStatus, string> = {
        DRAFT: 'Brouillon',
        CONFIRMED: 'Confirmee',
        IN_PRODUCTION: 'En production',
        READY: 'Prete',
        SHIPPED: 'Expediee',
        DELIVERED: 'Livree',
        CANCELED: 'Annulee',
      };

      return {
        id: order.id,
        customer: `${order.user.firstName} ${order.user.lastName}`.trim() || order.user.email,
        channel,
        total: `${(order.totalCents / 100).toFixed(2)} €`,
        status: statusLabelMap[order.status],
        statusCode: order.status,
        createdAt: order.createdAt.toISOString(),
      };
    });
  }
}
