import { NextRequest, NextResponse } from 'next/server';
import { z } from 'zod';
import type { InvoiceDocumentType } from '@/features/invoices/types/invoice.types';
import { InvoicePdfService } from '@/features/invoices/services/invoice-pdf.service';
import { AuditService } from '@/features/audit/services/audit.service';

const invoiceService = new InvoicePdfService();
const InvoiceQuerySchema = z.object({
  document: z.enum(['INVOICE', 'DELIVERY_NOTE']).optional(),
});

type Params = {
  params: {
    id: string;
  };
};

function parseDocument(value: string | null): InvoiceDocumentType {
  return value === 'DELIVERY_NOTE' ? 'DELIVERY_NOTE' : 'INVOICE';
}

export async function GET(request: NextRequest, { params }: Params) {
  try {
    const { searchParams } = new URL(request.url);
    const parsed = InvoiceQuerySchema.safeParse({ document: searchParams.get('document') ?? undefined });
    if (!parsed.success) {
      return NextResponse.json({ message: parsed.error.issues.map((i) => i.message).join('; ') }, { status: 400 });
    }

    const documentType = parseDocument(parsed.data.document ?? null);

    const pdf = await invoiceService.generate(params.id, documentType);

    await AuditService.log({
      action: 'INVOICE_DOWNLOAD',
      entity: 'Invoice',
      entityId: params.id,
      payload: {
        documentType,
        source: 'account',
        fileName: pdf.fileName,
      },
    });

    return new NextResponse(new Uint8Array(pdf.buffer), {
      headers: {
        'Content-Type': pdf.contentType,
        'Content-Disposition': `attachment; filename="${pdf.fileName}"`,
      },
    });
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Unable to generate invoice PDF.';
    const status = message === 'Order not found.' ? 404 : 400;
    return NextResponse.json({ message }, { status });
  }
}
