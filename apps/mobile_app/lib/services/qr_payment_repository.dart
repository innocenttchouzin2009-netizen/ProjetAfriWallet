import '../models/qr_payment.dart';

abstract interface class QrPaymentRepository {
  Future<QrPaymentPayload> decodeAndValidate(String rawCode);

  Future<QrPaymentResult> initiatePayment({
    required QrPaymentPayload payload,
    required String payerWalletId,
  });

  Future<QrPaymentResult> getAuthoritativeStatus(String transferIntentId);
}

class QrPaymentUnavailableException implements Exception {
  const QrPaymentUnavailableException(this.message);

  final String message;

  @override
  String toString() => message;
}

class InvalidQrPaymentException implements Exception {
  const InvalidQrPaymentException(this.message);

  final String message;

  @override
  String toString() => message;
}

class UnavailableQrPaymentRepository implements QrPaymentRepository {
  const UnavailableQrPaymentRepository();

  static const _message =
      'QR payments are unavailable. No payment result is simulated.';

  @override
  Future<QrPaymentPayload> decodeAndValidate(String rawCode) {
    if (rawCode.trim().isEmpty) {
      return Future<QrPaymentPayload>.error(
        const InvalidQrPaymentException('The QR code is empty.'),
      );
    }

    return Future<QrPaymentPayload>.error(
      const QrPaymentUnavailableException(_message),
    );
  }

  @override
  Future<QrPaymentResult> initiatePayment({
    required QrPaymentPayload payload,
    required String payerWalletId,
  }) {
    return Future<QrPaymentResult>.error(
      const QrPaymentUnavailableException(_message),
    );
  }

  @override
  Future<QrPaymentResult> getAuthoritativeStatus(String transferIntentId) {
    return Future<QrPaymentResult>.error(
      const QrPaymentUnavailableException(_message),
    );
  }
}
