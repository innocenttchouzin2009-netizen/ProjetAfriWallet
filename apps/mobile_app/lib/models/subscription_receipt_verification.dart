enum SubscriptionReceiptVerificationStatus {
  success,
}

class SubscriptionReceiptVerificationPayload {
  const SubscriptionReceiptVerificationPayload({
    required this.version,
    required this.invoiceId,
    required this.paymentReference,
    required this.status,
    required this.issuedAtEpochSeconds,
    required this.expiresAtEpochSeconds,
  });

  final int version;
  final String invoiceId;
  final String paymentReference;
  final SubscriptionReceiptVerificationStatus status;
  final int issuedAtEpochSeconds;
  final int expiresAtEpochSeconds;

  bool isExpiredAt(DateTime now) {
    return now.toUtc().millisecondsSinceEpoch ~/ 1000 > expiresAtEpochSeconds;
  }
}

class InvalidSubscriptionReceiptVerificationException implements Exception {
  const InvalidSubscriptionReceiptVerificationException(this.message);

  final String message;

  @override
  String toString() => message;
}
