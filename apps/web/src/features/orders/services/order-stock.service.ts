import type { Prisma, ProductVariant } from '@prisma/client';
import { InventoryService } from '@/features/inventory/services/inventory.service';

export type OrderStockLineInput = {
  sku?: string;
  quantity: number;
  unitPrice: number;
  name?: string;
};

export type OrderStockValidatedLine = {
  productVariantId: string;
  sku: string;
  quantity: number;
  unitPriceCents: number;
  totalPriceCents: number;
};

export class OrderStockError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'OrderStockError';
  }
}

function toCents(value: number): number {
  return Math.round(value * 100);
}

export class OrderStockService {
  static async validateAndDecrementInTransaction(
    tx: Prisma.TransactionClient,
    items: OrderStockLineInput[],
    source: 'ONLINE' | 'POS',
  ): Promise<OrderStockValidatedLine[]> {
    if (!items.length) {
      throw new OrderStockError('La commande doit contenir au moins un article.');
    }

    const normalized = new Map<string, { quantity: number; unitPrice: number; name?: string }>();

    for (const item of items) {
      const sku = item.sku?.trim();
      if (!sku) {
        throw new OrderStockError(`SKU requis pour l'article "${item.name ?? 'inconnu'}".`);
      }

      if (!Number.isFinite(item.quantity) || item.quantity <= 0) {
        throw new OrderStockError(`Quantite invalide pour le SKU ${sku}.`);
      }

      const current = normalized.get(sku);
      if (current) {
        current.quantity += item.quantity;
      } else {
        normalized.set(sku, {
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          name: item.name,
        });
      }
    }

    const variants = await tx.productVariant.findMany({
      where: {
        sku: {
          in: Array.from(normalized.keys()),
        },
      },
    });

    const bySku = new Map<string, ProductVariant>(variants.map((variant) => [variant.sku, variant]));

    const validated: OrderStockValidatedLine[] = [];

    for (const [sku, line] of normalized.entries()) {
      const variant = bySku.get(sku);

      if (!variant) {
        throw new OrderStockError(`SKU introuvable: ${sku}.`);
      }

      if (!variant.isActive) {
        throw new OrderStockError(`SKU inactif: ${sku}.`);
      }

      if (variant.stock < line.quantity) {
        throw new OrderStockError(
          `Stock insuffisant pour ${sku}. Disponible: ${variant.stock}, demande: ${line.quantity}.`,
        );
      }

      const unitPriceCents = toCents(line.unitPrice > 0 ? line.unitPrice : variant.priceCents / 100);

      validated.push({
        productVariantId: variant.id,
        sku,
        quantity: line.quantity,
        unitPriceCents,
        totalPriceCents: unitPriceCents * line.quantity,
      });
    }

    for (const line of validated) {
      const updated = await tx.productVariant.updateMany({
        where: {
          id: line.productVariantId,
          stock: {
            gte: line.quantity,
          },
        },
        data: {
          stock: {
            decrement: line.quantity,
          },
        },
      });

      if (updated.count !== 1) {
        throw new OrderStockError('Stock insuffisant detecte pendant la validation de commande.');
      }

      await InventoryService.recordSaleMovement(tx, {
        variantId: line.productVariantId,
        sku: line.sku,
        quantityDelta: -line.quantity,
        source,
        reason: source === 'POS' ? 'POS checkout' : 'Online checkout',
      });
    }

    return validated;
  }
}
