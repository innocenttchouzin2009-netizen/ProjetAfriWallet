import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/qr_payment.dart';
import 'package:mobile_app/services/afriwallet_qr_decoder.dart';
import 'package:mobile_app/services/qr_payment_repository.dart';

void main() {
  const decoder = AfriWalletQrDecoder();

  test('decodes a valid static AfriWallet QR without floating point money', () {
    final payload = decoder.decode(
      'AFW|Static|merchant-001|15.50|XAF|Afri Shop|Coffee purchase',
    );

    expect(payload.type, QrPaymentType.static);
    expect(payload.merchantId, 'merchant-001');
    expect(payload.amountMinor, 1550);
    expect(payload.currencyCode, 'XAF');
    expect(payload.merchantName, 'Afri Shop');
    expect(payload.description, 'Coffee purchase');
  });

  test('accepts a zero-amount dynamic QR', () {
    final payload = decoder.decode(
      'AFW|Dynamic|merchant-002|0|EUR|Afri Market|Open amount',
    );

    expect(payload.type, QrPaymentType.dynamic);
    expect(payload.amountMinor, 0);
  });

  test('rejects a non AfriWallet QR', () {
    expect(
      () => decoder.decode('OTHER|Static|merchant-001|10|EUR'),
      throwsA(isA<InvalidQrPaymentException>()),
    );
  });

  test('rejects an invalid static amount', () {
    expect(
      () => decoder.decode('AFW|Static|merchant-001|0|EUR'),
      throwsA(isA<InvalidQrPaymentException>()),
    );
  });

  test('rejects floating point precision beyond two decimal places', () {
    expect(
      () => decoder.decode('AFW|Static|merchant-001|10.999|EUR'),
      throwsA(isA<InvalidQrPaymentException>()),
    );
  });

  test('rejects an invalid currency code', () {
    expect(
      () => decoder.decode('AFW|Static|merchant-001|10|EURO'),
      throwsA(isA<InvalidQrPaymentException>()),
    );
  });
}
