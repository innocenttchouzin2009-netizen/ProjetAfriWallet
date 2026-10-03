import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/qr_payment.dart';
import 'package:mobile_app/pages/qr_payment_page.dart';
import 'package:mobile_app/services/qr_payment_repository.dart';

class FakeQrPaymentRepository implements QrPaymentRepository {
  int decodeCalls = 0;
  String? lastRawCode;

  @override
  Future<QrPaymentPayload> decodeAndValidate(String rawCode) async {
    decodeCalls += 1;
    lastRawCode = rawCode;

    if (rawCode.trim() != 'valid-code') {
      throw const InvalidQrPaymentException('QR backend invalide.');
    }

    return const QrPaymentPayload(
      type: QrPaymentType.static,
      merchantId: 'merchant-001',
      amountMinor: 1550,
      currencyCode: 'XAF',
      merchantName: 'Afri Shop',
      description: 'Coffee purchase',
      qrId: 'qr-001',
    );
  }

  @override
  Future<QrPaymentResult> initiatePayment({
    required QrPaymentPayload payload,
    required String payerWalletId,
  }) {
    throw UnimplementedError();
  }

  @override
  Future<QrPaymentResult> getAuthoritativeStatus(String transferIntentId) {
    throw UnimplementedError();
  }
}

void main() {
  testWidgets('test input is validated through the certified QR repository', (
    tester,
  ) async {
    final repository = FakeQrPaymentRepository();

    await tester.pumpWidget(
      MaterialApp(
        home: QrPaymentPage(
          repository: repository,
          onContinue: () {},
        ),
      ),
    );

    expect(find.text('Scanner avec la caméra'), findsOneWidget);
    expect(find.byKey(const Key('validate-qr-test-input')), findsOneWidget);

    await tester.enterText(
      find.byKey(const Key('qr-test-input')),
      'valid-code',
    );
    await tester.tap(find.byKey(const Key('validate-qr-test-input')));
    await tester.pumpAndSettle();

    expect(repository.decodeCalls, 1);
    expect(repository.lastRawCode, 'valid-code');
    expect(find.text('Vérifiez avant de payer'), findsOneWidget);
    expect(find.text('Afri Shop'), findsOneWidget);
    expect(find.text('15.50 XAF'), findsOneWidget);
    expect(find.text('merchant-001'), findsOneWidget);

    final backendNotice = find.textContaining(
      'confirmation financière autoritaire du backend',
    );
    await tester.scrollUntilVisible(
      backendNotice,
      200,
      scrollable: find.byType(Scrollable).first,
    );
    expect(backendNotice, findsOneWidget);
    expect(find.text('Paiement réussi'), findsNothing);
  });

  testWidgets('repository validation failure never reaches review', (
    tester,
  ) async {
    final repository = FakeQrPaymentRepository();

    await tester.pumpWidget(
      MaterialApp(
        home: QrPaymentPage(
          repository: repository,
          onContinue: () {},
        ),
      ),
    );

    await tester.enterText(
      find.byKey(const Key('qr-test-input')),
      'invalid-code',
    );
    await tester.tap(find.byKey(const Key('validate-qr-test-input')));
    await tester.pumpAndSettle();

    expect(repository.decodeCalls, 1);
    expect(find.byKey(const Key('qr-validation-error')), findsOneWidget);
    expect(find.text('Vérifiez avant de payer'), findsNothing);
    expect(find.text('Paiement réussi'), findsNothing);
  });

  testWidgets(
    'wallet return invokes dedicated callback without legacy continuation',
    (tester) async {
      var returnCount = 0;
      var continueCount = 0;

      await tester.pumpWidget(
        MaterialApp(
          home: QrPaymentPage(
            repository: FakeQrPaymentRepository(),
            onReturnToWallet: () => returnCount += 1,
            onContinue: () => continueCount += 1,
          ),
        ),
      );

      await tester.tap(find.byKey(const Key('qr-return-to-wallet')));

      expect(returnCount, 1);
      expect(continueCount, 0);
    },
  );

  testWidgets('legacy continuation remains available when supplied', (
    tester,
  ) async {
    var continueCount = 0;

    await tester.pumpWidget(
      MaterialApp(
        home: QrPaymentPage(
          repository: FakeQrPaymentRepository(),
          onContinue: () => continueCount += 1,
        ),
      ),
    );

    expect(find.byKey(const Key('qr-return-to-wallet')), findsNothing);
    expect(find.text('Continuer'), findsOneWidget);

    await tester.tap(find.text('Continuer'));
    expect(continueCount, 1);
  });
}
