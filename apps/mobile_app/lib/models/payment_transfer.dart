enum TransferStatus { created, authorized, processing, completed, cancelled, failed }

class SendTransferRequest {
  const SendTransferRequest({
    required this.payerId,
    required this.payeeId,
    required this.amountMinor,
    required this.currencyCode,
    required this.idempotencyKey,
  });

  final String payerId;
  final String payeeId;
  final int amountMinor;
  final String currencyCode;
  final String idempotencyKey;
}

class TransferReceipt {
  const TransferReceipt({
    required this.paymentIntentId,
    required this.status,
    required this.amountMinor,
    required this.currencyCode,
    required this.payeeId,
  });

  final String paymentIntentId;
  final TransferStatus status;
  final int amountMinor;
  final String currencyCode;
  final String payeeId;
}

class ReceiveIdentity {
  const ReceiveIdentity({required this.publicLabel, this.qrToken});

  final String publicLabel;
  final String? qrToken;

  bool get hasBackendQr => qrToken != null && qrToken!.trim().isNotEmpty;
}
