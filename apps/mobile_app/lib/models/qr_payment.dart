enum QrPaymentType { static, dynamic }

enum QrPaymentStatus {
  active,
  initiated,
  pendingConfirmation,
  paid,
  expired,
  invalid,
  unavailable,
}

class QrPaymentPayload {
  const QrPaymentPayload({
    required this.type,
    required this.merchantId,
    required this.amountMinor,
    required this.currencyCode,
    required this.merchantName,
    required this.description,
    this.qrId,
    this.expiresAt,
  });

  final QrPaymentType type;
  final String merchantId;
  final int amountMinor;
  final String currencyCode;
  final String merchantName;
  final String description;
  final String? qrId;
  final DateTime? expiresAt;

  bool get isExpired => expiresAt != null && !expiresAt!.isAfter(DateTime.now().toUtc());
}

class QrPaymentResult {
  const QrPaymentResult({
    required this.status,
    this.transferIntentId,
    this.receiptId,
    this.receiptCode,
    this.message,
  });

  final QrPaymentStatus status;
  final String? transferIntentId;
  final String? receiptId;
  final String? receiptCode;
  final String? message;

  bool get isFinanciallyConfirmed => status == QrPaymentStatus.paid;
}
