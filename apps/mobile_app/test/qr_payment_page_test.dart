import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/qr_payment.dart';
import 'package:mobile_app/pages/qr_payment_page.dart';
import 'package:mobile_app/services/qr_payment_repository.dart';

class FakeQrPaymentRepository implements QrPaymentRepository {
  @override
  Future<QrPaymentPayload> decodeAndValidate(String rawCode) {
    throw UnimplementedError();
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
  testWidgets('camera-first page keeps test validation as a non-payment path', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: QrPaymentPage(
          repository: FakeQrPaymentRepository(),
          onContinue: () {},
        ),
      ),
    );

    expect(find.text('Scanner avec la caméra'), findsOneWidget);
    expect(find.byKey(const Key('validate-qr-test-input')), findsOneWidget);

    await tester.enterText(
      find.byKey(const Key('qr-test-input')),
      'AFW|Static|merchant-001|15.50|XAF|Afri Shop|Coffee purchase',
    );
    await tester.tap(find.byKey(const Key('validate-qr-test-input')));
    await tester.pumpAndSettle();

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

  testWidgets('invalid test QR never reaches review', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: QrPaymentPage(
          repository: FakeQrPaymentRepository(),
          onContinue: () {},
        ),
      ),
    );

    await tester.enterText(
      find.byKey(const Key('qr-test-input')),
      'NOT-AFW|bad',
    );
    await tester.tap(find.byKey(const Key('validate-qr-test-input')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('qr-validation-error')), findsOneWidget);
    expect(find.text('Vérifiez avant de payer'), findsNothing);
    expect(find.text('Paiement réussi'), findsNothing);
  });

  testWidgets('wallet return invokes dedicated callback without legacy continuation', (tester) async {
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
  });

  testWidgets('legacy continuation remains available when supplied', (tester) async {
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
