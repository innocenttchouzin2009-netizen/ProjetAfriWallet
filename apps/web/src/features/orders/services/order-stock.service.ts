import type { Prisma, ProductVariant } from '@prisma/client';
import { InventoryService } from '@/features/inventory/services/inventory.service';

export type OrderStockLineInput = {
  sku?: string;
  quantity: number;
  unitPrice: number;
  name?: string;
  customInitials?: string;
  customLogoUrl?: string;
};

export type OrderStockValidatedLine = {
  productVariantId: string;
  sku: string;
  quantity: number;
  unitPriceCents: number;
  totalPriceCents: number;
  customInitials?: string;
  customLogoUrl?: string;
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

    const normalized = items.map((item) => {
      const sku = item.sku?.trim();
      if (!sku) {
        throw new OrderStockError(`SKU requis pour l'article "${item.name ?? 'inconnu'}".`);
      }

      if (!Number.isFinite(item.quantity) || item.quantity <= 0) {
        throw new OrderStockError(`Quantite invalide pour le SKU ${sku}.`);
      }

      return {
        sku,
        quantity: item.quantity,
        unitPrice: item.unitPrice,
        customInitials: item.customInitials,
        customLogoUrl: item.customLogoUrl,
      };
    });

    const variants = await tx.productVariant.findMany({
      where: {
        sku: {
          in: Array.from(new Set(normalized.map((line) => line.sku))),
        },
      },
    });

    const bySku = new Map<string, ProductVariant>(variants.map((variant) => [variant.sku, variant]));

    const validated: OrderStockValidatedLine[] = [];

    for (const line of normalized) {
      const variant = bySku.get(line.sku);

      if (!variant) {
        throw new OrderStockError(`SKU introuvable: ${line.sku}.`);
      }

      if (!variant.isActive) {
        throw new OrderStockError(`SKU inactif: ${line.sku}.`);
      }

      if (variant.stock < line.quantity) {
        throw new OrderStockError(
          `Stock insuffisant pour ${line.sku}. Disponible: ${variant.stock}, demande: ${line.quantity}.`,
        );
      }

      const unitPriceCents = toCents(line.unitPrice > 0 ? line.unitPrice : variant.priceCents / 100);

      validated.push({
        productVariantId: variant.id,
        sku: line.sku,
        quantity: line.quantity,
        unitPriceCents,
        totalPriceCents: unitPriceCents * line.quantity,
        customInitials: line.customInitials,
        customLogoUrl: line.customLogoUrl,
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
