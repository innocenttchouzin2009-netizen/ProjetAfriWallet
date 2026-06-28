import { describe, expect, it, vi } from 'vitest';
import { OrderStatus } from '@prisma/client';
import { PaymentService } from './payment.service';
import { AuditService } from '@/features/audit/services/audit.service';

describe('PaymentService.processWebhook', () => {
  it('ignores duplicate webhook events (idempotent)', async () => {
    const prismaClient = {
      auditLog: {
        findFirst: vi.fn().mockResolvedValue({ id: 'existing' }),
      },
      order: {
        findUnique: vi.fn(),
        update: vi.fn(),
      },
    } as unknown as any;

    const providerFactory = vi.fn().mockReturnValue({
      parseWebhookEvent: vi.fn().mockResolvedValue({
        provider: 'stripe',
        eventId: 'evt_1',
        orderId: 'ord_1',
        paymentReference: 'pi_1',
        status: 'succeeded',
        rawType: 'payment_intent.succeeded',
      }),
    });

    const service = new PaymentService({ prismaClient, providerFactory });

    await service.processWebhook('stripe', '{}', new Headers());

    expect(prismaClient.auditLog.findFirst).toHaveBeenCalledOnce();
    expect(prismaClient.order.findUnique).not.toHaveBeenCalled();
    expect(prismaClient.order.update).not.toHaveBeenCalled();
  });

  it('confirms draft orders on succeeded webhook', async () => {
    const prismaClient = {
      auditLog: {
        findFirst: vi.fn().mockResolvedValue(null),
      },
      order: {
        findUnique: vi.fn().mockResolvedValue({
          id: 'ord_1',
          status: OrderStatus.DRAFT,
          paymentReference: 'pi_1',
          totalCents: 2500,
          user: {
            email: 'client@example.com',
            firstName: 'Ada',
            lastName: 'Lovelace',
          },
        }),
        update: vi.fn().mockResolvedValue({}),
      },
    } as unknown as any;

    const sendOrderConfirmed = vi.fn().mockResolvedValue(undefined);
    const emailInvoice = vi.fn().mockResolvedValue(undefined);

    const providerFactory = vi.fn().mockReturnValue({
      parseWebhookEvent: vi.fn().mockResolvedValue({
        provider: 'stripe',
        eventId: 'evt_2',
        orderId: 'ord_1',
        paymentReference: 'pi_1',
        status: 'succeeded',
        rawType: 'payment_intent.succeeded',
      }),
    });

    const service = new PaymentService({
      prismaClient,
      providerFactory,
      notificationService: { sendOrderConfirmed } as unknown as any,
      invoicePdfService: { emailInvoice } as unknown as any,
    });

    await service.processWebhook('stripe', '{}', new Headers());

    expect(prismaClient.order.update).toHaveBeenCalledWith(
      expect.objectContaining({
        where: { id: 'ord_1' },
        data: expect.objectContaining({ status: OrderStatus.CONFIRMED }),
      }),
    );
    expect(sendOrderConfirmed).toHaveBeenCalledOnce();
    expect(emailInvoice).toHaveBeenCalledWith('ord_1');
  });

  it('cancels draft/confirmed orders on failed webhook', async () => {
    const prismaClient = {
      auditLog: {
        findFirst: vi.fn().mockResolvedValue(null),
      },
      order: {
        findUnique: vi.fn().mockResolvedValue({
          id: 'ord_2',
          status: OrderStatus.CONFIRMED,
          paymentReference: 'pi_2',
          totalCents: 1000,
          user: {
            email: 'client@example.com',
            firstName: 'Grace',
            lastName: 'Hopper',
          },
        }),
        update: vi.fn().mockResolvedValue({}),
      },
    } as unknown as any;

    const providerFactory = vi.fn().mockReturnValue({
      parseWebhookEvent: vi.fn().mockResolvedValue({
        provider: 'stripe',
        eventId: 'evt_3',
        orderId: 'ord_2',
        paymentReference: 'pi_2',
        status: 'failed',
        rawType: 'payment_intent.payment_failed',
      }),
    });

    const service = new PaymentService({
      prismaClient,
      providerFactory,
      notificationService: { sendOrderConfirmed: vi.fn() } as unknown as any,
    });

    await service.processWebhook('stripe', '{}', new Headers());

    expect(prismaClient.order.update).toHaveBeenCalledWith(
      expect.objectContaining({
        where: { id: 'ord_2' },
        data: expect.objectContaining({ status: OrderStatus.CANCELED }),
      }),
    );
  });
});

describe('PaymentService.confirmPaypalCapture', () => {
  it('confirms order when PayPal capture succeeds', async () => {
    const prismaClient = {
      order: {
        findUnique: vi.fn().mockResolvedValue({
          id: 'ord_3',
          status: OrderStatus.DRAFT,
          paymentReference: 'pp_old',
          totalCents: 5400,
          user: {
            email: 'client@example.com',
            firstName: 'Linus',
            lastName: 'Torvalds',
          },
        }),
        update: vi.fn().mockResolvedValue({ id: 'ord_3', status: OrderStatus.CONFIRMED }),
      },
      auditLog: {
        findFirst: vi.fn(),
      },
    } as unknown as any;

    const sendOrderConfirmed = vi.fn().mockResolvedValue(undefined);
    const emailInvoice = vi.fn().mockResolvedValue(undefined);
    const paypalProvider = {
      captureOrder: vi.fn().mockResolvedValue({
        reference: 'pp_cap_1',
        status: 'succeeded',
      }),
    } as unknown as any;

    const service = new PaymentService({
      prismaClient,
      notificationService: { sendOrderConfirmed } as unknown as any,
      invoicePdfService: { emailInvoice } as unknown as any,
      paypalProvider,
    });

    const result = await service.confirmPaypalCapture('ord_3', 'paypal_order_1');

    expect(result).toEqual({ orderId: 'ord_3', status: OrderStatus.CONFIRMED });
    expect(prismaClient.order.update).toHaveBeenCalledWith(
      expect.objectContaining({
        where: { id: 'ord_3' },
        data: expect.objectContaining({
          status: OrderStatus.CONFIRMED,
          paymentMethod: 'paypal',
          paymentReference: 'pp_cap_1',
        }),
      }),
    );
    expect(sendOrderConfirmed).toHaveBeenCalledOnce();
    expect(emailInvoice).toHaveBeenCalledWith('ord_3');
  });
});

describe('PaymentService.createRefund', () => {
  it('rejects refund that exceeds remaining refundable amount', async () => {
    vi.spyOn(AuditService, 'log').mockResolvedValue(undefined);

    const prismaClient = {
      order: {
        findUnique: vi.fn().mockResolvedValue({
          id: 'ord_4',
          totalCents: 1000,
          paymentReference: 'pi_4',
          paymentMethod: 'stripe',
        }),
      },
      auditLog: {
        findMany: vi.fn().mockResolvedValue([
          {
            payloadJson: JSON.stringify({ amountCents: 700 }),
          },
        ]),
      },
    } as unknown as any;

    const providerFactory = vi.fn().mockReturnValue({
      createRefund: vi.fn(),
    });

    const service = new PaymentService({ prismaClient, providerFactory });

    await expect(service.createRefund({ orderId: 'ord_4', amountCents: 400 })).rejects.toThrow(
      'Refund amount exceeds remaining refundable amount for this order.',
    );

    expect(providerFactory).not.toHaveBeenCalled();
  });

  it('uses provider from order and processes valid refund', async () => {
    vi.spyOn(AuditService, 'log').mockResolvedValue(undefined);

    const createRefund = vi.fn().mockResolvedValue({
      provider: 'stripe',
      reference: 're_1',
      status: 'succeeded',
    });

    const prismaClient = {
      order: {
        findUnique: vi.fn().mockResolvedValue({
          id: 'ord_5',
          totalCents: 2000,
          paymentReference: 'pi_5',
          paymentMethod: 'stripe',
        }),
      },
      auditLog: {
        findMany: vi.fn().mockResolvedValue([
          {
            payloadJson: JSON.stringify({ amountCents: 500 }),
          },
        ]),
      },
    } as unknown as any;

    const providerFactory = vi.fn().mockReturnValue({
      createRefund,
    });

    const service = new PaymentService({ prismaClient, providerFactory });

    const result = await service.createRefund({ orderId: 'ord_5', amountCents: 1000, reason: 'Customer request' });

    expect(providerFactory).toHaveBeenCalledWith('stripe');
    expect(createRefund).toHaveBeenCalledWith(
      expect.objectContaining({
        orderId: 'ord_5',
        amountCents: 1000,
        paymentReference: 'pi_5',
      }),
    );
    expect(result).toEqual({ provider: 'stripe', reference: 're_1', status: 'succeeded' });
  });
});
