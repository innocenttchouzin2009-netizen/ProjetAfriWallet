import { prisma } from '@/lib/prisma';
import { AuditService } from '@/features/audit/services/audit.service';
import { EmailProvider } from '@/features/notifications/providers/email.provider';
import { InvoiceNumberService } from './invoice-number.service';
import type { InvoiceDocumentType, InvoiceOrderSnapshot, InvoicePdfResult } from '../types/invoice.types';

function euro(cents: number): string {
  return `${(cents / 100).toFixed(2)} EUR`;
}

function escapePdfText(value: string): string {
  return value.replace(/\\/g, '\\\\').replace(/\(/g, '\\(').replace(/\)/g, '\\)');
}

function buildPdf(lines: string[]): Buffer {
  const contentStream = lines.join('\n');
  const contentLength = Buffer.byteLength(contentStream, 'utf8');

  const pdf = `%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n4 0 obj\n<< /Length ${contentLength} >>\nstream\n${contentStream}\nendstream\nendobj\n5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\ntrailer\n<< /Root 1 0 R /Size 6 >>\n%%EOF`;

  return Buffer.from(pdf, 'utf8');
}

export class InvoicePdfService {
  constructor(
    private readonly invoiceNumberService = new InvoiceNumberService(),
    private readonly emailProvider = new EmailProvider(),
  ) {}

  private async getOrderSnapshot(orderId: string): Promise<InvoiceOrderSnapshot> {
    const order = await prisma.order.findUnique({
      where: { id: orderId },
      include: {
        user: true,
        items: {
          include: {
            productVariant: {
              include: {
                product: true,
              },
            },
          },
        },
      },
    });

    if (!order) {
      throw new Error('Order not found.');
    }

    const invoiceNumber = await this.invoiceNumberService.getOrCreate(orderId);

    return {
      id: order.id,
      createdAtIso: order.createdAt.toISOString(),
      customerName: `${order.user.firstName} ${order.user.lastName}`.trim(),
      customerEmail: order.user.email,
      subtotalCents: order.subtotalCents,
      shippingCents: order.shippingCents,
      discountCents: order.discountCents,
      totalCents: order.totalCents,
      invoiceNumber,
      lines: order.items.map((item) => ({
        sku: item.productVariant.sku,
        name: item.productVariant.product.name,
        variantName: item.productVariant.name,
        quantity: item.quantity,
        unitPriceCents: item.unitPriceCents,
        totalPriceCents: item.totalPriceCents,
      })),
    };
  }

  async generate(orderId: string, documentType: InvoiceDocumentType): Promise<InvoicePdfResult> {
    const snapshot = await this.getOrderSnapshot(orderId);
    const title = documentType === 'INVOICE' ? 'FACTURE' : 'BON DE LIVRAISON';

    const bodyLines: string[] = [
      'BT',
      '/F1 12 Tf',
      '50 790 Td',
      `(${escapePdfText(`DOPE CUTE STUDIO - ${title}`)}) Tj`,
      '0 -18 Td',
      `(${escapePdfText(`Numero facture: ${snapshot.invoiceNumber}`)}) Tj`,
      '0 -18 Td',
      `(${escapePdfText(`Commande: ${snapshot.id}`)}) Tj`,
      '0 -18 Td',
      `(${escapePdfText(`Client: ${snapshot.customerName} (${snapshot.customerEmail})`)}) Tj`,
      '0 -18 Td',
      `(${escapePdfText(`Date commande: ${snapshot.createdAtIso}`)}) Tj`,
      '0 -28 Td',
      `(${escapePdfText('Lignes')}) Tj`,
    ];

    for (const line of snapshot.lines) {
      bodyLines.push('0 -16 Td');
      bodyLines.push(
        `(${escapePdfText(`${line.sku} | ${line.name} ${line.variantName} | x${line.quantity} | ${euro(line.totalPriceCents)}`)}) Tj`,
      );
    }

    if (documentType === 'INVOICE') {
      bodyLines.push('0 -26 Td');
      bodyLines.push(`(${escapePdfText(`Sous-total: ${euro(snapshot.subtotalCents)}`)}) Tj`);
      bodyLines.push('0 -16 Td');
      bodyLines.push(`(${escapePdfText(`Livraison: ${euro(snapshot.shippingCents)}`)}) Tj`);
      bodyLines.push('0 -16 Td');
      bodyLines.push(`(${escapePdfText(`Remise: -${euro(snapshot.discountCents)}`)}) Tj`);
      bodyLines.push('0 -16 Td');
      bodyLines.push(`(${escapePdfText(`Total: ${euro(snapshot.totalCents)}`)}) Tj`);
    }

    bodyLines.push('ET');

    if (documentType === 'DELIVERY_NOTE') {
      await AuditService.log({
        action: 'DELIVERY_NOTE_CREATED',
        entity: 'Invoice',
        entityId: orderId,
        payload: {
          invoiceNumber: snapshot.invoiceNumber,
        },
      });
    }

    return {
      fileName: `${documentType === 'INVOICE' ? 'facture' : 'bon-livraison'}-${snapshot.invoiceNumber}.pdf`,
      contentType: 'application/pdf',
      buffer: buildPdf(bodyLines),
    };
  }

  async emailInvoice(orderId: string): Promise<void> {
    const snapshot = await this.getOrderSnapshot(orderId);

    if (!snapshot.customerEmail) {
      return;
    }

    const publicUrl = process.env.NEXT_PUBLIC_APP_URL ?? 'http://localhost:3000';
    const downloadUrl = `${publicUrl}/api/account/orders/${orderId}/invoice?document=INVOICE`;

    await this.emailProvider.send({
      to: snapshot.customerEmail,
      subject: `Facture ${snapshot.invoiceNumber} - commande ${snapshot.id}`,
      text: [
        `Bonjour ${snapshot.customerName},`,
        '',
        `Votre paiement pour la commande ${snapshot.id} est confirme.`,
        `Votre facture numero ${snapshot.invoiceNumber} est disponible au lien suivant:`,
        downloadUrl,
      ].join('\n'),
      html: [
        `<p>Bonjour ${snapshot.customerName},</p>`,
        `<p>Votre paiement pour la commande <strong>${snapshot.id}</strong> est confirme.</p>`,
        `<p>Votre facture numero <strong>${snapshot.invoiceNumber}</strong> est disponible:</p>`,
        `<p><a href="${downloadUrl}">Telecharger la facture PDF</a></p>`,
      ].join(''),
    });

    await AuditService.log({
      action: 'INVOICE_EMAIL_SENT',
      entity: 'Invoice',
      entityId: orderId,
      payload: {
        invoiceNumber: snapshot.invoiceNumber,
        to: snapshot.customerEmail,
        downloadUrl,
      },
    });
  }
}
