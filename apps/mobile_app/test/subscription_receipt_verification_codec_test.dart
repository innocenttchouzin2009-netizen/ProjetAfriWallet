import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/subscription_receipt_verification.dart';
import 'package:mobile_app/services/subscription_receipt_verification_codec.dart';

void main() {
  const codec = SubscriptionReceiptVerificationCodec();

  test('encodes and decodes a versioned receipt verification payload', () {
    const payload = SubscriptionReceiptVerificationPayload(
      version: SubscriptionReceiptVerificationCodec.currentVersion,
      invoiceId: 'invoice beta1.33',
      paymentReference: 'BETA-invoice|beta1.33',
      status: SubscriptionReceiptVerificationStatus.success,
      issuedAtEpochSeconds: 1_800_000_000,
      expiresAtEpochSeconds: 1_800_086_400,
    );

    final encoded = codec.encode(payload);
    final decoded = codec.decode(encoded);

    expect(encoded, startsWith('AFW|RECEIPT|1|'));
    expect(decoded.version, 1);
    expect(decoded.invoiceId, payload.invoiceId);
    expect(decoded.paymentReference, payload.paymentReference);
    expect(decoded.status, SubscriptionReceiptVerificationStatus.success);
    expect(decoded.issuedAtEpochSeconds, payload.issuedAtEpochSeconds);
    expect(decoded.expiresAtEpochSeconds, payload.expiresAtEpochSeconds);
  });

  test('rejects a QR payment payload as a receipt verification code', () {
    expect(
      () => codec.decode('AFW|static|merchant-1|12.50|EUR'),
      throwsA(isA<InvalidSubscriptionReceiptVerificationException>()),
    );
  });

  test('rejects unsupported receipt verification versions', () {
    expect(
      () => codec.decode(
        'AFW|RECEIPT|2|invoice-1|BETA-invoice-1|SUCCESS|1800000000|1800086400',
      ),
      throwsA(isA<InvalidSubscriptionReceiptVerificationException>()),
    );
  });

  test('reports receipt verification expiry locally', () {
    const payload = SubscriptionReceiptVerificationPayload(
      version: 1,
      invoiceId: 'invoice-1',
      paymentReference: 'BETA-invoice-1',
      status: SubscriptionReceiptVerificationStatus.success,
      issuedAtEpochSeconds: 1_800_000_000,
      expiresAtEpochSeconds: 1_800_086_400,
    );

    expect(
      payload.isExpiredAt(
        DateTime.fromMillisecondsSinceEpoch(1_800_086_401 * 1000, isUtc: true),
      ),
      isTrue,
    );
    expect(
      payload.isExpiredAt(
        DateTime.fromMillisecondsSinceEpoch(1_800_086_400 * 1000, isUtc: true),
      ),
      isFalse,
    );
  });
}
