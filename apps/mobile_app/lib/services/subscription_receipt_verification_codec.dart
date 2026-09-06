import '../models/subscription_receipt_verification.dart';

class SubscriptionReceiptVerificationCodec {
  const SubscriptionReceiptVerificationCodec();

  static const String _brand = 'AFW';
  static const String _type = 'RECEIPT';
  static const int currentVersion = 1;

  String encode(SubscriptionReceiptVerificationPayload payload) {
    if (payload.version != currentVersion) {
      throw const InvalidSubscriptionReceiptVerificationException(
        'Unsupported receipt verification version.',
      );
    }
    if (payload.invoiceId.trim().isEmpty || payload.paymentReference.trim().isEmpty) {
      throw const InvalidSubscriptionReceiptVerificationException(
        'Receipt verification identifiers are required.',
      );
    }
    if (payload.expiresAtEpochSeconds <= payload.issuedAtEpochSeconds) {
      throw const InvalidSubscriptionReceiptVerificationException(
        'Receipt verification expiry must be after issuance.',
      );
    }

    return [
      _brand,
      _type,
      payload.version.toString(),
      Uri.encodeComponent(payload.invoiceId.trim()),
      Uri.encodeComponent(payload.paymentReference.trim()),
      _statusToken(payload.status),
      payload.issuedAtEpochSeconds.toString(),
      payload.expiresAtEpochSeconds.toString(),
    ].join('|');
  }

  SubscriptionReceiptVerificationPayload decode(String rawCode) {
    final code = rawCode.trim();
    final parts = code.split('|');
    if (parts.length != 8 || parts[0] != _brand || parts[1] != _type) {
      throw const InvalidSubscriptionReceiptVerificationException(
        'This is not a valid AfWal receipt verification code.',
      );
    }

    final version = int.tryParse(parts[2]);
    if (version != currentVersion) {
      throw const InvalidSubscriptionReceiptVerificationException(
        'Unsupported receipt verification version.',
      );
    }

    final invoiceId = Uri.decodeComponent(parts[3]).trim();
    final paymentReference = Uri.decodeComponent(parts[4]).trim();
    if (invoiceId.isEmpty || paymentReference.isEmpty) {
      throw const InvalidSubscriptionReceiptVerificationException(
        'Receipt verification identifiers are required.',
      );
    }

    final issuedAt = int.tryParse(parts[6]);
    final expiresAt = int.tryParse(parts[7]);
    if (issuedAt == null || expiresAt == null || expiresAt <= issuedAt) {
      throw const InvalidSubscriptionReceiptVerificationException(
        'Receipt verification timing is invalid.',
      );
    }

    return SubscriptionReceiptVerificationPayload(
      version: version!,
      invoiceId: invoiceId,
      paymentReference: paymentReference,
      status: _statusFromToken(parts[5]),
      issuedAtEpochSeconds: issuedAt,
      expiresAtEpochSeconds: expiresAt,
    );
  }

  String _statusToken(SubscriptionReceiptVerificationStatus status) {
    return switch (status) {
      SubscriptionReceiptVerificationStatus.success => 'SUCCESS',
    };
  }

  SubscriptionReceiptVerificationStatus _statusFromToken(String token) {
    return switch (token) {
      'SUCCESS' => SubscriptionReceiptVerificationStatus.success,
      _ => throw const InvalidSubscriptionReceiptVerificationException(
        'Receipt verification status is invalid.',
      ),
    };
  }
}
