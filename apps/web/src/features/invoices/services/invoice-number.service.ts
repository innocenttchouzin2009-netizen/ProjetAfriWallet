import { prisma } from '@/lib/prisma';
import { AuditService } from '@/features/audit/services/audit.service';

const INVOICE_PREFIX = 'INV';

function getYearMonth(date: Date): string {
  const year = date.getUTCFullYear();
  const month = String(date.getUTCMonth() + 1).padStart(2, '0');
  return `${year}${month}`;
}

function parseExistingSequence(invoiceNumber: string, prefix: string): number {
  if (!invoiceNumber.startsWith(prefix)) return 0;
  const sequence = Number(invoiceNumber.slice(prefix.length));
  return Number.isFinite(sequence) ? sequence : 0;
}

export class InvoiceNumberService {
  async getOrCreate(orderId: string): Promise<string> {
    const existingInvoiceLog = await prisma.auditLog.findFirst({
      where: {
        action: 'INVOICE_CREATED',
        entity: 'Invoice',
        entityId: orderId,
      },
      orderBy: {
        createdAt: 'desc',
      },
      select: {
        payloadJson: true,
      },
    });

    if (existingInvoiceLog?.payloadJson) {
      try {
        const payload = JSON.parse(existingInvoiceLog.payloadJson) as { invoiceNumber?: string };
        if (payload.invoiceNumber) {
          return payload.invoiceNumber;
        }
      } catch {
        // Ignore malformed historical payloads and regenerate.
      }
    }

    const now = new Date();
    const yearMonth = getYearMonth(now);
    const prefix = `${INVOICE_PREFIX}-${yearMonth}-`;

    const monthLogs = await prisma.auditLog.findMany({
      where: {
        action: 'INVOICE_CREATED',
        entity: 'Invoice',
      },
      select: {
        payloadJson: true,
      },
    });

    let maxSeq = 0;
    for (const log of monthLogs) {
      if (!log.payloadJson) continue;
      try {
        const payload = JSON.parse(log.payloadJson) as { invoiceNumber?: string };
        if (!payload.invoiceNumber) continue;
        const seq = parseExistingSequence(payload.invoiceNumber, prefix);
        if (seq > maxSeq) maxSeq = seq;
      } catch {
        continue;
      }
    }

    const nextSeq = String(maxSeq + 1).padStart(5, '0');
    const invoiceNumber = `${prefix}${nextSeq}`;

    await AuditService.log({
      action: 'INVOICE_CREATED',
      entity: 'Invoice',
      entityId: orderId,
      payload: {
        invoiceNumber,
        generatedAt: now.toISOString(),
      },
    });

    return invoiceNumber;
  }
}
