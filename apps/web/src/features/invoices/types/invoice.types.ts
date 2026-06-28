export type InvoiceDocumentType = 'INVOICE' | 'DELIVERY_NOTE';

export interface InvoiceLine {
  sku: string;
  name: string;
  variantName: string;
  quantity: number;
  unitPriceCents: number;
  totalPriceCents: number;
}

export interface InvoiceOrderSnapshot {
  id: string;
  createdAtIso: string;
  customerName: string;
  customerEmail: string;
  subtotalCents: number;
  shippingCents: number;
  discountCents: number;
  totalCents: number;
  invoiceNumber: string;
  lines: InvoiceLine[];
}

export interface InvoicePdfResult {
  fileName: string;
  contentType: 'application/pdf';
  buffer: Buffer;
}

export interface InvoiceIssueResult {
  orderId: string;
  invoiceNumber: string;
}
