import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/qr_payment.dart';
import 'package:mobile_app/services/afriwallet_qr_decoder.dart';
import 'package:mobile_app/services/qr_payment_repository.dart';

void main() {
  const decoder = AfriWalletQrDecoder();

  group('QR scanner validation contract', () {
    test('scanner payload decodes before any payment action', () {
      final payload = decoder.decode(
        'AFW|Static|merchant-qr-001|25.00|XAF|Afri Shop|QR purchase',
      );

      expect(payload.type, QrPaymentType.static);
      expect(payload.merchantId, 'merchant-qr-001');
      expect(payload.amountMinor, 2500);
      expect(payload.currencyCode, 'XAF');
      expect(payload.merchantName, 'Afri Shop');
    });

    test('foreign QR is rejected before review', () {
      expect(
        () => decoder.decode('HTTPS|Static|merchant-001|25.00|XAF'),
        throwsA(isA<InvalidQrPaymentException>()),
      );
    });

    test('empty scanner result is rejected', () {
      expect(
        () => decoder.decode('   '),
        throwsA(isA<InvalidQrPaymentException>()),
      );
    });

    test('dynamic scanner QR cannot carry a pre-authorized amount', () {
      expect(
        () => decoder.decode(
          'AFW|Dynamic|merchant-qr-002|10.00|EUR|Afri Market|Open amount',
        ),
        throwsA(isA<InvalidQrPaymentException>()),
      );
    });
  });
}
