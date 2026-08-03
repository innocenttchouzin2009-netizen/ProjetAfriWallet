export type AuditEntity = 'Order' | 'Product' | 'User' | 'Auth' | 'Payment' | 'Shipping' | 'Invoice' | 'Inventory' | 'Settings' | 'Dashboard';

export type AuditAction =
  | 'ORDER_CREATED_ONLINE'
  | 'ORDER_CREATED_POS'
  | 'ORDER_STATUS_UPDATED'
  | 'PAYMENT_CHECKOUT_CREATED'
  | 'PAYMENT_WEBHOOK_PROCESSED'
  | 'PAYMENT_INIT_FAILED'
  | 'PAYMENT_CAPTURE_CONFIRMED'
  | 'PAYMENT_REFUND_REQUESTED'
  | 'PAYMENT_REFUNDED'
  | 'PAYMENT_REFUND_FAILED'
  | 'SHIPPING_CREATED'
  | 'INVOICE_CREATED'
  | 'DELIVERY_NOTE_CREATED'
  | 'INVOICE_EMAIL_SENT'
  | 'INVOICE_DOWNLOAD'
  | 'INVENTORY_ADJUSTED'
  | 'INVENTORY_MOVEMENT_RECORDED'
  | 'INVENTORY_LOW_STOCK_ALERT'
  | 'PRODUCT_CREATED'
  | 'PRODUCT_UPDATED'
  | 'PRODUCT_DELETED'
  | 'USER_REGISTERED'
  | 'ADMIN_SLA_THRESHOLD_UPDATED'
  | 'ADMIN_SLA_BREACH_ALERT';

export interface AuditLogInput {
  action: AuditAction;
  entity: AuditEntity;
  entityId?: string;
  userId?: string;
  ipAddress?: string;
  payload?: Record<string, unknown>;
}
