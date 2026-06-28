import { prisma } from '@/lib/prisma';
import type { AuditLogInput } from '../types/audit.types';
import { logger } from '@/lib/monitoring/logger';

export class AuditService {
  static async log(input: AuditLogInput): Promise<void> {
    try {
      await prisma.auditLog.create({
        data: {
          action: input.action,
          entity: input.entity,
          entityId: input.entityId,
          userId: input.userId,
          ipAddress: input.ipAddress,
          payloadJson: input.payload ? JSON.stringify(input.payload) : undefined,
        },
      });
    } catch (error) {
      logger.error('Audit log write failed', error, {
        action: input.action,
        entity: input.entity,
        entityId: input.entityId,
      });
    }
  }
}
