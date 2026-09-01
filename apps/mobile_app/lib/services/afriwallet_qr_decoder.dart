import '../models/qr_payment.dart';
import 'qr_payment_repository.dart';

class AfriWalletQrDecoder {
  const AfriWalletQrDecoder();

  QrPaymentPayload decode(String rawCode) {
    final code = rawCode.trim();
    if (code.isEmpty) {
      throw const InvalidQrPaymentException('The QR code is empty.');
    }

    final parts = code.split('|');
    if (parts.length < 5 || parts.first != 'AFW') {
      throw const InvalidQrPaymentException(
        'This is not a valid AfriWallet QR code.',
      );
    }

    final type = switch (parts[1].toLowerCase()) {
      'static' => QrPaymentType.static,
      'dynamic' => QrPaymentType.dynamic,
      _ => throw const InvalidQrPaymentException(
        'The AfriWallet QR payment type is invalid.',
      ),
    };

    final merchantId = parts[2].trim();
    if (merchantId.isEmpty) {
      throw const InvalidQrPaymentException(
        'The AfriWallet QR code has no merchant identifier.',
      );
    }

    final backendAmount = decimalMajorToMinor(parts[3]);
    if (type == QrPaymentType.static && backendAmount <= 0) {
      throw const InvalidQrPaymentException(
        'A static AfriWallet QR code requires a positive amount.',
      );
    }
    if (type == QrPaymentType.dynamic && backendAmount != 0) {
      throw const InvalidQrPaymentException(
        'A dynamic AfriWallet QR code must not pre-authorize an amount.',
      );
    }

    final currency = parts[4].trim().toUpperCase();
    if (!RegExp(r'^[A-Z]{3}$').hasMatch(currency)) {
      throw const InvalidQrPaymentException(
        'The AfriWallet QR currency is invalid.',
      );
    }

    return QrPaymentPayload(
      type: type,
      merchantId: merchantId,
      amountMinor: backendAmount,
      currencyCode: currency,
      merchantName: parts.length > 5 ? parts[5].trim() : '',
      description: parts.length > 6 ? parts.sublist(6).join('|').trim() : '',
    );
  }

  int decimalMajorToMinor(String rawAmount) {
    final value = rawAmount.trim();
    final match = RegExp(r'^(\d+)(?:\.(\d{1,2}))?$').firstMatch(value);
    if (match == null) {
      throw const InvalidQrPaymentException(
        'The AfriWallet QR amount is invalid.',
      );
    }

    final whole = int.parse(match.group(1)!);
    final fraction = (match.group(2) ?? '').padRight(2, '0');
    return whole * 100 + (fraction.isEmpty ? 0 : int.parse(fraction));
  }
}
