import { prisma } from '@/lib/prisma';
import { AuditService } from '@/features/audit/services/audit.service';
import type {
  AdjustInventoryInput,
  InventoryItem,
  InventoryMovement,
  InventoryMovementSource,
  InventoryMovementType,
  InventoryOverview,
} from '../types/inventory.types';

const DEFAULT_LOW_STOCK_THRESHOLD = 10;

type MovementPayload = {
  variantId?: string;
  sku?: string;
  quantityDelta?: number;
  type?: InventoryMovementType;
  source?: InventoryMovementSource;
  reason?: string;
};

function parseMovementPayload(payloadJson: string | null): MovementPayload | null {
  if (!payloadJson) return null;

  try {
    return JSON.parse(payloadJson) as MovementPayload;
  } catch {
    return null;
  }
}

export class InventoryService {
  async getOverview(limitPerItem = 6): Promise<InventoryOverview> {
    const variants = await prisma.productVariant.findMany({
      include: {
        product: true,
      },
      orderBy: {
        createdAt: 'desc',
      },
    });

    const variantIds = variants.map((variant) => variant.id);

    const movementLogs = variantIds.length
      ? await prisma.auditLog.findMany({
          where: {
            action: 'INVENTORY_MOVEMENT_RECORDED',
            entity: 'Inventory',
            entityId: { in: variantIds },
          },
          orderBy: {
            createdAt: 'desc',
          },
        })
      : [];

    const movementsByVariant = new Map<string, InventoryMovement[]>();

    for (const log of movementLogs) {
      if (!log.entityId) continue;

      const payload = parseMovementPayload(log.payloadJson);
      if (!payload?.sku || !payload.quantityDelta || !payload.type || !payload.source) {
        continue;
      }

      const existing = movementsByVariant.get(log.entityId) ?? [];
      if (existing.length >= limitPerItem) {
        continue;
      }

      existing.push({
        id: log.id,
        variantId: log.entityId,
        sku: payload.sku,
        quantityDelta: payload.quantityDelta,
        type: payload.type,
        source: payload.source,
        reason: payload.reason,
        createdAt: log.createdAt.toISOString(),
      });

      movementsByVariant.set(log.entityId, existing);
    }

    const items: InventoryItem[] = variants.map((variant) => {
      const lowStockThreshold = DEFAULT_LOW_STOCK_THRESHOLD;

      return {
        variantId: variant.id,
        productId: variant.productId,
        productName: variant.product.name,
        variantName: variant.name,
        sku: variant.sku,
        stock: variant.stock,
        lowStockThreshold,
        lowStockAlert: variant.stock <= lowStockThreshold,
        recentMovements: movementsByVariant.get(variant.id) ?? [],
      };
    });

    const lowStockCount = items.filter((item) => item.lowStockAlert).length;
    const totalStockUnits = items.reduce((sum, item) => sum + item.stock, 0);

    return {
      items,
      lowStockCount,
      totalStockUnits,
    };
  }

  async adjustStock(input: AdjustInventoryInput) {
    if (!Number.isInteger(input.quantityDelta) || input.quantityDelta === 0) {
      throw new Error('quantityDelta must be a non-zero integer.');
    }

    const variant = await prisma.productVariant.findUnique({
      where: {
        id: input.variantId,
      },
      include: {
        product: true,
      },
    });

    if (!variant) {
      throw new Error('Variant not found.');
    }

    const nextStock = variant.stock + input.quantityDelta;
    if (nextStock < 0) {
      throw new Error(`Negative stock blocked for SKU ${variant.sku}. Current=${variant.stock}, delta=${input.quantityDelta}.`);
    }

    const updated = await prisma.productVariant.update({
      where: {
        id: variant.id,
      },
      data: {
        stock: nextStock,
      },
      include: {
        product: true,
      },
    });

    const source: InventoryMovementSource = input.source ?? 'ADMIN';
    const movementType: InventoryMovementType = input.quantityDelta > 0 ? 'IN' : 'ADJUSTMENT';

    await AuditService.log({
      action: 'INVENTORY_ADJUSTED',
      entity: 'Inventory',
      entityId: updated.id,
      payload: {
        variantId: updated.id,
        sku: updated.sku,
        quantityDelta: input.quantityDelta,
        previousStock: variant.stock,
        nextStock,
        type: movementType,
        source,
        reason: input.reason,
      },
    });

    await AuditService.log({
      action: 'INVENTORY_MOVEMENT_RECORDED',
      entity: 'Inventory',
      entityId: updated.id,
      payload: {
        variantId: updated.id,
        sku: updated.sku,
        quantityDelta: input.quantityDelta,
        type: movementType,
        source,
        reason: input.reason,
      },
    });

    if (nextStock <= DEFAULT_LOW_STOCK_THRESHOLD) {
      await AuditService.log({
        action: 'INVENTORY_LOW_STOCK_ALERT',
        entity: 'Inventory',
        entityId: updated.id,
        payload: {
          sku: updated.sku,
          stock: nextStock,
          threshold: DEFAULT_LOW_STOCK_THRESHOLD,
        },
      });
    }

    return {
      variantId: updated.id,
      sku: updated.sku,
      stock: updated.stock,
      lowStockThreshold: DEFAULT_LOW_STOCK_THRESHOLD,
      lowStockAlert: updated.stock <= DEFAULT_LOW_STOCK_THRESHOLD,
    };
  }

  static async recordSaleMovement(
    tx: { auditLog: { create: (args: { data: { action: string; entity: string; entityId: string; payloadJson: string } }) => Promise<unknown> } },
    input: {
      variantId: string;
      sku: string;
      quantityDelta: number;
      source: 'ONLINE' | 'POS';
      reason?: string;
    },
  ) {
    await tx.auditLog.create({
      data: {
        action: 'INVENTORY_MOVEMENT_RECORDED',
        entity: 'Inventory',
        entityId: input.variantId,
        payloadJson: JSON.stringify({
          variantId: input.variantId,
          sku: input.sku,
          quantityDelta: input.quantityDelta,
          type: 'OUT',
          source: input.source,
          reason: input.reason,
        }),
      },
    });
  }
}
